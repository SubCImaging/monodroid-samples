using Android.Graphics;
using Android.Views;
using SubCTools.Droid.EventArguments;
using System;

namespace SubCTools.Droid.Listeners
{
    public class SurfaceTextureListener : Java.Lang.Object, TextureView.ISurfaceTextureListener
    {
        public event EventHandler<SurfaceTextureArgs> SurfaceTextureAvailable;

        public event EventHandler<SurfaceTextureArgs> SurfaceTextureChanged;

        public event EventHandler<SurfaceTexture> SurfaceTextureUpdated;

        public event EventHandler SurfaceTextureDestroyed;

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            SurfaceTextureAvailable?.Invoke(this, new SurfaceTextureArgs(surface, width, height));
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            SurfaceTextureDestroyed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            SurfaceTextureChanged?.Invoke(this, new SurfaceTextureArgs(surface, width, height));
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
            SurfaceTextureUpdated?.Invoke(this, surface);
        }
    }
}