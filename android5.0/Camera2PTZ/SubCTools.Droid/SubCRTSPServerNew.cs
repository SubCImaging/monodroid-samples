using Android.Content;
using Android.Views;
using AndroidCameraStreaming;
using System;
using System.Threading.Tasks;

namespace SubCTools.Droid
{
    public static class SubCRTSPServerNew
    {
        private const int Height = 360;
        private const int Framerate = 20;
        private const int Bitrate = 1_800_000;
        private static Streaming streaming = new Streaming();
        private static TextureView textureView;
        private static Context context;
        private static bool isStreaming = false;
        private static bool firstStream = true;


        public static void StartStreaming(TextureView textureView, Context context)
        {          
            SubCRTSPServerNew.textureView = textureView;
            SubCRTSPServerNew.context = context;

            if (firstStream)
            {
                textureView.SurfaceTextureSizeChanged += Surface_Changed;
                firstStream = false;
            }

            if (isStreaming)
            {
                return;
            }
            var aspectRatio = ((float)textureView.Width / (float)textureView.Height) + 0.00001f;
            var width = (int)((float)Height * aspectRatio);
            streaming.SetupStreaming(width, Height, Framerate, Bitrate, textureView, context);
            streaming.StartStreaming();
            isStreaming = true;
        }

        private static void Surface_Changed(object sender, TextureView.SurfaceTextureSizeChangedEventArgs e)
        {
            StopStreaming();
        }

        public static void StopStreaming()
        {
           if(!isStreaming)
            {
                return;
            }
            streaming.StopStreaming();
            isStreaming = false;
        }

        public async static void UpdateAspectRatio()
        {
            if(textureView == null)
            {
                return;
            }
            StopStreaming();
            await Task.Delay(1000);
            StartStreaming(textureView, context);
        }
    }
}