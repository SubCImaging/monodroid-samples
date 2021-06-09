using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.IO
{
    public class HexInfo
    {
        /// <summary>
        /// Aux input that the data was received
        /// </summary>
        public int From { get; set; }

        /// <summary>
        /// Data received
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Byte array of hex of the data
        /// </summary>
        public byte[] Hex => Strings.HexToByteArray(Data);

        /// <summary>
        /// Ascii representation of the data
        /// </summary>
        /// <returns>Ascii representation of the data</returns>
        public override string ToString() => Data.HexToAscii();
    }
}