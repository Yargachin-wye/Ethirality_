using Leaderboard;
using NUnit.Framework;

namespace LeaderboardTests
{
    // Wire format checks against the contract in
    // supabase/functions/_shared/crypto.ts and types.ts: sorted keys, omitted
    // optionals, ECMAScript number formatting, SHA-256 chain, HMAC-SHA256.
    public class RunChunkSerializationTests
    {
        [TestCase(0, "0")]
        [TestCase(5, "5")]
        [TestCase(-3, "-3")]
        [TestCase(0.1, "0.1")]
        [TestCase(123.45, "123.45")]
        [TestCase(0.000001, "0.000001")]
        [TestCase(1e-7, "1e-7")]
        [TestCase(1.5e-7, "1.5e-7")]
        [TestCase(1e20, "100000000000000000000")]
        [TestCase(1e21, "1e+21")]
        [TestCase(2.5e22, "2.5e+22")]
        public void JsNumberMatchesEcmascriptFormatting(double value, string expected)
        {
            Assert.AreEqual(expected, RunChunkJson.JsNumber(value));
        }

        [Test]
        public void JsNumberNormalizesNonFiniteAndNegativeZero()
        {
            Assert.AreEqual("null", RunChunkJson.JsNumber(double.NaN));
            Assert.AreEqual("null", RunChunkJson.JsNumber(double.PositiveInfinity));
            Assert.AreEqual("null", RunChunkJson.JsNumber(double.NegativeInfinity));
            Assert.AreEqual("0", RunChunkJson.JsNumber(-0.0));
        }

        [Test]
        public void FloatPrecisionSurvivesAsShortestDouble()
        {
            // (double)0.3f is not 0.3; the wire must carry its own shortest
            // round-trip form so server re-serialization agrees byte-for-byte.
            Assert.AreEqual("0.30000001192092896", RunChunkJson.JsNumber((double)0.3f));
            Assert.AreEqual("1", RunChunkJson.JsNumber(1f));
        }

        [Test]
        public void ChunkBodyIsCanonicalWithSortedKeysAndOmittedOptionals()
        {
            var chunk = new RunChunk { seq = 0, tick = 500, prevHash = "ignored" };
            chunk.checkpoints.Add(new RunCheckpointEntry
            {
                tick = 100,
                state = new RunCheckpoint
                {
                    x = 1, y = 2, vx = 0, vy = 10, rotation = 0, angularVelocity = 0,
                    hp = 4, alive = true, frozen = false, dashing = false,
                    dashCooldown = 0, upgrades = new[] { 1, 2 }
                }
            });
            chunk.events.Add(new RunEventRecord { type = RunEventTypes.Damage, tick = 300, amount = 5 });
            chunk.events.Add(new RunEventRecord { type = RunEventTypes.PauseStart, tick = 400 });
            chunk.inputHistogram.look = 3;
            chunk.inputHistogram.dash = 1;

            Assert.AreEqual(
                "{\"checkpoints\":[{\"alive\":true,\"angularVelocity\":0,\"dashCooldown\":0," +
                "\"dashing\":false,\"frozen\":false,\"hp\":4,\"rotation\":0,\"tick\":100," +
                "\"upgrades\":[1,2],\"vx\":0,\"vy\":10,\"x\":1,\"y\":2}]," +
                "\"events\":[{\"amount\":5,\"tick\":300,\"type\":\"damage\"}," +
                "{\"tick\":400,\"type\":\"pauseStart\"}]," +
                "\"inputHistogram\":{\"dash\":1,\"look\":3}," +
                "\"seq\":0,\"tick\":500}",
                RunChunkJson.BodyToCanonical(chunk));
        }

        [Test]
        public void WireChunkCarriesHmacAndPrevHashButBodyHashDoesNot()
        {
            var chunk = new RunChunk { seq = 2, tick = 600, prevHash = "aa" };
            string body = RunChunkJson.BodyToCanonical(chunk);
            chunk.hmac = new string('f', 64);
            Assert.AreEqual(body, RunChunkJson.BodyToCanonical(chunk));

            string wire = RunChunkJson.ChunkToCanonical(chunk);
            StringAssert.Contains("\"hmac\":\"" + new string('f', 64) + "\"", wire);
            StringAssert.Contains("\"prevHash\":\"aa\"", wire);
            // key order: checkpoints, events, hmac, inputHistogram, prevHash, seq, tick
            Assert.Less(wire.IndexOf("\"events\"", System.StringComparison.Ordinal),
                wire.IndexOf("\"hmac\"", System.StringComparison.Ordinal));
            Assert.Less(wire.IndexOf("\"hmac\"", System.StringComparison.Ordinal),
                wire.IndexOf("\"inputHistogram\"", System.StringComparison.Ordinal));
            Assert.Less(wire.IndexOf("\"prevHash\"", System.StringComparison.Ordinal),
                wire.IndexOf("\"seq\"", System.StringComparison.Ordinal));
        }

        [Test]
        public void Sha256MatchesKnownVector()
        {
            Assert.AreEqual(
                "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                RunHashChain.Sha256Hex("abc"));
        }

        [Test]
        public void HmacMatchesKnownVector()
        {
            Assert.AreEqual(
                "f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8",
                RunHashChain.ChunkHmac(
                    System.Text.Encoding.ASCII.GetBytes("key"),
                    "The quick brown fox jumps over the lazy dog"));
        }

        [Test]
        public void HexRoundTripsAndRejectsOddInput()
        {
            var bytes = new byte[] { 0x00, 0x0f, 0xa5, 0xff };
            Assert.AreEqual("000fa5ff", RunHashChain.ToHex(bytes));
            CollectionAssert.AreEqual(bytes, RunHashChain.HexToBytes("000fa5ff"));
            Assert.Throws<System.ArgumentException>(() => RunHashChain.HexToBytes("abc"));
        }

        [Test]
        public void SealChainsChunksThroughPrevHash()
        {
            string sessionKeyHex = new string('a', 64);
            byte[] sessionKey = RunHashChain.HexToBytes(sessionKeyHex);
            string genesis = RunHashChain.GenesisHash("run-1", sessionKeyHex);
            Assert.AreEqual(64, genesis.Length);

            var first = new RunChunk { seq = 0, tick = 100, prevHash = genesis };
            first.inputHistogram.look = 1;
            RunHashChain.Seal(first, sessionKey);
            Assert.AreEqual(64, first.hash.Length);
            Assert.AreEqual(
                RunHashChain.ChunkHmac(sessionKey, first.hash), first.hmac);

            var second = new RunChunk { seq = 1, tick = 200, prevHash = first.hash };
            second.inputHistogram.shot = 2;
            RunHashChain.Seal(second, sessionKey);
            Assert.AreNotEqual(first.hash, second.hash);
            Assert.AreEqual(first.hash, second.prevHash);
        }

        [Test]
        public void DifferentBodiesProduceDifferentHashes()
        {
            var a = new RunChunk { seq = 0, tick = 10, prevHash = "g" };
            var b = new RunChunk { seq = 0, tick = 11, prevHash = "g" };
            Assert.AreNotEqual(
                RunHashChain.ChunkHash("g", RunChunkJson.BodyToCanonical(a)),
                RunHashChain.ChunkHash("g", RunChunkJson.BodyToCanonical(b)));
        }
    }
}
