namespace SubCTools.Droid.Converters
{
    using Android.Graphics;
    using SubCTools.Interfaces;

    public class StringToImageFormatType : IPropertyConverter
    {
        public string Format => "Jpeg or Raw";

        public bool TryConvert(object data, out object value)
        {
            value = data;

            var strData = data?.ToString().ToLower() ?? string.Empty;

            if (string.IsNullOrEmpty(strData)
                || (strData != "jpeg"
                && strData != "jpg"
                && strData != "raw"
                && strData != "rawsensor"))
            {
                return false;
            }

            value = strData.Contains("raw") ? ImageFormatType.RawSensor : ImageFormatType.Jpeg;
            return true;
        }

        public bool TryConvertBack(object data, out object value)
        {
            value = (ImageFormatType)data == ImageFormatType.Jpeg ? "Jpeg" : "Raw";
            return true;
        }
    }
}