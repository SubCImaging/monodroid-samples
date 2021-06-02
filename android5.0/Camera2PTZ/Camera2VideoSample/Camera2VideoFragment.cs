using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Concurrent;
using SubCTools.Droid.Camera;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Camera2PTZ
{
    public class Camera2VideoFragment : Fragment, View.IOnClickListener
    {
        public CaptureRequest.Builder builder;
        public CameraDevice cameraDevice;
        public Semaphore cameraOpenCloseLock = new Semaphore(1);
        public MediaRecorder mediaRecorder;
        public CameraCaptureSession previewSession;

        // AutoFitTextureView for camera preview
        public AutoFitTextureView textureView;

        private const string TAG = "Camera2VideoFragment";
        private Handler backgroundHandler;
        private HandlerThread backgroundThread;

        // Button to record video
        private Button buttonVideo;

        private Button down;
        private Button left;
        private SparseIntArray ORIENTATIONS = new SparseIntArray();
        private CaptureRequest.Builder previewBuilder;
        private Size previewSize;
        private DigitalPTZ ptz;
        private Button right;
        private Size sensorSize;

        // Called when the CameraDevice changes state
        private MyCameraStateCallback stateListener;

        // Handles several lifecycle events of a TextureView
        private MySurfaceTextureListener surfaceTextureListener;

        private Button up;
        private Size videoSize;
        private Button zoomin;
        private Button zoomout;

        public Camera2VideoFragment()
        {
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation0, 90);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation90, 0);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation180, 270);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation270, 180);
            surfaceTextureListener = new MySurfaceTextureListener(this);
            stateListener = new MyCameraStateCallback(this);
        }

        public static Camera2VideoFragment newInstance()
        {
            var fragment = new Camera2VideoFragment();
            fragment.RetainInstance = true;
            return fragment;
        }

        //Configures the neccesary matrix transformation to apply to the textureView
        public void configureTransform(int viewWidth, int viewHeight)
        {
            if (null == Activity || null == previewSize || null == textureView)
                return;

            int rotation = (int)Activity.WindowManager.DefaultDisplay.Rotation;
            var matrix = new Matrix();
            var viewRect = new RectF(0, 0, viewWidth, viewHeight);
            var bufferRect = new RectF(0, 0, previewSize.Height, previewSize.Width);
            float centerX = viewRect.CenterX();
            float centerY = viewRect.CenterY();
            if ((int)SurfaceOrientation.Rotation90 == rotation || (int)SurfaceOrientation.Rotation270 == rotation)
            {
                bufferRect.Offset((centerX - bufferRect.CenterX()), (centerY - bufferRect.CenterY()));
                matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
                float scale = System.Math.Max(
                    (float)viewHeight / previewSize.Height,
                    (float)viewHeight / previewSize.Width);
                matrix.PostScale(scale, scale, centerX, centerY);
                matrix.PostRotate(90 * (rotation - 2), centerX, centerY);
            }
            textureView.SetTransform(matrix);
        }

        public void OnClick(View view)
        {
            switch (view.Id)
            {
                //case Resource.Id.video:
                //    {
                //        if (isRecordingVideo)
                //        {
                //            stopRecordingVideo();
                //        }
                //        else
                //        {
                //            StartRecordingVideo();
                //        }
                //        break;
                //    }

                //case Resource.Id.info:
                //    {
                //        if (null != Activity)
                //        {
                //            new AlertDialog.Builder(Activity)
                //                .SetMessage(Resource.String.intro_message)
                //                .SetPositiveButton(Android.Resource.String.Ok, (Android.Content.IDialogInterfaceOnClickListener)null)
                //                .Show();
                //        }
                //        break;
                //    }
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            ;
            return inflater.Inflate(Resource.Layout.fragment_camera2_video, container, false);
        }

        public override void OnPause()
        {
            CloseCamera();
            StopBackgroundThread();
            base.OnPause();
        }

        public override void OnResume()
        {
            base.OnResume();
            StartBackgroundThread();
            if (textureView.IsAvailable)
                openCamera(textureView.Width, textureView.Height);
            else
                textureView.SurfaceTextureListener = surfaceTextureListener;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            textureView = (AutoFitTextureView)view.FindViewById(Resource.Id.texture);
            zoomin = view.FindViewById<Button>(Resource.Id.zoomin);
            zoomout = view.FindViewById<Button>(Resource.Id.zoomout);
            up = view.FindViewById<Button>(Resource.Id.up);
            down = view.FindViewById<Button>(Resource.Id.down);
            left = view.FindViewById<Button>(Resource.Id.left);
            right = view.FindViewById<Button>(Resource.Id.right);

            zoomin.Click += Zoomin_Click;
            zoomout.Click += Zoomout_Click;
            up.Click += Up_Click;
            down.Click += Down_Click;
            left.Click += Left_Click;
            right.Click += Right_Click;
        }

        //Tries to open a CameraDevice
        public void openCamera(int width, int height)
        {
            if (null == Activity || Activity.IsFinishing)
                return;

            CameraManager manager = (CameraManager)Activity.GetSystemService(Context.CameraService);
            try
            {
                if (!cameraOpenCloseLock.TryAcquire(2500, TimeUnit.Milliseconds))
                    throw new RuntimeException("Time out waiting to lock camera opening.");
                string cameraId = manager.GetCameraIdList()[0];
                CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);
                StreamConfigurationMap map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);

                // var fps = map.GetHighSpeedVideoFpsRanges();
                var a = map.GetHighSpeedVideoSizes();

                foreach (var item in characteristics.AvailableCaptureRequestKeys)
                {
                    System.Console.WriteLine(item.Name);
                }

                //)
                var framerates = (Java.Lang.Object[])characteristics.Get(CameraCharacteristics.ControlAeAvailableTargetFpsRanges);

                foreach (var item in framerates)
                {
                    var f = (Android.Util.Range)item;
                    System.Console.WriteLine(f);
                }

                videoSize = ChooseVideoSize(map.GetOutputSizes(Class.FromType(typeof(MediaRecorder))));
                previewSize = ChooseOptimalSize(map.GetOutputSizes(Class.FromType(typeof(MediaRecorder))), width, height, videoSize);
                int orientation = (int)Resources.Configuration.Orientation;
                if (orientation == (int)Android.Content.Res.Orientation.Landscape)
                {
                    textureView.SetAspectRatio(previewSize.Width, previewSize.Height);
                }
                else
                {
                    textureView.SetAspectRatio(previewSize.Height, previewSize.Width);
                }
                configureTransform(width, height);
                mediaRecorder = new MediaRecorder();
                manager.OpenCamera(cameraId, stateListener, null);

                var streamMap = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                sensorSize = streamMap.GetOutputSizes((int)ImageFormatType.RawSensor).First();
            }
            catch (CameraAccessException)
            {
                Toast.MakeText(Activity, "Cannot access the camera.", ToastLength.Short).Show();
                Activity.Finish();
            }
            catch (NullPointerException)
            {
                var dialog = new ErrorDialog();
                dialog.Show(FragmentManager, "dialog");
            }
            catch (InterruptedException)
            {
                throw new RuntimeException("Interrupted while trying to lock camera opening.");
            }
        }

        //Start the camera preview
        public void startPreview()
        {
            if (null == cameraDevice || !textureView.IsAvailable || null == previewSize)
                return;

            try
            {
                SurfaceTexture texture = textureView.SurfaceTexture;
                //Assert.IsNotNull(texture);
                texture.SetDefaultBufferSize(previewSize.Width, previewSize.Height);
                previewBuilder = cameraDevice.CreateCaptureRequest(CameraTemplate.Record);
                var surfaces = new List<Surface>();
                var previewSurface = new Surface(texture);
                surfaces.Add(previewSurface);
                previewBuilder.AddTarget(previewSurface);

                var recorderSurface = mediaRecorder.Surface;
                surfaces.Add(recorderSurface);
                previewBuilder.AddTarget(recorderSurface);

                cameraDevice.CreateCaptureSession(surfaces, new PreviewCaptureStateCallback(this), backgroundHandler);
                ptz = new DigitalPTZ(previewBuilder, new System.Drawing.Size(sensorSize.Height, sensorSize.Width));
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
            catch (IOException e)
            {
                e.PrintStackTrace();
            }
        }

        //Update the preview
        public void updatePreview()
        {
            if (null == cameraDevice)
                return;

            try
            {
                setUpCaptureRequestBuilder(previewBuilder);
                HandlerThread thread = new HandlerThread("CameraPreview");
                thread.Start();
                previewSession.SetRepeatingRequest(previewBuilder.Build(), null, backgroundHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }

        private Size ChooseOptimalSize(Size[] choices, int width, int height, Size aspectRatio)
        {
            var bigEnough = new List<Size>();
            int w = aspectRatio.Width;
            int h = aspectRatio.Height;
            foreach (Size option in choices)
            {
                if (option.Height == option.Width * h / w &&
                    option.Width >= width && option.Height >= height)
                    bigEnough.Add(option);
            }

            if (bigEnough.Count > 0)
                return (Size)Collections.Min(bigEnough, new CompareSizesByArea());
            else
            {
                Log.Error(TAG, "Couldn't find any suitable preview size");
                return choices[0];
            }
        }

        private Size ChooseVideoSize(Size[] choices)
        {
            foreach (Size size in choices)
            {
                if (size.Width == size.Height * 4 / 3 && size.Width <= 1000)
                    return size;
            }
            Log.Error(TAG, "Couldn't find any suitable video size");
            return choices[choices.Length - 1];
        }

        private void CloseCamera()
        {
            try
            {
                cameraOpenCloseLock.Acquire();
                if (null != cameraDevice)
                {
                    cameraDevice.Close();
                    cameraDevice = null;
                }
                if (null != mediaRecorder)
                {
                    mediaRecorder.Release();
                    mediaRecorder = null;
                }
            }
            catch (InterruptedException e)
            {
                throw new RuntimeException("Interrupted while trying to lock camera closing.");
            }
            finally
            {
                cameraOpenCloseLock.Release();
            }
        }

        private void Down_Click(object sender, EventArgs e)
        {
            ptz.TiltDown();
            updatePreview();
        }

        private void Left_Click(object sender, EventArgs e)
        {
            ptz.PanLeft();
            updatePreview();
        }

        private void Right_Click(object sender, EventArgs e)
        {
            ptz.PanRight();
            updatePreview();
        }

        private void setUpCaptureRequestBuilder(CaptureRequest.Builder builder)
        {
            builder.Set(CaptureRequest.ControlMode, new Java.Lang.Integer((int)ControlMode.Auto));
        }

        private void StartBackgroundThread()
        {
            backgroundThread = new HandlerThread("CameraBackground");
            backgroundThread.Start();
            backgroundHandler = new Handler(backgroundThread.Looper);
        }

        private void StopBackgroundThread()
        {
            backgroundThread.QuitSafely();
            try
            {
                backgroundThread.Join();
                backgroundThread = null;
                backgroundHandler = null;
            }
            catch (InterruptedException e)
            {
                e.PrintStackTrace();
            }
        }

        private void Up_Click(object sender, EventArgs e)
        {
            ptz.TiltUp();
            updatePreview();
        }

        private void Zoomin_Click(object sender, EventArgs e)
        {
            ptz.ZoomIn();
            updatePreview();
        }

        private void Zoomout_Click(object sender, EventArgs e)
        {
            ptz.ZoomOut();
            updatePreview();
        }

        public class ErrorDialog : DialogFragment
        {
            public override Dialog OnCreateDialog(Bundle savedInstanceState)
            {
                var alert = new AlertDialog.Builder(Activity);
                alert.SetMessage("This device doesn't support Camera2 API.");
                alert.SetPositiveButton(Android.Resource.String.Ok, new MyDialogOnClickListener(this));
                return alert.Show();
            }
        }

        // Compare two Sizes based on their areas
        private class CompareSizesByArea : Java.Lang.Object, Java.Util.IComparator
        {
            public int Compare(Java.Lang.Object lhs, Java.Lang.Object rhs)
            {
                // We cast here to ensure the multiplications won't overflow
                if (lhs is Size && rhs is Size)
                {
                    var right = (Size)rhs;
                    var left = (Size)lhs;
                    return Long.Signum((long)left.Width * left.Height -
                        (long)right.Width * right.Height);
                }
                else
                    return 0;
            }
        }

        private class MyDialogOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
        {
            private ErrorDialog er;

            public MyDialogOnClickListener(ErrorDialog e)
            {
                er = e;
            }

            public void OnClick(IDialogInterface dialogInterface, int i)
            {
                er.Activity.Finish();
            }
        }
    }
}