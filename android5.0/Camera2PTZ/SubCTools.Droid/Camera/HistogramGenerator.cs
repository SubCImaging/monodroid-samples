//using Android.App;
//using Android.Content;
//using Android.Graphics;
//using Android.Hardware.Camera2;
//using Android.Media;
//using Android.OS;
//using Android.Runtime;
//using Android.Views;
//using Android.Widget;
//using SubCTools.Droid.Callbacks;
//using SubCTools.Droid.Listeners;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace SubCTools.Droid.Camera
//{
//    public class HistogramGenerator
//    {
//        private CaptureRequest.Builder previewRequestBuilder;

//        public HistogramGenerator(CameraDevice camera, Surface cameraSurface, PreviewHandler preview)
//        {
//            var previewWidth = 1920;
//            var previewHeight = 1080;
//            var mImageReaderPreviewYUV = ImageReader.NewInstance(previewWidth, previewHeight, ImageFormatType.Yuv420888, 2);

//            var ImageAvailableThread = new HandlerThread("CameraImageAvailable");
//            ImageAvailableThread.Start();
//            var ImageAvailableHandler = new Handler(ImageAvailableThread.Looper);

//            var imageListener = new ImageAvailableListener(null);
//            imageListener.ImageAvailable += ImageListener_ImageAvailable;

//            mImageReaderPreviewYUV.SetOnImageAvailableListener(imageListener, ImageAvailableHandler);

//            var surfaces = new List<Surface>() { mImageReaderPreviewYUV.Surface, cameraSurface };

//            previewRequestBuilder = camera.CreateCaptureRequest(CameraTemplate.Preview);
//            previewRequestBuilder.AddTarget(mImageReaderPreviewYUV.Surface); //Add ImageReader
//            previewRequestBuilder.AddTarget(cameraSurface); //Add surface of SurfaceView

//            var callback = new CameraCaptureListener();
//            callback.CaptureCompleted += Callback_CaptureCompleted;
//            preview.Capture(previewRequestBuilder, callback, surfaces);
//        }

//        private void Callback_CaptureCompleted(object sender, CaptureEventArgs e)
//        {
//            Console.WriteLine("Hist complete");
//        }

//        private void ImageListener_ImageAvailable(object sender, ImageReader.ImageAvailableEventArgs e)
//        {
//            //var bitmap = e.Reader.AcquireLatestImage();
//            Console.WriteLine("Histogram image available");
//        }
//    }
//}