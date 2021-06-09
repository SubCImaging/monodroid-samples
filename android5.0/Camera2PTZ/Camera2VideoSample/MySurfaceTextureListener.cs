using Android.Graphics;
using Android.Views;

namespace Camera2PTZ
{
    public class MySurfaceTextureListener : Java.Lang.Object, TextureView.ISurfaceTextureListener
    {
        private Camera2VideoFragment fragment;

        public MySurfaceTextureListener(Camera2VideoFragment frag)
        {
            fragment = frag;
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface_texture, int width, int height)
        {
            fragment.openCamera();
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface_texture)
        {
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface_texture, int width, int height)
        {
            fragment.configureTransform(width, height);
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface_texture)
        {
        }
    }
}