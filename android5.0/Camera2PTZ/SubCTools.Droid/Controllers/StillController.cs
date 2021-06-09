// <copyright file="StillController.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
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
    using SubCTools.Droid.Camera;
    using SubCTools.Droid.Services;

    /// <summary>
    /// Responsible for controlling stills through a REST interface.
    /// </summary>
    public class StillController : WebApiController
    {
        private readonly StillService r;

        /// <summary>
        /// Initializes a new instance of the <see cref="StillController"/> class.
        /// </summary>
        /// <param name="r">Rayfin to interact with.</param>
        public StillController(StillService r)
        {
            this.r = r;
        }

        [Route(HttpVerbs.Get, "/take")]
        public async Task<string> TakePicture()
        {
            return await r.TakePictureAsync();
        }
    }
}