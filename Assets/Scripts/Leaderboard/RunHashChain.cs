using System;
using System.Security.Cryptography;
using System.Text;

namespace Leaderboard
{
    // SHA-256 chunk chain + HMAC, byte-compatible with supabase/functions/_shared/crypto.ts:
    //   genesisHash = SHA256(runId + ":" + sessionKeyHex + ":genesis")
    //   chunkHash_i = SHA256(prevHash + ":" + canonicalJson(body_i))
    //   hmac_i      = HMAC_SHA256(sessionKeyBytes, ascii(chunkHash_i))
    public static class RunHashChain
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static string GenesisHash(string runId, string sessionKeyHex) =>
            Sha256Hex($"{runId}:{sessionKeyHex}:genesis");

        public static string ChunkHash(string prevHash, string canonicalBody) =>
            Sha256Hex($"{prevHash}:{canonicalBody}");

        public static string ChunkHmac(byte[] sessionKeyBytes, string chunkHashHex)
        {
            using (var hmac = new HMACSHA256(sessionKeyBytes))
                return ToHex(hmac.ComputeHash(Utf8.GetBytes(chunkHashHex)));
        }

        public static string Sha256Hex(string utf8) => Sha256Hex(Utf8.GetBytes(utf8));

        public static string Sha256Hex(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return ToHex(sha.ComputeHash(bytes));
        }

        public static byte[] HexToBytes(string hex)
        {
            if (hex == null || (hex.Length & 1) != 0)
                throw new ArgumentException("Expected an even-length hex string.", nameof(hex));
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = byte.Parse(hex.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            return bytes;
        }

        public static string ToHex(byte[] bytes)
        {
            var chars = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = "0123456789abcdef"[bytes[i] >> 4];
                chars[i * 2 + 1] = "0123456789abcdef"[bytes[i] & 0xF];
            }
            return new string(chars);
        }

        // Fills chunk.hmac and chunk.hash; the caller keeps chunk.hash as the
        // next chunk's prevHash and as lastHash for /finish.
        public static string Seal(RunChunk chunk, byte[] sessionKeyBytes)
        {
            string hash = ChunkHash(chunk.prevHash, RunChunkJson.BodyToCanonical(chunk));
            chunk.hash = hash;
            chunk.hmac = ChunkHmac(sessionKeyBytes, hash);
            return hash;
        }
    }
}
