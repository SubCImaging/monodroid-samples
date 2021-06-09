namespace SubCTools.Droid.IO.AuxDevices
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Attributes;
    using SubCTools.Communicators;
    using SubCTools.Droid.Communicators;
    using SubCTools.Helpers;
    using SubCTools.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class KongsbergPanTiltAux : HexController
    {
        private readonly KongsbergPanTilt k;

        private int pan = 0;

        private int tilt = 0;

        public KongsbergPanTiltAux(
            AndroidSerial serial,
            int input,
            TeensyListener listener)
            : base(serial, input, listener)
        {
            k = new KongsbergPanTilt(new TeensyHexCommunicator(serial, input, listener));

            k.PanChanged += (_, e) => Pan = e;
            k.TiltChanged += (_, e) => Tilt = e;
        }

        [RemoteState]
        public int Pan
        {
            get => pan;
            set
            {
                if (pan != value)
                {
                    pan = value;
                    OnNotify($"{nameof(Pan)}:{pan}");
                }
            }
        }

        [RemoteState]
        public int Tilt
        {
            get => tilt;
            set
            {
                if (tilt != value)
                {
                    tilt = value;
                    OnNotify($"{nameof(Tilt)}:{tilt}");
                }
            }
        }

        [RemoteCommand]
        public void KDown()
        {
            k.StopAll();
            k.TiltDown();
        }

        [RemoteCommand]
        public void KLeft()
        {
            k.StopAll();
            k.PanLeft();
        }

        [RemoteCommand]
        public void KRight()
        {
            k.StopAll();
            k.PanRight();
        }

        [RemoteCommand]
        public void KStop()
        {
            k.StopAll();
        }

        [RemoteCommand]
        public void KUp()
        {
            k.StopAll();
            k.TiltUp();
        }

        [RemoteCommand]
        public void PanGoto(int degrees)
        {
            k.StopAll();
            k.PanGoto(degrees);
        }

        [RemoteCommand]
        public void TiltGoto(int degrees)
        {
            k.StopAll();
            k.TiltGoto(degrees);
        }

        protected override void AuxDataReceived(AuxData e)
        {
            //appender.Append(e.Data.ToHex().Replace(" ", string.Empty));
        }

        protected override void Connected()
        {
            // ignore
        }
    }
}