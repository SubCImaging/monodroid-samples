using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Jcifs.Smb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubCTools.Droid.Helpers
{
    class JCIFS
    {
        public static string USER_NAME = null;
        public static string PASSWORD = null;
        public static string DOMAIN = null;
        public static string NETWORK_FOLDER = "smb://192.168.2.21/Rayfin/";

        public static void Send(byte[] data, string filename)
        {
            try
            {
                Task.Run(() =>
                {
                    copyFiles(data, filename);
                });
            }
            catch (Exception e)
            {
                System.Console.Out.WriteLine("Exception caught. Cause: " + e.Message);
            }
        }

        public static bool copyFiles(byte[] data, string fileName)
        {
            bool successful = false;
            string path = null;
            NtlmPasswordAuthentication auth = null;
            SmbFile sFile = null;
            SmbFileOutputStream sfos = null;
            try
            {
                auth = new NtlmPasswordAuthentication(DOMAIN, USER_NAME, PASSWORD);
                path = NETWORK_FOLDER + fileName;
                sFile = new SmbFile(path, auth);
                sfos = new SmbFileOutputStream(sFile);
                sfos.Write(data);
                successful = true;
                System.Console.Out.WriteLine("File successfully created.");
            }
            catch (Exception e)
            {
                successful = false;
                System.Console.Out.WriteLine("Unable to create file. Cause: " + e.Message);
            }
            return successful;
        }
    }
}