using Android.App;
using Android.Content;
using Android.OS;
using System.Reflection;
using Xamarin.Android.NUnitLite;

namespace SubCTools.Droid.UnitTests
{
    [Activity(Label = "SubCTools.Droid.UnitTests", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : TestSuiteActivity
    {
        protected override void OnCreate(Bundle bundle)
        {

            var manager = GetSystemService(Context.CameraService);

            // tests can be inside the main assembly
            AddTest(Assembly.GetExecutingAssembly());
            // or in any reference assemblies
            // AddTest (typeof (Your.Library.TestClass).Assembly);

            // Once you called base.OnCreate(), you cannot add more assemblies.
            base.OnCreate(bundle);
        }
    }
}

