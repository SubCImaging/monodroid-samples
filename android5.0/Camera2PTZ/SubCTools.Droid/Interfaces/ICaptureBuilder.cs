using Android.Hardware.Camera2;
using Java.Lang;
using System.Collections.Generic;

namespace SubCTools.Droid.Interfaces
{
    public interface ICaptureBuilder
    {
        List<ISurface> Surfaces { get; }
        Dictionary<ICaptureRequestKey, Object> BuilderProperties { get; }

        void AddTarget(ISurface surface);

        ICaptureRequest Build();

        object Get(ICaptureRequestKey key);

        void RemoveTarget(ISurface surface);

        //void Set(ICaptureRequestKey key, Object value);

        //void Set(Dictionary<ICaptureRequestKey, Object> values);

        void Set(Dictionary<CaptureRequest.Key, Java.Lang.Object> values);

        void Set(CaptureRequest.Key key, Object value);

        void SetTag(Object tag);
    }
}