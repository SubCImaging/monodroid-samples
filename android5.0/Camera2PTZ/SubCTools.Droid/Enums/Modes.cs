namespace SubCTools.Droid.Enums
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public enum CameraModes
    {
        Photo, Video
    }

    public enum FocusModes
    {
        Manual, Auto
    }

    public enum FlashModes
    {
        Off, On, Lamp
    }

    public enum Resolution
    {
        NTSC, PAL, FullHD
    }
}