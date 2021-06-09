using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.Camera
{
    public class VideoSnapshot
    {
        private readonly StillHandler stillHandler;
        private readonly RecordingHandler recordingHandler;

        public VideoSnapshot(StillHandler stillHandler, RecordingHandler recordingHandler)
        {
            this.stillHandler = stillHandler;
            this.recordingHandler = recordingHandler;
        }
    }
}