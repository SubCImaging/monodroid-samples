using NUnit.Framework;
using System.IO;

namespace SubCTools.Droid.UnitTests
{
    [TestFixture]
    public class DroidSystemTest
    {
        [Test]
        public void NoNull()
        {
            Assert.NotNull(DroidSystem.Instance);
        }

        [Test]
        public void HasLogDirectory()
        {
            Assert.AreEqual(DroidSystem.LogDirectory, Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(string.Empty).AbsolutePath, "Logs"));
        }

        [Test]
        public void TestShell()
        {
            var ls = DroidSystem.ShellSync("ls");
            Assert.AreNotEqual(ls, string.Empty);
        }

        [Test]
        public void DebugToggle()
        {
            Assert.IsFalse(DroidSystem.Instance.IsDebugging);
            DroidSystem.Instance.EnableDebugging();
            Assert.IsTrue(DroidSystem.Instance.IsDebugging);
            DroidSystem.Instance.DisableDebugging();
            Assert.IsFalse(DroidSystem.Instance.IsDebugging);
        }

        [Test]
        public void HasMAC()
        {
            Assert.AreNotEqual(DroidSystem.Instance.MAC, string.Empty);
        }

    }
}