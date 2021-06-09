using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SubCTools.Droid.Helpers
{
    public class SubCFileObserver : Android.OS.FileObserver
    {
        private string currentPath;
        private Timer modificationTimer = new Timer() { Interval = 5000 };
        private DateTime lastModification;
        private TimeSpan timeOut;

        private bool isClosed = false;

        public SubCFileObserver(string path) : this(path, TimeSpan.FromSeconds(10))
        {

        }

        public SubCFileObserver(string path, TimeSpan timeOut) : base(path)
        {
            this.timeOut = timeOut;

            StartWatching();
            modificationTimer.Start();
            currentPath = path;

            modificationTimer.Elapsed += ModificationTimer_Elapsed;
        }

        public event EventHandler<string> FileClosed;
        //public event EventHandler<string> WriteFailure;

        public new async Task Wait()
        {
            if (isClosed)
            {
                return;
            }

            var tcs = new TaskCompletionSource<bool>();

            var handler = new EventHandler<string>((s, e) =>
            {
                tcs.TrySetResult(true);
            });

            FileClosed += handler;

            var timer = new Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);
            timer.Elapsed += (s, e) => tcs.TrySetResult(false);
            timer.Start();

            var result = await tcs.Task;

            timer.Stop();

            FileClosed -= handler;

            if (!result)
            {
                throw new Exception($"File {currentPath} failed to close");
            }
        }

        public override void OnEvent([GeneratedEnum] FileObserverEvents e, string path)
        {
            switch (e)
            {
                case FileObserverEvents.Modify:
                    lastModification = DateTime.Now;
                    //Console.WriteLine(currentPath + " " + lastModification);
                    break;
                //case FileObserverEvents.CloseNowrite:
                case FileObserverEvents.CloseWrite:
                    isClosed = true;

                    modificationTimer.Stop();
                    StopWatching();
                    FileClosed?.Invoke(this, currentPath);
                    break;
            }
        }

        private void ModificationTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if ((e.SignalTime - lastModification) >= timeOut)
            {
                StopWatching();
                modificationTimer.Stop();
                FileClosed?.Invoke(this, currentPath);
                //WriteFailure?.Invoke(this, currentPath);
            }
        }
    }
}