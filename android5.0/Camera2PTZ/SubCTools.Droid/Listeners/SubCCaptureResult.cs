using Android.Hardware.Camera2;
using SubCTools.Droid.Interfaces;
using SubCTools.Droid.Models;
using System;

namespace SubCTools.Droid.Listeners
{
    internal class SubCCaptureResult : ICaptureResult
    {
        public SubCCaptureResult(CaptureResult result)
        {
            CaptureResult = result;
        }

        public CaptureResult CaptureResult { get; }

        public Java.Lang.Object Get(ICaptureResultKey key)
        {
            if (key is SubCCaptureResultKey k)
            {
                return CaptureResult.Get(k.Key);
            }

            throw new ArgumentException($"Key must be SubCCaptureResult");
        }
    }
}