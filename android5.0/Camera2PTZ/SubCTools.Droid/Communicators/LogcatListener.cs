using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.IO;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubCTools.Droid.Communicators
{
    public class LogcatListener
    {
        public LogcatListener(string filter)
        {
            Listen(filter);
        }

        public event EventHandler<string> DataReceived;

        public event EventHandler StreamClosed;

        private void Listen(string filter)
        {
            try
            {
                var log = new Java.Lang.StringBuilder();
                var process = Runtime.GetRuntime().Exec(new[] { $@"logcat | grep {filter}" });
                var bufferedReader = new BufferedReader(
                new InputStreamReader(process.InputStream));

                string line;

                while ((line = bufferedReader.ReadLine()) != null)
                {
                    DataReceived?.Invoke(this, line);
                }
                StreamClosed?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                StreamClosed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}