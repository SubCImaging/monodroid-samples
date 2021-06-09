using SubCTools.Droid.Interfaces;
using System;

namespace SubCTools.Droid.EventArguments
{
    public class CaptureEventArgs : EventArgs
    {
        public CaptureEventArgs(
            ICameraCaptureSession session,
            ICaptureResult captureResult
            //,
            //File sessionFile
            )
        {
            Session = session;
            CaptureResult = captureResult;
            //SessionFile = sessionFile;
        }

        public ICameraCaptureSession Session { get; }
        public ICaptureResult CaptureResult { get; }
        //public File SessionFile { get; }
    }
}