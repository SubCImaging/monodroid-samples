using Android.Hardware.Camera2;
using Android.Views;
using Java.Lang;
using SubCTools.Droid.Interfaces;
using SubCTools.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SubCTools.Droid.Models
{
    public class SubCCaptureRequest : ICaptureRequest
    {
        public SubCCaptureRequest(CaptureRequest request)
        {
            Request = request;
        }

        public CaptureRequest Request { get; }
    }

    public class SubCCaptureBuilder : ICaptureBuilder
    {
        private readonly CaptureRequest.Builder builder;

        public SubCCaptureBuilder(CaptureRequest.Builder builder)
        {
            this.builder = builder;
        }

        public Dictionary<ICaptureRequestKey, Java.Lang.Object> BuilderProperties { get; } = new Dictionary<ICaptureRequestKey, Java.Lang.Object>();
        public List<ISurface> Surfaces { get; } = new List<ISurface>();

        public void AddTarget(ISurface surface)
        {
            // don't add a surface if it already exists
            if (Surfaces.Contains(surface)) return;

            if (surface is SubCSurface s)
            {
                builder.AddTarget(s.Surface);
            }
            else
            {
                throw new ArgumentException("surface needs to be SubCSurface");
            }

            Surfaces.Add(surface);
        }

        public ICaptureRequest Build() => new SubCCaptureRequest(builder.Build());

        public object Get(ICaptureRequestKey key)
        {
            if (key is SubCCaptureRequestKey k)
            {
                return builder.Get(k.Key);
            }

            throw new IllegalArgumentException("Key need to be SubCCaptureRequestKey");
        }

        public void RemoveTarget(ISurface surface)
        {
            if (surface is SubCSurface s)
            {
                builder.RemoveTarget(s.Surface);
            }
            else
            {
                throw new ArgumentException("surface needs to be SubCSurface");
            }

            Surfaces.Remove(surface);
        }

        //public void Set(ICaptureRequestKey key, Java.Lang.Object value)
        //{
        //    //if (key is SubCCaptureRequestKey k)
        //    //{
        //    builder.Set((key as SubCCaptureRequestKey).Key, value);////k.Key, value);
        //    //}

        //    //BuilderProperties.Update(key, value);
        //}

        //public void Set(Dictionary<ICaptureRequestKey, Java.Lang.Object> values)
        //{
        //    // create a copy of the dictionary so it doesn't change while we're looping through it
        //    var copy = new Dictionary<ICaptureRequestKey, Java.Lang.Object>(values);
        //    foreach (var item in copy)
        //    {
        //        Set(item.Key, item.Value);
        //    }
        //}

        public void Set(Dictionary<CaptureRequest.Key, Java.Lang.Object> values)
        {
            // create a copy of the dictionary so it doesn't change while we're looping through it
            var copy = new Dictionary<CaptureRequest.Key, Java.Lang.Object>(values);
            foreach (var item in copy)
            {
                Set(item.Key, item.Value);
            }
        }

        public void Set(CaptureRequest.Key key, Java.Lang.Object value)
        {
            builder.Set(key, value);
        }

        //public static Java.Lang.Object Cast(object obj)
        //{
        //    var propertyInfo = obj.GetType().GetProperty("Instance");
        //    return propertyInfo == null ? null : propertyInfo.GetValue(obj, null) as Java.Lang.Object;
        //}

        //public static object Cast(Java.Lang.Object obj)
        //{
        //    var propertyInfo = obj.GetType().GetProperty("Instance");
        //    return propertyInfo == null ? null : propertyInfo.GetValue(obj, null) as object;
        //}

        public void SetTag(Java.Lang.Object tag)
        {
            builder.SetTag((Java.Lang.Object)tag);
        }

        public override string ToString() => string.Join("\n", BuilderProperties.Select(b => b.Key.Name + ": " + b.Value.ToString()));
    }
}