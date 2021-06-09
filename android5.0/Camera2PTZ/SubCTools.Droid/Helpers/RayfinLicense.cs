namespace SubCTools.Droid.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Util;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Enums;
    using SubCTools.Security;

    public static class RayfinLicense
    {
        private const string Salt = @"qx2yFHQ?vQXeZf!x4V7$MnPmY4*+MLky";
        private static readonly FileInfo LicenseFile = new FileInfo("/data/local/tmp/license");

        public static CameraType FetchCameraType()
        {
            var type = DroidSystem.GetProp("persist.camera.type");
            var key = DroidSystem.GetProp("persist.camera.key");
            var hwid = Security.Encryption.UniqueIdentifier();
            var validKey = Encryption.ComputeSha256Hash(type + hwid + Salt);
            var licenseFileContents = DroidSystem.ShellSync("cat /data/local/tmp/license").TrimEnd();

            if (key == validKey && licenseFileContents == validKey)
            {
                Log.Debug("Rayfin", $"Valid license detected for {type}");
            }
            else
            {
                Fail();
            }

            var cameraType = (CameraType)Enum.Parse(typeof(CameraType), type);
            return cameraType;
        }

        private static void Fail()
        {
            Log.Error("Rayfin", "Internal Error e:999");
            throw new UnauthorizedAccessException("Internal Error e:999");
        }
    }
}