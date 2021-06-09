// <copyright file="RecordingController.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using EmbedIO;
    using EmbedIO.Routing;
    using EmbedIO.WebApi;
    using Microsoft.AspNetCore.Mvc;
    using SubCTools.Droid.Camera;
    using SubCTools.Droid.Services;
    using SubCTools.Enums;

    /// <summary>
    /// Responsible for controlling stills through a REST interface.
    /// </summary>
    public class RecordingController : WebApiController
    {
        private readonly RecordingService r;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingController"/> class.
        /// </summary>
        /// <param name="r">Rayfin to interact with.</param>
        public RecordingController(RecordingService r)
        {
            this.r = r;
        }

        [Route(HttpVerbs.Get, "/start")]
        public void StartRecording()
        {
            try
            {
                r.StartRecording();
            }
            catch (Exception e)
            {
                throw HttpException.BadRequest(e.Message);
            }
        }

        [Route(HttpVerbs.Get, "/stop")]
        public Task StopRecording()
        {
            var t = r.StopRecording();

            if (t.Exception != null)
            {
                throw HttpException.BadRequest(t.Exception.Message);
            }

            return Task.CompletedTask;
        }
    }
}