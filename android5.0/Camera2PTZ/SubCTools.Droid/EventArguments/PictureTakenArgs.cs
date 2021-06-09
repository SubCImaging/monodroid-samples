using System;

namespace SubCTools.Droid.EventArguments
{
    public class PictureTakenArgs : EventArgs
    {
        public PictureTakenArgs(string path)
        {
            Path = path;
        }

        public string Path { get; }
    }
}