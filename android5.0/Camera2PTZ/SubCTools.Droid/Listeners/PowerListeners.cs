//-----------------------------------------------------------------------
// <copyright file="PowerListener.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Listeners
{
    using Android.App;
    using Android.Content;
    using Android.Hardware.Usb;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using Attributes;
    using Java.IO;
    using Java.Lang;
    using SubCTools.Attributes;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Timers;

    public class PowerListener : BroadcastReceiver, INotifier
    {
        private bool? isPowerConnected = null;
        private int currentDraw = 0;
        private float voltage = 0;

        private readonly Context context;

        public PowerListener(Context context)
        {
            this.context = context;

            context.RegisterReceiver(this, new IntentFilter(Intent.ActionBatteryChanged));
            context.RegisterReceiver(this, new IntentFilter(Intent.ActionPowerConnected));
            context.RegisterReceiver(this, new IntentFilter(Intent.ActionPowerDisconnected));
        }

        public new event EventHandler<NotifyEventArgs> Notify;

        public event EventHandler<bool> IsPowerConnectedChanged;

        /// <summary>
        /// The battery status
        /// </summary>
        [RemoteState(true)]
        public BatteryStatus Status { get; private set; }

        /// <summary>
        /// The battery state
        /// </summary>
        [RemoteState(true)]
        public string BatteryState { get; private set; }

        /// <summary>
        /// The battery plug
        /// </summary>
        [RemoteState(true)]
        public string BatteryPlug { get; private set; }

        /// <summary>
        /// The battery health status
        /// </summary>
        [RemoteState(true)]
        public BatteryHealth Health { get; private set; }

        /// <summary>
        /// The amount of charge left in the battery
        /// </summary>
        [RemoteState(true)]
        public float BatteryLevel { get; private set; }

        /// <summary>
        /// The temperature of the battery
        /// </summary>
        [RemoteState(true)]
        public float BatteryTemperature { get; private set; }

        /// <summary>
        /// The power source state of the battery
        /// </summary>
        [RemoteState(true)]
        public BatteryPlugged PowerSource { get; private set; }

        /// <summary>
        /// The amount of current being drawn by the device
        /// </summary>
        [RemoteState(true)]
        public int CurrentDraw
        {
            get => currentDraw;
            private set
            {
                if (currentDraw == value) return;
                currentDraw = value;
                OnNotify($"{nameof(CurrentDraw)}:{currentDraw}");
            }
        }

        /// <summary>
        /// The voltage supplied by the battery
        /// </summary>
        [RemoteState(true)]
        public float Voltage
        {
            get => voltage;
            private set
            {
                if (voltage == value) return;
                voltage = value;
                OnNotify($"{nameof(Voltage)}:{voltage}");
            }
        }

        public bool? IsPowerConnected
        {
            get => isPowerConnected;
            set
            {
                if (isPowerConnected == value) return;
                isPowerConnected = value;
                //IsPowerConnectedChanged?.Invoke(this, value);
                OnNotify($"{nameof(isPowerConnected)}:{isPowerConnected}");
            }
        }

        public void Unregister()
        {
            context.UnregisterReceiver(this);
        }

        /// <summary>
        /// Forces a refresh of the value stored for current draw.
        /// </summary>
        [RemoteCommand]
        public void CheckCurrent()
        {
            OnNotify("Checking Current");

            if (int.TryParse(DroidSystem.ShellSync("cat /sys/class/power_supply/battery/current_now"), out var current))
            {
                CurrentDraw = current;
            }
        }

        public override void OnReceive(Context context, Intent intent)
        {
            var action = intent.Action;

            IsPowerConnected = action == Intent.ActionPowerConnected
                ? true : action == Intent.ActionPowerDisconnected
                ? false : IsPowerConnected;

            var status = intent.GetIntExtra(BatteryManager.ExtraStatus, -1);
            var isCharging = status == (int)BatteryStatus.Charging || status == (int)BatteryStatus.Full;

            Status = (BatteryStatus)status;

            var chargePlug = intent.GetIntExtra(BatteryManager.ExtraPlugged, -1);
            var usbCharge = chargePlug == (int)BatteryPlugged.Usb;
            var acCharge = chargePlug == (int)BatteryPlugged.Ac;

            PowerSource = (BatteryPlugged)chargePlug;

            var level = intent.GetIntExtra(BatteryManager.ExtraLevel, -1);
            var scale = intent.GetIntExtra(BatteryManager.ExtraScale, -1);

            Health = (BatteryHealth)intent.GetIntExtra(BatteryManager.ExtraHealth, -1);

            CheckCurrent();

            Voltage = (float)intent.GetIntExtra(BatteryManager.ExtraVoltage, -1) / 1000;

            BatteryTemperature = (float)intent.GetIntExtra(BatteryManager.ExtraTemperature, -1) / 10;

            BatteryLevel = level / (float)scale * 100;
        }

        private void OnNotify(string message, MessageTypes messageType = MessageTypes.Information)
        {
            Notify?.Invoke(this, new NotifyEventArgs(message, messageType));
        }
    }
}