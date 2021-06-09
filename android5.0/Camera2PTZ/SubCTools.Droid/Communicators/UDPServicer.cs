namespace SubCTools.Droid.Communicators
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using Java.IO;
    using Java.Security;
    using SubCTools.Attributes;
    using SubCTools.Communicators;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.Managers;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    public class UDPServicer : DroidBase
    {
        private readonly SubCUDP server;//
        private readonly CameraInfo cameraInfo;
        private Guid GUIDCode;

        private string nickname;

        public UDPServicer(
            ISettingsService settings,
            SubCUDP server,
            CameraInfo cameraInfo)
            : base(settings)
        {
            this.server = server;
            this.cameraInfo = cameraInfo;
            server.DataReceived += Server_DataReceived;

            LoadSettings();
        }

        //[Savable]
        //[RemoteState]
        //public string Nickname
        //{
        //    get => nickname;
        //    set => Set(nameof(Nickname), ref nickname, value);
        //}

        private async void Server_DataReceived(object sender, string e)
        {
            e = e.ToLower().Trim();

            if (e == "israyfin")
            {
                server.SendAsync("yes");
            }
            else if (e == "camerainfo")
            {
                server.SendAsync(Newtonsoft.Json.JsonConvert.SerializeObject(cameraInfo));
            }
            else if (e == "name")
            {
                server.SendAsync(cameraInfo.Name);
                //server.SendAsync(!string.IsNullOrEmpty(Nickname) ? Nickname : GetCommandOutput("getprop net.hostname"));
            }
            else if (e == "requestunlock")
            {
                server.SendAsync("Unlock code =");
                server.SendAsync(RequestUnlock());
                //server.SendAsync("password=");
                //server.SendAsync(HashKey(GUIDCode.ToString()));
            }
            else if (e == "clearsettings")
            {
                Shell("rm -r /storage/emulated/0/Settings");
                await server.SendAsync("success");
                Shell("reboot");
            }
            else if (e == "protocol")
            {
                if (Settings.TryLoad("Protocol", out string protocol))
                {
                    await server.SendAsync($"Protocol:{protocol}");
                }
                else
                {
                    await server.SendAsync($"Protocol:TCP");
                }
            }
            else if (e == "cameratype")
            {
                await server.SendAsync(DroidSystem.Instance.CameraType.ToString());
            }
            //else if (e == "open" || e == "start")
            //{
            //    try
            //    {
            //        var intent = new Intent();
            //        intent.SetComponent(new ComponentName("SubCRayfin.SubCRayfin", "subcrayfin.activity"));
            //        StartActivity(intent);
            //    }
            //    catch (Exception ex)
            //    {
            //        server.SendAsync(ex.ToString());
            //    }
            //}
            //else if (e == "lock")
            //{
            //    server.SendAsync("Locked");
            //    Lock();
            //}
            //else if (e.ToLower() == HashKey(GUIDCode.ToString()).ToLower())
            //{
            //    server.SendAsync("Unlocked");
            //    Unlock();
            //}
        }

        private string GetCommandOutput(string Command)
        {
            // Run the command
            var process = Java.Lang.Runtime.GetRuntime().Exec(new[] { "su", "-c", Command });
            var bufferedReader = new BufferedReader(new InputStreamReader(process.InputStream));

            // Grab the results
            var log = new System.Text.StringBuilder();
            var line = "";
            while ((line = bufferedReader.ReadLine()) != null)
            {
                log.AppendLine(line);
            }
            log.Replace("\n", "");
            return log.ToString();
        }

        private void Shell(string Command)
        {
            var process = Java.Lang.Runtime.GetRuntime().Exec(new[] { "su", "-c", Command });
            process.WaitFor();
        }

        private void Lock()
        {
            Shell("mount -o rw,remount /system");
            Shell("cp /system/etc/permissions/ralo /system/build.prop");
            Shell("mount -o ro,remount /system");
            Shell("reboot");
        }

        private void Unlock()
        {
            Shell("mount -o rw,remount /system");
            Shell("cp /system/etc/permissions/raun /system/build.prop");
            Shell("mount -o ro,remount /system");
            Shell("reboot");
        }

        private void CheckUnlockCode(string UnlockCode)
        {
            if (UnlockCode == HashKey(GUIDCode.ToString()))
            {
                Unlock();
                return;
            }
            GUIDCode = new Guid();
            Lock();
        }

        private string RequestUnlock()
        {
            GUIDCode = Guid.NewGuid();
            return GUIDCode.ToString();
        }

        public static string HashSha512(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            SHA512Managed hashstring = new SHA512Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }

        private string HashKey(string Key)
        {
            char[] prependChars = { (char)0x53, (char)0x75, (char)0x62,
                (char)0x43, (char)0x49, (char)0x6d, (char)0x61, (char)0x67,
                (char)0x69, (char)0x6e, (char)0x67, (char)0x32, (char)0x30,
                (char)0x31, (char)0x37 };

            char[] appendChars = { (char)0x63, (char)0x52, (char)0x40, (char)0x6e,
                (char)0x4b, (char)0x59, (char)0x54, (char)0x72, (char)0x33, (char)0x33 };

            return HashSha512(prependChars + Key + appendChars);
        }
    }
}