using Android.Graphics;
using System;

namespace SubCTools.Droid.EventArguments
{
    public class SurfaceTextureArgs : EventArgs
    {
        public SurfaceTextureArgs(SurfaceTexture texture)
            : this(texture, 0, 0)
        {

        }

        public SurfaceTextureArgs(SurfaceTexture texture, int width, int height)
        {
            Texture = texture;
            Width = width;
            Height = height;
        }

        public SurfaceTexture Texture { get; }
        public int Width { get; }
        public int Height { get; }
    }
}