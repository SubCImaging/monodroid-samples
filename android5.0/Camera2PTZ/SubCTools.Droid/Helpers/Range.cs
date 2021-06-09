using Android.Util;

namespace SubCTools.Droid.Helpers
{
    public class Range<T> where T : Java.Lang.Object
    {
        Range range;
        public Range(Range range)
        {
            this.range = range;
        }

        public T Upper => (T)range.Upper;
        public T Lower => (T)range.Lower;

        public override string ToString() => $"{Lower},{Upper}";
    }
}