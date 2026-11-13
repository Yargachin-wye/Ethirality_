using System;
using Leaderboard;
using NUnit.Framework;

namespace LeaderboardTests
{
    public class AuthSessionStoreTests
    {
        [Test]
        public void ProtectRoundTrips()
        {
            string blob = AuthSessionStore.Protect("{\"refreshToken\":\"abc\"}", "device-1");
            Assert.AreEqual("{\"refreshToken\":\"abc\"}", AuthSessionStore.Unprotect(blob, "device-1"));
        }

        [Test]
        public void WrongDeviceSecretIsRejected()
        {
            string blob = AuthSessionStore.Protect("secret", "device-1");
            Assert.IsNull(AuthSessionStore.Unprotect(blob, "device-2"));
        }

        [Test]
        public void TamperedBlobIsRejected()
        {
            var bytes = Convert.FromBase64String(AuthSessionStore.Protect("secret", "device-1"));
            bytes[20] ^= 0x01;
            Assert.IsNull(AuthSessionStore.Unprotect(Convert.ToBase64String(bytes), "device-1"));
        }

        [TestCase("")]
        [TestCase("not base64!")]
        [TestCase("AAAA")]
        public void GarbageIsRejected(string text)
        {
            Assert.IsNull(AuthSessionStore.Unprotect(text, "device-1"));
        }
    }
}
