namespace SubCTools.Droid.Security
{
    public static class Encryption
    {
        public static string UniqueIdentifier()
        {
            var androidId = Android.Provider.Settings.Secure.GetString(
                Android.App.Application.Context.ContentResolver,
                Android.Provider.Settings.Secure.AndroidId);

            return androidId;
        }
    }
}