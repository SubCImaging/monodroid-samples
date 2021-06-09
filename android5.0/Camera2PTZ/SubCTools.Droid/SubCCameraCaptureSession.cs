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

    public class SubCCameraCaptureSession : ICameraCaptureSession
    {
        private readonly CameraCaptureSession session;

        public SubCCameraCaptureSession(CameraCaptureSession session)
        {
            this.session = session;
        }

        public int Capture(ICaptureRequest request, ICameraCaptureListener listener, IHandler handler)
        {
            if (request is SubCCaptureRequest r && listener is SubCCaptureCallback l && handler is SubCHandler h)
            {
                return session.Capture(r.Request, l, h.Handler);
            }

            throw new ArgumentException("Request must be SubCCaptureRequest, listener SubCCaptureCallback, and handler Handler");
        }

        public int CaptureBurst(IList<ICaptureRequest> requests, ICameraCaptureListener listener, IHandler handler)
        {
            if (requests is IList<SubCCaptureRequest> r && listener is SubCCaptureCallback l && handler is SubCHandler h)
            {
                return session.CaptureBurst(r.Select(re => re.Request).ToList(), l, h.Handler);
            }

            throw new ArgumentException("Request must be SubCCaptureRequest, listener SubCCaptureCallback, and handler Handler");
        }

        public void Close()
        {
            session.Close();
        }

        public void Prepare(ISurface surface)
        {
            if (surface is Surface s)
            {
                session.Prepare(s);
            }
            else
            {
                throw new ArgumentException("surface must be Surface");
            }
        }

        public int SetRepeatingBurst(IList<ICaptureRequest> requests, ICameraCaptureListener listener, IHandler handler)
        {
            if (requests is IList<SubCCaptureRequest> r && listener is SubCCaptureCallback l && handler is SubCHandler h)
            {
                return session.SetRepeatingBurst(r.Select(re => re.Request).ToList(), l, h.Handler);
            }

            throw new ArgumentException("Request must be SubCCaptureRequest, listener SubCCaptureCallback, and handler Handler");
        }

        public int SetRepeatingRequest(ICaptureRequest request, ICameraCaptureListener listener, IHandler handler)
        {
            if (request is SubCCaptureRequest r && listener is SubCCaptureCallback l && handler is SubCHandler h)
            {
                return session.SetRepeatingRequest(r.Request, l, h.Handler);
            }

            throw new ArgumentException($"Request {request is SubCCaptureRequest} must be SubCCaptureRequest, listener {listener is SubCCaptureCallback} SubCCaptureCallback, and handler {handler is SubCHandler} Handler");
        }

        public void StopRepeating()
        {
            session.StopRepeating();
        }
    }
}