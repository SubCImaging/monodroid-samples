//-----------------------------------------------------------------------
// <copyright file="SubCCaptureSession.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid
{
    using Android.Graphics;
    using Android.Hardware.Camera2;
    using Android.OS;
    using Android.Views;
    using SubCTools.Droid.Callbacks;
    using SubCTools.Droid.Enums;
    using SubCTools.Droid.EventArguments;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Droid.Listeners;
    using SubCTools.Droid.Models;
    using SubCTools.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class SubCCameraDevice : ICameraDevice
    {
        private readonly CameraDevice cameraDevice;

        public SubCCameraDevice(CameraDevice cameraDevice)
        {
            this.cameraDevice = cameraDevice;
        }

        public ICaptureBuilder CreateCaptureRequest(SubCCameraTemplate cameraTemplate)
        {
            var iTemp = (int)cameraTemplate;
            var builder = cameraDevice.CreateCaptureRequest((CameraTemplate)iTemp);
            return new SubCCaptureBuilder(builder);
        }

        public void CreateCaptureSession(IList<ISurface> surfaces, ICameraSessionCallback callback, IHandler handler)
        {
            var sur = new List<Surface>();
            foreach (var item in surfaces)
            {
                sur.Add((item as SubCSurface).Surface);
            }
            //surfaces is IList<SubCSurface> l &&

            if (callback is CameraSessionCallback c && handler is SubCHandler h)
            {
                cameraDevice.CreateCaptureSession(sur, c, h.Handler);
            }
            else
            {
                throw new ArgumentException($"surfaces {surfaces is IList<SubCSurface>} need to be SubCSurface, callback {callback is CameraSessionCallback} CameraSessionCallback, handler {handler is SubCHandler} Handler");
            }
        }
    }
}