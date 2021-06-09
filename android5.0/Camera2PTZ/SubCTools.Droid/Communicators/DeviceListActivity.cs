//using Android.App;
//using Android.Content;
//using Android.Hardware.Usb;
//using Android.OS;
//using Hoho.Android.UsbSerial.Driver;

//[assembly: UsesFeature("android.hardware.usb.host")]
//namespace SubCTools.Droid.Communicators
//{
//    //[Activity(Label = "@string/app_name", MainLauncher = true, Icon = "@drawable/icon")]
//    [IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached })]
//    [MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/device_filter")]
//    class DeviceListActivity : Activity
//    {
//        static readonly string TAG = typeof(DeviceListActivity).Name;
//        const string ACTION_USB_PERMISSION = "com.hoho.android.usbserial.examples.USB_PERMISSION";

//        UsbManager usbManager;
//        //ListView listView;
//        //TextView progressBarTitle;
//        //ProgressBar progressBar;

//        //UsbSerialPortAdapter adapter;
//        BroadcastReceiver detachedReceiver;
//        IUsbSerialPort selectedPort;

//        protected override void OnCreate(Bundle bundle)
//        {
//            base.OnCreate(bundle);

//            //SetContentView(Resource.Layout.Main);

//            usbManager = GetSystemService(Context.UsbService) as UsbManager;

//        }

//        protected override async void OnResume()
//        {
//            base.OnResume();

//            //adapter = new UsbSerialPortAdapter(this);
//            //listView.Adapter = adapter;

//            //listView.ItemClick += async (sender, e) =>
//            //{
//            //    await OnItemClick(sender, e);
//            //};

//            //await PopulateListAsync();

//            //register the broadcast receivers
//            //detachedReceiver = new UsbDeviceDetachedReceiver(this);
//            RegisterReceiver(detachedReceiver, new IntentFilter(UsbManager.ActionUsbDeviceDetached));
//        }

//        protected override void OnPause()
//        {
//            base.OnPause();

//            // unregister the broadcast receivers
//            var temp = detachedReceiver; // copy reference for thread safety
//            if (temp != null)
//                UnregisterReceiver(temp);
//        }

//        protected override void OnDestroy()
//        {
//            base.OnDestroy();

//        }

//        //async Task OnItemClick(object sender, AdapterView.ItemClickEventArgs e)
//        //{
//        //    //Log.Info(TAG, "Pressed item " + e.Position);
//        //    //if (e.Position >= adapter.Count)
//        //    //{
//        //        //Log.Info(TAG, "Illegal position.");
//        //    //    return;
//        //    //}

//        //    // request user permisssion to connect to device
//        //    // NOTE: no request is shown to user if permission already granted
//        //    //selectedPort = adapter.GetItem(e.Position);
//        //    var permissionGranted = await usbManager.RequestPermissionAsync(selectedPort.Driver.Device, this);
//        //    if (permissionGranted)
//        //    {
//        //        // start the SerialConsoleActivity for this device
//        //        var newIntent = new Intent(this, typeof(AndroidSerial));//SerialConsoleActivity));
//        //        newIntent.PutExtra(Android.Nfc.NfcAdapter.ExtraTag, new UsbSerialPortInfo(selectedPort));
//        //        StartActivity(newIntent);
//        //    }
//        //}

//        //async Task PopulateListAsync()
//        //{
//        //    //ShowProgressBar();

//        //    //Log.Info(TAG, "Refreshing device list ...");

//        //    var drivers = await FindAllDriversAsync(usbManager);

//        //    //adapter.Clear();
//        //    foreach (var driver in drivers)
//        //    {
//        //        var ports = driver.Ports;
//        //        //Log.Info(TAG, string.Format("+ {0}: {1} port{2}", driver, ports.Count, ports.Count == 1 ? string.Empty : "s"));
//        //        foreach (var port in ports)
//        //            //adapter.Add(port);
//        //    }

//        //    //adapter.NotifyDataSetChanged();
//        //    //progressBarTitle.Text = string.Format("{0} device{1} found", adapter.Count, adapter.Count == 1 ? string.Empty : "s");
//        //    //HideProgressBar();
//        //    //Log.Info(TAG, "Done refreshing, " + adapter.Count + " entries found.");
//        //}

//        //void ShowProgressBar()
//        //{
//        //    progressBar.Visibility = ViewStates.Visible;
//        //    //progressBarTitle.Text = GetString(Resource.String.refreshing);
//        //}

//        //void HideProgressBar()
//        //{
//        //    progressBar.Visibility = ViewStates.Invisible;
//        //}

//        #region UsbSerialPortAdapter implementation

//        //class UsbSerialPortAdapter : ArrayAdapter<IUsbSerialPort>
//        //{
//        //    public UsbSerialPortAdapter(Context context)
//        //        : base(context, global::Android.Resource.Layout.SimpleExpandableListItem2)
//        //    {
//        //    }

//        //    public override View GetView(int position, View convertView, ViewGroup parent)
//        //    {
//        //        var row = convertView;
//        //        if (row == null)
//        //        {
//        //            var inflater = Context.GetSystemService(Context.LayoutInflaterService) as LayoutInflater;
//        //            row = inflater.Inflate(global::Android.Resource.Layout.SimpleListItem2, null);
//        //        }

//        //        var port = this.GetItem(position);
//        //        var driver = port.Driver;
//        //        var device = driver.Device;

//        //        //var title = string.Format("Vendor {0} Product {1}",
//        //        //    HexDump.ToHexString((short)device.VendorId),
//        //        //    HexDump.ToHexString((short)device.ProductId));
//        //        //row.FindViewById<TextView>(global::Android.Resource.Id.Text1).Text = title;

//        //        //var subtitle = device.Class.SimpleName;
//        //        //row.FindViewById<TextView>(global::Android.Resource.Id.Text2).Text = subtitle;

//        //        return row;
//        //    }
//        //}

//        #endregion

//        #region UsbDeviceDetachedReceiver implementation

//        //class UsbDeviceDetachedReceiver
//        //    : BroadcastReceiver
//        //{
//        //    readonly string TAG = typeof(UsbDeviceDetachedReceiver).Name;
//        //    readonly DeviceListActivity activity;

//        //    public UsbDeviceDetachedReceiver(DeviceListActivity activity)
//        //    {
//        //        this.activity = activity;
//        //    }

//        //    public override void OnReceive(Context context, Intent intent)
//        //    {
//        //        var device = intent.GetParcelableExtra(UsbManager.ExtraDevice) as UsbDevice;

//        //        //Log.Info(TAG, "USB device detached: " + device.DeviceName);

//        //        activity.PopulateListAsync();
//        //    }
//        //}

//        #endregion
//    }
//}