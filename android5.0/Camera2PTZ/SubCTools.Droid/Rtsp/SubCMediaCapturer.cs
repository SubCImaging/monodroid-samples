//-----------------------------------------------------------------------
// <copyright file="SubCMediaCapturer.cs" company="SubCImaging">
//     Copyright (c) SubCImaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Rtsp
{
    using System;
    using Android.Content;
    using Android.Runtime;
    using Android.Util;
    using Veg.Mediacapture.Sdk;

    /// <summary>
    /// A class that wraps the <see cref="MediaCapture"/> class and implements <see cref="IMediaCapturer"/>
    /// to make the parent objects easier to test and their children easier to mock.
    /// </summary>
    public class SubCMediaCapturer : MediaCapture, IMediaCapturer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubCMediaCapturer"/> class.
        /// </summary>
        /// <param name="context">The <see cref="Context"/> of the root application.</param>
        /// <param name="attr">Unknown <see cref="IAttributeSet"/></param>
        /// <param name="is_window">Unknown <see cref="bool"/></param>
        public SubCMediaCapturer(Context context, IAttributeSet attr, bool is_window) : base(context, attr, is_window)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCMediaCapturer"/> class.
        /// </summary>
        /// <param name="context">The <see cref="Context"/> of the root application.</param>
        /// <param name="attr">Unknown <see cref="IAttributeSet"/></param>
        public SubCMediaCapturer(Context context, IAttributeSet attr) : base(context, attr)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCMediaCapturer"/> class.
        /// </summary>
        /// <param name="javaReference">Unknown <see cref="IntPtr"/></param>
        /// <param name="transfer">Unknown <see cref="JniHandleOwnership"/></param>
        public SubCMediaCapturer(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }
    }
}