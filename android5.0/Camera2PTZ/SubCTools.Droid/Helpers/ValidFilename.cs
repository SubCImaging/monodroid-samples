//using Android.App;
//using Android.Content;
//using Android.OS;
//using Android.Runtime;
//using Android.Views;
//using Android.Widget;
//using SubCTools.Helpers;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace SubCTools.Droid.Helpers
//{
//    class ValidFilename
//    {
//        //const string Match = "[^a-zA-Z._-]";
//        //const string AntiMatch = "[a-zA-Z._-]";
//        const string BadCharPatten = "[:\\/*?<>|\"]";
//        const string BadCharAntiPatten = "[^:\\/*?<>|\"]";

//        public static bool CheckInvalidFileChar(string filename)
//        {
//            return Regex.Match(filename, BadCharPatten).Success;
//        }

//        public static string UpdateFileName(ref string value)
//        {
//            string warning = "";
//            if (CheckInvalidFileChar(value))
//            {
//                string badChars = Regex.Replace(value, BadCharAntiPatten, "").Aggregate(string.Empty, (c, i) => c + i + ",").Trim(',');
//                value = Regex.Replace(value, BadCharPatten, "");
//                //badChars = badChars.Replace(" ", "Space character");
//                warning = ($"\nInvalid characters Removed : {badChars}");
//            }
//            if (value.Contains("\\")) { warning += warning == "" ? "\nInvalid characters Removed : \\" : ",\\"; }
//            value = value.Replace("\\", String.Empty);//this is here because regex isn't picking up on '\'
//            value = value.Replace(@"../", string.Empty);
//            return ($"File name changed to {value}" + warning);
//        }
//    }
//}

namespace SubCTools.Droid.Helpers
{
    public class ValidFilename
    {
        public static readonly int MaxLength = 100;
    }
}