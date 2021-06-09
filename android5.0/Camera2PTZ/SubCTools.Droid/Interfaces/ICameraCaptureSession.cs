//-----------------------------------------------------------------------
// <copyright file="SubCCaptureSession.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Interfaces
{
    using System.Collections.Generic;

    public interface ICameraCaptureSession
    {
        int Capture(ICaptureRequest request, ICameraCaptureListener listener, IHandler handler);

        int CaptureBurst(IList<ICaptureRequest> requests, ICameraCaptureListener listener, IHandler handler);

        int SetRepeatingBurst(IList<ICaptureRequest> requests, ICameraCaptureListener listener, IHandler handler);

        int SetRepeatingRequest(ICaptureRequest request, ICameraCaptureListener listener, IHandler handler);

        void Close();

        void Prepare(ISurface surface);

        void StopRepeating();
    }
}