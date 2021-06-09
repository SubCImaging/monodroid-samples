using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Util;
using Android.Views;
using NUnit.Framework;
using SubCTools.Droid.Camera;
using SubCTools.Droid.EventArguments;
using SubCTools.Droid.Interfaces;
using System;

namespace SubCTools.Droid.UnitTests.Camera
{
    public class DummyImageReaderGrabber : IImageReaderGrabber
    {
        public IImageReader NewInstance(int width, int height, ImageFormatType format, int maxImages)
        {
            return null;
        }
    }


    public class DummyImageReader : IImageReader
    {
        public int Width => 1920;

        public int Height => 1080;

        public ImageFormatType ImageFormat => ImageFormatType.Jpeg;

        public Surface Surface => null;

        ImageReader.IOnImageAvailableListener listener;

        public Image AcquireLatestImage()
        {
            return null;
        }

        public void Close()
        {

        }

        public void SetOnImageAvailableListener(ImageReader.IOnImageAvailableListener listener, Handler handler)
        {
            this.listener = listener;
        }
    }

    public class DummyImageListener : IImageListener
    {
        public IntPtr Handle => throw new NotImplementedException();

        public event EventHandler<ImageAvailableEventArgs> ImageAvailable;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void OnImageAvailable(ImageReader reader)
        {
            ImageAvailable?.Invoke(this, new ImageAvailableEventArgs(reader.AcquireLatestImage(), new System.IO.FileInfo("test")));
        }
    }

    public class DummyCaptureListener : ICameraCaptureListener
    {
        public event EventHandler<CaptureEventArgs> CaptureCompleted;
        public event EventHandler<CaptureEventArgs> CaptureProgressed;
        public event EventHandler<CaptureFailure> CaptureFailed;
        public event EventHandler CaptureStarted;
        public event EventHandler SequenceCompleted;
    }

    [TestFixture]
    public class StillHandlerTest
    {
        StillHandler stillHandler;
        public StillHandlerTest()
        {
            stillHandler = new StillHandler(
                new DummyImageReaderGrabber(),
                new DummyImageListener(),
                new DummyCaptureListener(),
                new[] { new Size(1920, 1080) },
                new[] { new Size(1920, 1080) },
                null);
        }

        [Test]
        public void NotNull()
        {
            Assert.NotNull(stillHandler);
        }

        [Test]
        public void Resolutions()
        {

        }

    }
}