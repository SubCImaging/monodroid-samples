//-----------------------------------------------------------------------
// <copyright file="SubCCaptureSession.cs" company="SubC Imaging Ltd">
// Copyright (c) SubC Imaging Ltd. All rights reserved.
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

    /// <summary>
    /// SubC implementation of the Java CameraCaptureSession
    /// </summary>
    public class SubCCaptureSession
    {
        /// <summary>
        /// Camera device for creating capture sessions and capturerequests
        /// </summary>
        private readonly ICameraDevice camera;

        /// <summary>
        /// Collection of all cameras settings to apply to the surfaces
        /// </summary>
        private readonly Dictionary<CaptureRequest.Key, Java.Lang.Object> cameraSettings = new Dictionary<CaptureRequest.Key, Java.Lang.Object>();

        /// <summary>
        /// Thread for executing session calls
        /// </summary>
        private readonly IHandler handler;

        /// <summary>
        /// Composed Android capture session
        /// </summary>
        private ICameraCaptureSession captureSession;

        /// <summary>
        /// Surfaces that will always be used with every capture
        /// </summary>
        private Dictionary<SurfaceTypes, ISurface> persistentSurfaces = new Dictionary<SurfaceTypes, ISurface>();

        /// <summary>
        /// Temporarily hold surfaces until they need to be used again. When recording 4K for example
        /// </summary>
        private Dictionary<SurfaceTypes, ISurface> tempSurfaces = new Dictionary<SurfaceTypes, ISurface>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCCaptureSession"/> class.
        /// </summary>
        /// <param name="camera">Camera device used to create the sessions and the capture requests</param>
        /// <param name="handler">Thread to execute the calls on</param>
        public SubCCaptureSession(ICameraDevice camera, IHandler handler)
        {
            this.camera = camera;
            this.handler = handler;
        }

        /// <summary>
        /// Event to fire when a capture (picture) is completed
        /// </summary>
        public event EventHandler<ICaptureResult> CaptureCompleted;

        /// <summary>
        /// Event to fire when a capture fails
        /// </summary>
        public event EventHandler CaptureFailed;

        /// <summary>
        /// Gets collection of surfaces that are currently active
        /// </summary>
        public Dictionary<SurfaceTypes, ISurface> Surfaces { get; } = new Dictionary<SurfaceTypes, ISurface>();

        /// <summary>
        /// Perform a capture request on the capture session
        /// </summary>
        /// <param name="builder">Builder which holds surfaces and settings to build the capture request</param>
        /// <returns>The results of the capture request</returns>
        public async Task<CaptureEventArgs> Capture(ICaptureBuilder builder)
        {
            builder.AddTarget(persistentSurfaces[SurfaceTypes.Preview]);
            return await Capture(builder?.Build());
        }

        /// <summary>
        /// Perform a capture request on the capture session
        /// </summary>
        /// <param name="request">The request with all settings to capture</param>
        /// <returns>The results of the capture request</returns>
        public async Task<CaptureEventArgs> Capture(ICaptureRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("Request cannot be null");
            }

            var callback = new SubCCaptureCallback();

            var completed = new TaskCompletionSource<CaptureEventArgs>();

            var completedHandler = new EventHandler<CaptureEventArgs>((s, e) =>
            {
                completed.TrySetResult(e);
            });

            callback.CaptureCompleted += completedHandler;
            callback.CaptureFailed += (s, e) => completedHandler(s, null);

            if (Capture(request, callback, handler) == -1)
            {
                return null;
            }

            return await completed.Task;
        }

        /// <summary>
        /// Perform a capture request on the capture session
        /// </summary>
        /// <param name="request">The request with all settings to capture</param>
        /// <param name="listener">Listener to call when capture is completed</param>
        /// <param name="handler">Thread to execute on</param>
        /// <returns>-1 if capture session is null</returns>
        public int Capture(ICaptureRequest request, ICameraCaptureListener listener, IHandler handler)
            => captureSession?.Capture(request, listener, handler) ?? -1;

        /// <summary>
        /// Burst through a list of requests
        /// </summary>
        /// <param name="requests">Requests to burst through</param>
        /// <returns>The results of the capture request</returns>
        public async Task<CaptureEventArgs> CaptureBurst(IList<ICaptureRequest> requests)
        {
            var callback = new SubCCaptureCallback();

            var completed = new TaskCompletionSource<CaptureEventArgs>();

            var completedHandler = new EventHandler<CaptureEventArgs>((s, e) =>
            {
                completed.TrySetResult(e);
            });

            callback.CaptureCompleted += completedHandler;
            callback.CaptureFailed += (s, e) => completedHandler(s, null);

            if (CaptureBurst(requests, callback, handler) == -1)
            {
                return null;
            }

            return await completed.Task;
        }

        /// <summary>
        /// Burst through a list of requests
        /// </summary>
        /// <param name="requests">Requests to burst through</param>
        /// <param name="listener">Listener to call when capture is completed</param>
        /// <param name="handler">Thread to execute on</param>
        /// <returns>-1 if capture session is null</returns>
        public int CaptureBurst(IList<ICaptureRequest> requests, ICameraCaptureListener listener, IHandler handler)
            => captureSession?.CaptureBurst(requests, listener, handler) ?? -1;

        /// <summary>
        /// Close the current capture session
        /// </summary>
        public void Close()
        {
            captureSession?.Close();
        }

        /// <summary>
        /// Create a new capture builder with all the session camera settings, and the preview surface added
        /// </summary>
        /// <exception cref="CameraAccessException">The camera device is no longer connected or has encountered a fatal error</exception>
        /// <exception cref="Java.Lang.IllegalArgumentException">The templateType is not supported by this device</exception>
        /// <exception cref="Java.Lang.IllegalStateException">The camera device has been closed</exception>
        /// <param name="template">Type of capture to initialize</param>
        /// <returns>Builder with all session settings set</returns>
        public ICaptureBuilder CreateCaptureBuilder(SubCCameraTemplate template)
        {
            var builder = camera.CreateCaptureRequest(template);

            builder.Set(
                new SubCCaptureRequestKey(CaptureRequest.ControlCaptureIntent).Key,
                template == SubCCameraTemplate.Preview ?
                    (int)ControlCaptureIntent.Preview :
                    template == SubCCameraTemplate.Record ?
                        (int)ControlCaptureIntent.VideoRecord :
                        template == SubCCameraTemplate.StillCapture ?
                            (int)ControlCaptureIntent.StillCapture :
                            template == SubCCameraTemplate.ZeroShutterLag ?
                                (int)ControlCaptureIntent.ZeroShutterLag :
                                    (int)ControlCaptureIntent.Custom);

            builder.Set(cameraSettings);

            builder.SetTag(template.ToString());

            builder.AddTarget(persistentSurfaces[SurfaceTypes.Preview]);

            return builder;
        }

        /// <summary>
        /// Create a capture session using the persistent and session surfaces
        /// </summary>
        /// <returns>True if the session was created successfully, false otherwise</returns>
        public async Task<bool> CreateSession()
        {
            var tcs = new TaskCompletionSource<bool>();

            var failedHandler = new EventHandler<ICameraCaptureSession>((s, e) =>
            {
                captureSession = e;
                tcs.TrySetResult(false);
            });

            var configuredHandler = new EventHandler<ICameraCaptureSession>((s, e) =>
            {
                captureSession = e;
                tcs.TrySetResult(true);
            });

            var sessionCallback = new CameraSessionCallback();
            sessionCallback.Configured += configuredHandler;
            sessionCallback.ConfigureFailed += failedHandler;

            var sessionSurfaces = new List<ISurface>();
            sessionSurfaces.AddRange(persistentSurfaces.Values);
            sessionSurfaces.AddRange(Surfaces.Values);

            try
            {
                camera.CreateCaptureSession(sessionSurfaces, sessionCallback, handler);
            }
            catch (Exception e)
            {
                tcs.TrySetResult(false);
                throw e;
            }

            return await tcs.Task;
        }

        /// <summary>
        /// Prepare a surface
        /// </summary>
        /// <param name="surface">Surface to use for preparing</param>
        public void Prepare(ISurface surface) => captureSession?.Prepare(surface);

        /// <summary>
        /// Remove the camera setting with the associated key
        /// </summary>
        /// <param name="key">Key of camera setting to remove</param>
        public void Remove(ICaptureRequestKey key)
        {
            cameraSettings.Remove((key as SubCCaptureRequestKey).Key);
        }

        /// <summary>
        /// Remove a persistent surface and restart the session
        /// </summary>
        /// <param name="surfaceType">Surface type to remove</param>
        /// <returns>Empty task</returns>
        public async Task RemovePersistentSurface(SurfaceTypes surfaceType)
        {
            persistentSurfaces.Remove(surfaceType);
            await CreateSession();
        }

        /// <summary>
        /// Remove the surface with the supplied type from the session surfaces
        /// </summary>
        /// <param name="surfaceType">Type of surface to remove</param>
        /// <returns>Empty task</returns>
        public async Task RemoveSurface(SurfaceTypes surfaceType)
        {
            Surfaces.Remove(surfaceType);
            await CreateSession();
        }

        /// <summary>
        /// Repeat the request with all the current settings
        /// </summary>
        /// <exception cref="CameraAccessException">The camera device is no longer connected or has encountered a fatal error</exception>
        /// <exception cref="Java.Lang.IllegalArgumentException">The templateType is not supported by this device</exception>
        /// <exception cref="Java.Lang.IllegalStateException">The camera device has been closed</exception>
        public void Repeat()
        {
            ICaptureBuilder builder;

            // if you have a recording surface then you want to continue recording with the updated settings
            if (Surfaces.ContainsKey(SurfaceTypes.Recording))
            {
                builder = CreateCaptureBuilder(SubCCameraTemplate.Record);
                builder.AddTarget(Surfaces[SurfaceTypes.Recording]);
            }
            else
            {
                builder = CreateCaptureBuilder(SubCCameraTemplate.Preview);

                if (Surfaces.ContainsKey(SurfaceTypes.Burst))
                {
                    builder.AddTarget(Surfaces[SurfaceTypes.Burst]);
                }
            }

            builder.Set(cameraSettings);

            Repeat(builder);
        }

        /// <summary>
        /// Repeat the request with all the current settings
        /// </summary>
        /// <param name="builder">Specific builder to use to build the request</param>
        public void Repeat(ICaptureBuilder builder)
        {
            // always add the preview surface
            builder.AddTarget(persistentSurfaces[SurfaceTypes.Preview]);
            Repeat(builder.Build());
        }

        public async Task RepeatAsync(ICaptureBuilder builder)
        {
            builder.AddTarget(persistentSurfaces[SurfaceTypes.Preview]);

            var request = builder.Build();

            var tcs = new TaskCompletionSource<bool>();

            var callback = new SubCCaptureCallback();
            callback.CaptureCompleted += (s, e) =>
            {
                CaptureCompleted?.Invoke(this, e.CaptureResult);
                tcs.TrySetResult(true);
            };

            callback.CaptureFailed += (s, e) =>
            {
                CaptureFailed?.Invoke(this, EventArgs.Empty);
                tcs.TrySetResult(false);
            };

            if (captureSession.SetRepeatingRequest(request, callback, handler) == -1)
            {
                throw new Exception("Request failed to repeat");
            }

            await tcs.Task;
        }

        /// <summary>
        /// Temporarily stash a surface that was being used in the capture session
        /// </summary>
        /// <param name="surfaceType">Surface to temporarily stash</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws when trying to unstash a surface that was never stashed</exception>
        public void StashSurface(SurfaceTypes surfaceType)
        {
            if (!Surfaces.ContainsKey(surfaceType))
            {
                throw new ArgumentOutOfRangeException($"{nameof(Surfaces)} does not contain surface type {surfaceType}");
            }

            var surface = Surfaces[surfaceType];
            Surfaces.Remove(surfaceType);
            tempSurfaces.Update(surfaceType, surface);
        }

        /// <summary>
        /// Stop the repeating request on the capture session
        /// </summary>
        public void StopRepeating()
        {
            captureSession?.StopRepeating();
        }

        /// <summary>
        /// Take a surface out of the temporary holder and re-add it to the session surfaces
        /// </summary>
        /// <param name="surfaceType">Surface to take out of temporary holder</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws when trying to unstash a surface that was never stashed</exception>
        public void UnStashSurface(SurfaceTypes surfaceType)
        {
            if (!tempSurfaces.ContainsKey(surfaceType))
            {
                throw new ArgumentOutOfRangeException($"{nameof(tempSurfaces)} does not contain surface type {surfaceType}");
            }

            var surface = tempSurfaces[surfaceType];
            tempSurfaces.Remove(surfaceType);
            Surfaces.Update(surfaceType, surface);
        }

        /// <summary>
        /// Update the collection of camera settings
        /// </summary>
        /// <param name="key">Key of object to update</param>
        /// <param name="value">Value to assign</param>
        public void Update(ICaptureRequestKey key, Java.Lang.Object value)
        {
            cameraSettings.Update((key as SubCCaptureRequestKey).Key, value);
        }

        /// <summary>
        /// Add a persistent surface that the capture session will always use
        /// </summary>
        /// <param name="surfaceType">Type of surface to add</param>
        /// <param name="surface">Surface to add</param>
        /// <returns>Empty task</returns>
        public async Task UpdatePersistentSurface(SurfaceTypes surfaceType, ISurface surface)
        {
            persistentSurfaces.Update(surfaceType, surface);
            await CreateSession();
        }

        /// <summary>
        /// Add a new surface to the session surfaces and restart the capture session
        /// </summary>
        /// <param name="surfaceType">Type of surface to add</param>
        /// <param name="surface">Surface to add</param>
        /// <returns>Empty task</returns>
        public async Task<bool> UpdateSurface(SurfaceTypes surfaceType, ISurface surface)
        {
            Surfaces.Update(surfaceType, surface);
            var result = await CreateSession();
            if (!result)
            {
                await RemoveSurface(surfaceType);
            }

            return result;
        }

        /// <summary>
        /// Repeat the request with all the current settings
        /// </summary>
        /// <param name="request">Specific request to repeat</param>
        /// <exception cref="Exception">Throws when request fails to repeat</exception>
        private void Repeat(ICaptureRequest request)
        {
            var callback = new SubCCaptureCallback();
            callback.CaptureCompleted += (s, e) => CaptureCompleted?.Invoke(this, e.CaptureResult);
            callback.CaptureFailed += (s, e) =>
            {
                CaptureFailed?.Invoke(this, EventArgs.Empty);
            };

            if (captureSession.SetRepeatingRequest(request, callback, handler) == -1)
            {
                throw new Exception("Request failed to repeat");
            }
        }
    }
}