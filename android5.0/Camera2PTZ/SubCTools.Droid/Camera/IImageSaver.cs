//-----------------------------------------------------------------------
// <copyright file="StillHandler.cs" company="SubCImaging">
//     Copyright (c) SubCImaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Camera
{
    using Android.Graphics;
    using Android.Hardware.Camera2;
    using Android.Media;
    using Android.OS;
    using Android.Util;
    using SubCTools.Attributes;
    using SubCTools.Droid.Attributes;
    using SubCTools.Droid.Converters;
    using SubCTools.Droid.Enums;
    using SubCTools.Droid.EventArguments;
    using SubCTools.Droid.Extensions;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Droid.Listeners;
    using SubCTools.Droid.Models;
    using SubCTools.Droid.Tools;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IImageSaver
    {
        void SaveJpeg(Image image, FileInfo file);

        void SaveRaw(CameraCharacteristics characteristics, ICaptureResult result, Image image, FileInfo file);
    }
}