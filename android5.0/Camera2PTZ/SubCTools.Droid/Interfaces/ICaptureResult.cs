//-----------------------------------------------------------------------
// <copyright file="SubCCaptureSession.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Interfaces
{
    public interface ICaptureResult
    {
        Java.Lang.Object Get(ICaptureResultKey key);
    }
}