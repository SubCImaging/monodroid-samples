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

    public class SubCHandler : IHandler
    {
        //public SubCHandler(ILooper looper)
        //{
        //    Looper = looper;
        //}

        public SubCHandler(Handler handler)
        {
            Handler = handler;
        }

        //public ILooper Looper { get; }
        public Handler Handler { get; }
    }
}