using Android.Hardware.Camera2;
using Android.Runtime;
using SubCTools.Droid.EventArguments;
using System;

namespace SubCTools.Droid.Callbacks
{
    public interface ICameraStateCallback
    {
        event EventHandler<CameraDevice> Disconnected;

        event EventHandler<CameraErrorArgs> Error;

        event EventHandler<CameraDevice> Opened;
    }

    public class CameraStateCallback : CameraDevice.StateCallback
    {
        public event EventHandler<CameraDevice> Disconnected;

        public event EventHandler<CameraErrorArgs> Error;

        public event EventHandler<CameraDevice> Opened;

        public override void OnDisconnected(CameraDevice camera)
        {
            Disconnected?.Invoke(this, camera);
        }

        public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error)
        {
            Error?.Invoke(this, new CameraErrorArgs(camera, error));
        }

        public override void OnOpened(CameraDevice camera)
        {
            Opened?.Invoke(this, camera);
        }
    }
}