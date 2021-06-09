using Android.Media;
using Android.Runtime;
using System;

namespace SubCTools.Droid.Listeners
{

    public struct MediaRecorderEventArgs
    {
        public MediaRecorderEventArgs(MediaRecorder mediaRecorder, MediaRecorderInfo info)
        {
            MediaRecorder = mediaRecorder;
            Info = info;
        }

        public MediaRecorder MediaRecorder { get; }
        public MediaRecorderInfo Info { get; }
    }

    public class MediaRecorderListener : Java.Lang.Object, MediaRecorder.IOnInfoListener
    {
        public event EventHandler<MediaRecorderEventArgs> InfoGenerated;
        public event EventHandler MaxReached;

        public void OnInfo(MediaRecorder mr, [GeneratedEnum] MediaRecorderInfo Event, int extra)
        {
            Console.WriteLine("Recorder info: " + Event);
            InfoGenerated?.Invoke(this, new MediaRecorderEventArgs(mr, Event));

            if (Event == MediaRecorderInfo.MaxDurationReached || Event == MediaRecorderInfo.MaxFilesizeReached)
            {
                MaxReached?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}