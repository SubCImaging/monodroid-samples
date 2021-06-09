using SubCTools.Interfaces;
using System;
using System.Text.RegularExpressions;

namespace SubCTools.Droid.Converters
{
    public class MeasurementToCM : IPropertyConverter
    {
        public string Format => "Auto, #, #mm, #cm, #m, #\", #'";

        public bool TryConvert(object data, out object value)
        {
            value = data;

            var input = data.ToString().ToLower();

            const string pattern = @"auto|(\d+(.\d+)?)(mm|cm|m|" + "\"|')?";

            var match = Regex.Match(value.ToString().ToLower(), pattern);

            if (!match.Success) return false;

            var distance = Convert.ToDouble(match.Groups[1].Value);
            var units = match.Groups[3].Value;

            switch (units)
            {
                case "mm":
                    distance /= 10;
                    break;
                case "m":
                    distance *= 100;
                    break;
                case "\"":
                case "in":
                case "inches":
                    distance *= 2.54;
                    break;
                case "'":
                case "ft":
                case "feet":
                    distance *= 30.48;
                    break;
                default:
                    break;
            }

            value = input == "auto" ? (float)-1 : (float)distance;//(float)Math.Pow((700 / distance), (1 / 1.2));
            return true;
        }


        public bool TryConvertBack(object data, out object value)
        {
            value = data;

            if (data.ToString() == "-1")
            {
                value = "Auto";
            }
            //else
            //{
            //    value = Math.Pow(700 / Convert.ToDouble(data), 1.2);
            //}

            return true;
        }
    }
}