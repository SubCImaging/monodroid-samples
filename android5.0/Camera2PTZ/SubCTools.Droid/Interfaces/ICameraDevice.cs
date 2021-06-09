//-----------------------------------------------------------------------
// <copyright file="SubCCaptureSession.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Interfaces
{
    using SubCTools.Droid.Enums;
    using System.Collections.Generic;

    public interface ICameraDevice
    {
        ICaptureBuilder CreateCaptureRequest(SubCCameraTemplate cameraTemplate);

        void CreateCaptureSession(IList<ISurface> surfaces, ICameraSessionCallback callback, IHandler handler);
    }
}