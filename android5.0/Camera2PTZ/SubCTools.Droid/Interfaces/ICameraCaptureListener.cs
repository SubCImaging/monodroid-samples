using SubCTools.Droid.EventArguments;
using System;

namespace SubCTools.Droid.Interfaces
{
    public interface ICameraCaptureListener
    {
        event EventHandler<CaptureEventArgs> CaptureCompleted;

        event EventHandler<CaptureEventArgs> CaptureProgressed;

        event EventHandler/*<CaptureFailure>*/ CaptureFailed;

        event EventHandler CaptureStarted;

        event EventHandler SequenceCompleted;
    }
}