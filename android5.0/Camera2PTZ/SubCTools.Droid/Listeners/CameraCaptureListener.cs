namespace SubCTools.Droid.Listeners
{
    using Android.Hardware.Camera2;
    using Android.Views;
    using Java.IO;
    using SubCTools.Droid.EventArguments;
    using SubCTools.Droid.Interfaces;
    using System;

    public class SubCCaptureCallback : CameraCaptureSession.CaptureCallback, ICameraCaptureListener
    {
        public event EventHandler<CaptureEventArgs> CaptureCompleted;

        public event EventHandler CaptureFailed;

        public event EventHandler<CaptureEventArgs> CaptureProgressed;

        public event EventHandler CaptureStarted;

        public event EventHandler SequenceCompleted;

        public override void OnCaptureBufferLost(CameraCaptureSession session, CaptureRequest request, Surface target, long frameNumber)
        {
            System.Console.WriteLine("OnCaptureBufferLost");
            base.OnCaptureBufferLost(session, request, target, frameNumber);
        }

        public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
        {
            CaptureCompleted?.Invoke(this, new CaptureEventArgs(new SubCCameraCaptureSession(session), new SubCCaptureResult(result)));
            base.OnCaptureCompleted(session, request, result);
        }

        public override void OnCaptureFailed(CameraCaptureSession session, CaptureRequest request, CaptureFailure failure)
        {
            System.Console.WriteLine("OnCaptureFailed: " + (request.Tag ?? "Unknown") + " Reason: " + failure.Reason);
            base.OnCaptureFailed(session, request, failure);
            CaptureFailed?.Invoke(this, EventArgs.Empty);
        }

        public override void OnCaptureProgressed(CameraCaptureSession session, CaptureRequest request, CaptureResult partialResult)
        {
            CaptureProgressed?.Invoke(this, new CaptureEventArgs(new SubCCameraCaptureSession(session), new SubCCaptureResult(partialResult)));
            base.OnCaptureProgressed(session, request, partialResult);
        }

        public override void OnCaptureSequenceAborted(CameraCaptureSession session, int sequenceId)
        {
            System.Console.WriteLine("OnCaptureSequenceAborted");
            base.OnCaptureSequenceAborted(session, sequenceId);
        }

        public override void OnCaptureSequenceCompleted(CameraCaptureSession session, int sequenceId, long frameNumber)
        {
            base.OnCaptureSequenceCompleted(session, sequenceId, frameNumber);
            SequenceCompleted?.Invoke(this, null);
        }

        public override void OnCaptureStarted(CameraCaptureSession session, CaptureRequest request, long timestamp, long frameNumber)
        {
            CaptureStarted?.Invoke(this, EventArgs.Empty);
            base.OnCaptureStarted(session, request, timestamp, frameNumber);
        }
    }
}