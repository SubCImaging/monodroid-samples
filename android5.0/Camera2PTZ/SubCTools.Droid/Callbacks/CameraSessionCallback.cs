using Android.Hardware.Camera2;
using SubCTools.Droid.Interfaces;
using System;
using System.Collections.Generic;

namespace SubCTools.Droid.Callbacks
{
    /*public class SubCCameraCaptureSession : ICameraCaptureSession
    {
        private readonly CameraCaptureSession cameraCaptureSession;

        public SubCCameraCaptureSession(CameraCaptureSession cameraCaptureSession)
        {
            this.cameraCaptureSession = cameraCaptureSession;
        }

        public int Capture(ICaptureRequest request, ICameraCaptureListener listener, IHandler handler)
        {
            throw new NotImplementedException();
        }

        public int CaptureBurst(IList<ICaptureRequest> requests, ICameraCaptureListener listener, IHandler handler)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Prepare(ISurface surface)
        {
            throw new NotImplementedException();
        }

        public int SetRepeatingBurst(IList<ICaptureRequest> requests, ICameraCaptureListener listener, IHandler handler)
        {
            throw new NotImplementedException();
        }

        public int SetRepeatingRequest(ICaptureRequest request, ICameraCaptureListener listener, IHandler handler)
        {
            throw new NotImplementedException();
        }

        public void StopRepeating()
        {
            throw new NotImplementedException();
        }
    }*/

    public class CameraSessionCallback : CameraCaptureSession.StateCallback, ICameraSessionCallback
    {
        public event EventHandler<ICameraCaptureSession> Configured;

        public event EventHandler<ICameraCaptureSession> ConfigureFailed;

        public override void OnConfigured(CameraCaptureSession session)
        {
            Configured?.Invoke(this, new SubCCameraCaptureSession(session));
        }

        public override void OnConfigureFailed(CameraCaptureSession session)
        {
            ConfigureFailed?.Invoke(this, new SubCCameraCaptureSession(session));
        }
    }
}