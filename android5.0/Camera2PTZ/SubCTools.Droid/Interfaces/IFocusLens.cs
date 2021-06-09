//-----------------------------------------------------------------------
// <copyright file="SubCCamera.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Interfaces
{
    using Android.Content;
    using Android.Graphics;
    using Android.Hardware.Camera2;
    using Android.Hardware.Camera2.Params;
    using Android.OS;
    using Android.Runtime;
    using Android.Util;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Attributes;
    using SubCTools.DiveLog.Interfaces;
    using SubCTools.Droid.Attributes;
    using SubCTools.Droid.Callbacks;
    using SubCTools.Droid.Converters;
    using SubCTools.Droid.Enums;
    using SubCTools.Droid.EventArguments;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Droid.Listeners;
    using SubCTools.Droid.Tools;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public interface IFocusLens
    {
        bool IsManualFocus { get; }

        void EnableAutoFocus();

        void EnableManualFocus();
    }
}