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

namespace SubCTools.Droid.IO
{
    public class AuxInfo
    {

        public AuxInfo(int input, string property, string value)
        {
            Property = property;//= AuxInputManager.ToPascalCase(property);
            Input = input;
            Value = value;
        }

        public int Input { get; private set; }
        public string Property { get; private set; }
        public string Value { get; set; }

        public override string ToString() => $"{Property}{Input}:{Value}";
    }
}