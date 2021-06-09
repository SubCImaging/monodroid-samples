namespace SubCTools.Droid.IO.AuxDevices
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

    public class AuxDeviceArgs
    {
        public AuxDevice OldDevice { get; set; }
        public AuxDevice NewDevice { get; set; }
        public int Input { get; set; }
    }

}