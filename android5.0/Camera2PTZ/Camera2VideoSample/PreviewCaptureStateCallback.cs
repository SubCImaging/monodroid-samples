using Android.Hardware.Camera2;
using Android.Widget;

namespace Camera2PTZ
{
    public class PreviewCaptureStateCallback : CameraCaptureSession.StateCallback
    {
        private Camera2VideoFragment fragment;

        public PreviewCaptureStateCallback(Camera2VideoFragment frag)
        {
            fragment = frag;
        }

        public override void OnConfigured(CameraCaptureSession session)
        {
            //fragment.previewSession = session;
            //fragment.updatePreview ();
        }

        public override void OnConfigureFailed(CameraCaptureSession session)
        {
            if (null != fragment.Activity)
                Toast.MakeText(fragment.Activity, "Failed", ToastLength.Short).Show();
        }
    }
}