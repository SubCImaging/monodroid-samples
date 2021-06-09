//-----------------------------------------------------------------------
// <copyright file="SubCCaptureSession.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Interfaces
{
    using System;

    public interface ICameraSessionCallback
    {
        event EventHandler<ICameraCaptureSession> Configured;

        event EventHandler<ICameraCaptureSession> ConfigureFailed;
    }
}