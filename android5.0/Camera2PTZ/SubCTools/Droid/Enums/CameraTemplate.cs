// <copyright file="CameraTemplate.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid.Enums
{
    //
    // Summary:
    //     Enumerates values returned by several methods of Android.Hardware.Camera2.CameraTemplate
    //     and taken as a parameter of the Android.Hardware.Camera2.CameraDevice.CreateCaptureRequest
    //     member.
    //
    // Remarks:
    //     Enumerates values returned by the following: Android.Hardware.Camera2.CameraTemplate.ManualAndroid.Hardware.Camera2.CameraTemplate.PreviewAndroid.Hardware.Camera2.CameraTemplate.RecordAndroid.Hardware.Camera2.CameraTemplate.StillCaptureAndroid.Hardware.Camera2.CameraTemplate.VideoSnapshotAndroid.Hardware.Camera2.CameraTemplate.ZeroShutterLag
    //     and taken as a parameter of the Android.Hardware.Camera2.CameraDevice.CreateCaptureRequest
    //     member.
    public enum SubCCameraTemplate
    {
        //
        // Summary:
        //     To be added.
        Preview = 1,

        //
        // Summary:
        //     To be added.
        StillCapture = 2,

        //
        // Summary:
        //     To be added.
        Record = 3,

        //
        // Summary:
        //     To be added.
        VideoSnapshot = 4,

        //
        // Summary:
        //     To be added.
        ZeroShutterLag = 5,

        //
        // Summary:
        //     To be added.
        Manual = 6,
    }
}