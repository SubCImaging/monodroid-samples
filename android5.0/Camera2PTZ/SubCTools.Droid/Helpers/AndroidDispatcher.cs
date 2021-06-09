using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.Helpers
{
    public class AndroidDispatcher : IDispatcher
    {
        Activity activity;

        public AndroidDispatcher(Activity activity)
        {
            this.activity = activity;
        }

        public void Invoke(Action action)
        {
            activity.RunOnUiThread(action);
        }
    }
}