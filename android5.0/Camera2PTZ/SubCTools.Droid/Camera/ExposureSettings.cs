//-----------------------------------------------------------------------
// <copyright file="ExposureSettings.cs" company="SubCImaging">
// Copyright (c) SubCImaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Camera
{
    using Android.Hardware.Camera2;
    using Android.Hardware.Camera2.Params;
    using Java.Lang;
    using Newtonsoft.Json;
    using SubCTools.Attributes;
    using SubCTools.Droid.Converters;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Droid.Models;
    using SubCTools.Enums;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExposureSettings" /> class.
    /// </summary>
    public class ExposureSettings : DroidBase
    {
        private const string AddNewPresetString = "Add New Preset...";

        /// <summary>
        /// Aquorea MK2 white balance preset
        /// </summary>
        private readonly WhiteBalancePreset aquoreaMK2 = new WhiteBalancePreset(1.875f, 0.91f, 1.73f, "Aquorea MK2");

        /// <summary>
        /// Aquorea MK3 white balance preset
        /// </summary>
        private readonly WhiteBalancePreset aquoreaMK3 = new WhiteBalancePreset(1.875f, 0.89f, 1.81f, "Aquorea MK3");

        /// <summary>
        /// Capture session to update exposure on
        /// </summary>
        private readonly SubCCaptureSession captureSession;

        /// <summary>
        /// List of available exposure times
        /// </summary>
        private readonly IEnumerable<long> exposureTimes;

        /// <summary>
        /// The 'manual' while balance item
        /// </summary>
        private readonly Tuple<string, bool> manualWhiteBalance = new Tuple<string, bool>("Manual", true);

        /// <summary>
        /// A collection of readonly presets that we added to the system
        /// </summary>
        private readonly string[] readOnlyPresets = {
            "Aquorea MK2", "Aquorea MK3"
        };

        /// <summary>
        /// A collection of readonly presets that we added to the system
        /// </summary>
        private readonly string[] ReadOnlyPresets = {
            "Aquorea MK2", "Aquorea MK3"
        };

        private readonly WhiteBalancePreset shallowWaterPreset = new WhiteBalancePreset(1.634f, 0.571f, 1.248f, "Shallow Water");

        /// <summary>
        /// Current auto exposure state
        /// </summary>
        private ControlAEState autoExposureState;

        /// <summary>
        /// Value of the blue channel for white balance
        /// </summary>
        private float blueChannel = -1;

        /// <summary>
        /// Current exposure value
        /// </summary>
        private int exposure;

        /// <summary>
        /// Number of exposure stops for strobe compensation
        /// </summary>
        private int exposureStops;

        /// <summary>
        /// Green channel for white balance
        /// </summary>
        private float greenChannel = -1;

        /// <summary>
        /// True if auto exposure is locked, false if not
        /// </summary>
        private bool isAELocked;

        /// <summary>
        /// True if is in auto exposure, false if in manual exposure
        /// </summary>
        private bool isAutoExposure = true;

        /// <summary>
        /// Current ISO value
        /// </summary>
        private int iso;

        /// <summary>
        /// Red channel of the white balance
        /// </summary>
        private float redChannel = -1;

        /// <summary>
        /// The white balance value that is selected
        /// </summary>
        private string selectedWhiteBalance;

        /// <summary>
        /// The name of the white balance value that is currently selected
        /// </summary>
        private Tuple<string, bool> selectedWhiteBalanceTuple;

        /// <summary>
        /// Current camera shutter speed
        /// </summary>
        private long shutterSpeed;

        /// <summary>
        /// ISO to use when strobing
        /// </summary>
        private int strobeISO;

        /// <summary>
        /// Shutter speed to use when strobing
        /// </summary>
        private long strobeShutter;

        /// <summary>
        /// Selected white balance mode
        /// </summary>
        private ControlAwbMode whiteBalance = ControlAwbMode.Auto;

        /// <summary>
        /// All of the available white balance presets
        /// </summary>
        private List<WhiteBalancePreset> whiteBalancePresets = new List<WhiteBalancePreset>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExposureSettings" /> class.
        /// </summary>
        /// <param name="characteristics">
        /// Camera characteristics used to get iso and shutter ranges
        /// </param>
        /// <param name="settings"> Settings service to save all settings </param>
        /// <param name="captureSession"> Capture session to set values </param>
        public ExposureSettings(
            CameraCharacteristics characteristics,
            ISettingsService settings,
            SubCCaptureSession captureSession)
            : this(
                  new Range<Integer>(characteristics.Get(CameraCharacteristics.ControlAeCompensationRange) as Android.Util.Range),
                  new Range<Integer>(characteristics.Get(CameraCharacteristics.SensorInfoSensitivityRange) as Android.Util.Range),
                  new Range<Long>(characteristics.Get(CameraCharacteristics.SensorInfoExposureTimeRange) as Android.Util.Range),
                  settings,
                  captureSession)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExposureSettings" /> class.
        /// </summary>
        /// <param name="exposureRange"> Range of exposure values </param>
        /// <param name="isoRange"> Range of iso values </param>
        /// <param name="shutterRange"> Range of shutter speeds </param>
        /// <param name="settings"> Settings service to save all settings </param>
        /// <param name="captureSession"> Capture session to set values </param>
        public ExposureSettings(
            Range<Integer> exposureRange,
            Range<Integer> isoRange,
            Range<Long> shutterRange,
            ISettingsService settings,
            SubCCaptureSession captureSession)
            : base(settings)
        {
            this.captureSession = captureSession;

            ExposureRange = exposureRange;
            ISORange = isoRange;
            ShutterRange = shutterRange;

            // set the white balance mode by default
            captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ColorCorrectionMode), (int)ColorCorrectionMode.TransformMatrix);

            var shutter = ShutterSpeeds.Generate((long)ShutterRange.Lower, (long)ShutterRange.Upper);
            exposureTimes = shutter.Select(f => f.ToNanoseconds());

            // generate all the selectable ISO values
            ISOValues = GenerateIsoStops(MinISO, MaxISO);

            captureSession.CaptureCompleted += (s, e) => PreviewHandler_CaptureCompleted(e);
        }

        /// <summary>
        /// Event to fire when the exposure changes
        /// </summary>
        public event EventHandler<double> ExposureChanged;

        /// <summary>
        /// Event to fire when IsAutoExposure changes
        /// </summary>
        public event EventHandler<bool> IsAutoExposureChanged;

        /// <summary>
        /// Event to fire when ISO changes
        /// </summary>
        public event EventHandler<double> ISOChanged;

        /// <summary>
        /// Event to fire when shutter speed fires
        /// </summary>
        public event EventHandler<double> ShutterSpeedChanged;

        /// <summary>
        /// Event to fire when white balance changes
        /// </summary>
        public event EventHandler<ControlAwbMode> WhiteBalanceChanged;

        /// <summary>
        /// Gets the value for the auto exposure state. E.g. Searching, Locked...
        /// </summary>
        [RemoteState(true)]
        public ControlAEState AEState
        {
            get => autoExposureState;
            private set
            {
                if (autoExposureState != value)
                {
                    autoExposureState = value;
                    OnNotify($"{nameof(AEState)}:{AEState}");
                }
            }
        }

        [RemoteState]
        public string AllAvailableWhiteBalances => JsonConvert.SerializeObject(GetAllWhiteBalances());

        /// <summary>
        /// Gets blue channel of white balance
        /// </summary>
        [Savable]
        [RemoteState(true)]
        [CancelWhen(nameof(WhiteBalance), ComparisonOperators.NotEqualTo, "Off")]
        public float B
        {
            get => blueChannel;
            private set
            {
                if (Set(nameof(B), ref blueChannel, value))
                {
                    captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ColorCorrectionGains), new RggbChannelVector((float)R, (float)G, (float)G, B));
                    OnNotify($"{nameof(B)}:{B}");
                }
            }
        }

        /// <summary>
        /// Gets range of exposure values, usually -12 - +12
        /// </summary>
        public Range<Integer> ExposureRange { get; }

        /// <summary>
        /// Gets or sets number of stops to perform when compensating for strobe. Range 0 to 15.
        /// </summary>
        [Savable]
        [RemoteState]
        public int ExposureStops
        {
            get => exposureStops;
            set
            {
                Set(nameof(ExposureStops), ref exposureStops, value);
            }
        }

        /// <summary>
        /// Gets or sets exposure offset for when in Auto Exposure
        /// </summary>
        [Savable]
        [RemoteState(true)]
        [CancelWhen(nameof(IsAutoExposure), false)]
        public int ExposureValue
        {
            get => exposure;
            set
            {
                value = value.Clamp(ExposureRange.Lower.IntValue(), ExposureRange.Upper.IntValue());

                if (!Set(nameof(ExposureValue), ref exposure, value))
                {
                    return;
                }

                captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ControlAeExposureCompensation), ExposureValue);

                ExposureChanged?.Invoke(this, value);
                OnNotify(nameof(ExposureValue) + ":" + value);
            }
        }

        /// <summary>
        /// Gets green channel of the white balance
        /// </summary>
        [Savable]
        [RemoteState(true)]
        [CancelWhen(nameof(WhiteBalance), ComparisonOperators.NotEqualTo, "Off")]
        public float G
        {
            get => greenChannel;
            private set
            {
                if (Set(nameof(G), ref greenChannel, value))
                {
                    captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ColorCorrectionGains), new RggbChannelVector((float)R, (float)G, (float)G, (float)B));
                    OnNotify($"{nameof(G)}:{G}");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether returns true of the Auto Exposure is locked,
        /// false otherwise
        /// </summary>
        [Savable]
        [RemoteState(true)]
        public bool IsAELocked
        {
            get => isAELocked;
            set
            {
                if (!Set(nameof(IsAELocked), ref isAELocked, value))
                {
                    return;
                }

                OnNotify("IsAELocked:" + value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether returns true if exposure is in auto mode, false if it's
        /// in manual
        /// </summary>
        [Savable]
        [RemoteState(true)]
        public bool IsAutoExposure
        {
            get => isAutoExposure;
            private set
            {
                if (!Set(nameof(IsAutoExposure), ref isAutoExposure, value))
                {
                    return;
                }

                captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ControlAeMode), value ? (int)ControlAEMode.On : (int)ControlAEMode.Off);

                IsAutoExposureChanged?.Invoke(this, value);
                OnNotify("IsAutoExposure:" + value);
            }
        }

        /// <summary>
        /// Gets or sets iSO value
        /// </summary>
        [Savable]
        [RemoteState(true)]
        public int ISO
        {
            // get the sensor reading if you're auto
            get => iso;
            set
            {
                // get what the shutter was before ae was disabled
                value = value.Clamp(ISORange.Lower.IntValue(), ISORange.Upper.IntValue());
                if (!Set(nameof(ISO), ref iso, value))
                {
                    return;
                }

                captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.SensorSensitivity), value);

                ISOChanged?.Invoke(this, value);
                OnNotify($"{nameof(ISO)}:{value}");
            }
        }

        /// <summary>
        /// Gets range of available ISO values
        /// </summary>
        public Range<Integer> ISORange { get; }

        /// <summary>
        /// Gets cSV of ISO values
        /// </summary>
        public IEnumerable<int> ISOValues { get; }

        /// <summary>
        /// Gets maximum exposure value
        /// </summary>
        public int MaxExposureValue => ExposureRange.Upper.IntValue();

        /// <summary>
        /// Gets maximum ISO value
        /// </summary>
        public int MaxISO => ISORange.Upper.IntValue();

        /// <summary>
        /// Gets maximum shutter speed
        /// </summary>
        public int MaxShutter => ShutterRange.Upper.IntValue();

        /// <summary>
        /// Gets minimum Exposure value
        /// </summary>
        public int MinExposureValue => ExposureRange.Lower.IntValue();

        /// <summary>
        /// Gets minimum ISO value
        /// </summary>
        public int MinISO => ISORange.Lower.IntValue();

        /// <summary>
        /// Gets minimum shutter value
        /// </summary>
        public int MinShutter => ShutterRange.Lower.IntValue();

        /// <summary>
        /// Gets red channel of the white balance
        /// </summary>
        [Savable]
        [RemoteState(true)]
        [CancelWhen(nameof(WhiteBalance), ComparisonOperators.NotEqualTo, "Off")]
        public float R
        {
            get => redChannel;
            private set
            {
                if (Set(nameof(R), ref redChannel, value))
                {
                    captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ColorCorrectionGains), new RggbChannelVector(R, G, G, B));
                    OnNotify($"{nameof(R)}:{R}");
                }
            }
        }

        /// <summary>
        /// Gets or sets the white balance value that is selected
        /// </summary>
        [RemoteState]
        [Savable]
        public string SelectedWhiteBalance
        {
            get => JsonConvert.SerializeObject(selectedWhiteBalanceTuple);

            set
            {
                Set(nameof(SelectedWhiteBalance), ref selectedWhiteBalance, value);
                selectedWhiteBalanceTuple = JsonConvert.DeserializeObject<Tuple<string, bool>>(value);
                OnNotify($"{nameof(SelectedWhiteBalance)}:{value}");
            }
        }

        /// <summary>
        /// Gets min and max range for shutter speeds
        /// </summary>
        public Range<Long> ShutterRange { get; }

        /// <summary>
        /// Gets or sets shutter speed in nano seconds
        /// </summary>
        [Savable]
        [RemoteState(true)]
        public long ShutterSpeed
        {
            // get the sensor reading if you're auto
            get => shutterSpeed;
            set
            {
                value = exposureTimes.Nearest(value);
                if (!Set(nameof(ShutterSpeed), ref shutterSpeed, value))
                {
                    return;
                }

                captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.SensorExposureTime), (long)value);

                ShutterSpeedChanged?.Invoke(this, value);
                OnNotify($"{nameof(ShutterSpeed)}:{value}");
            }
        }

        /// <summary>
        /// Gets or sets ISO to use when a strobe is connected
        /// </summary>
        [RemoteState(true, false, true)]
        public int StrobeISO
        {
            get => strobeISO;
            set
            {
                if (Set(nameof(StrobeISO), ref strobeISO, value))
                {
                    OnNotify($"{nameof(StrobeISO)}:{StrobeISO}");
                }
            }
        }

        /// <summary>
        /// Gets or sets shutter speed to use when I strobe is connected
        /// </summary>
        [RemoteState(true, false, true)]
        public long StrobeShutter
        {
            get => strobeShutter;
            set
            {
                if (Set(nameof(StrobeShutter), ref strobeShutter, value))
                {
                    OnNotify($"{nameof(StrobeShutter)}:{StrobeShutter}");
                }
            }
        }

        /// <summary>
        /// Gets or sets selected White balance mode
        /// </summary>
        [Savable]
        [RemoteState(true)]
        [PropertyConverter(typeof(StringToAWB))]
        public ControlAwbMode WhiteBalance
        {
            get => whiteBalance;
            set
            {
                if (!Set(nameof(WhiteBalance), ref whiteBalance, value))
                {
                    return;
                }

                captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ControlAwbMode), (int)value);

                WhiteBalanceChanged?.Invoke(this, value);
                OnNotify($"{nameof(WhiteBalance)}:{value}");
            }
        }

        /// <summary>
        /// Gets the available range of white balance presets
        /// </summary>
        public IEnumerable<string> WhiteBalancePresetNames => GetWhiteBalancePresetNames();

        /// <summary>
        /// Gets the available range of white balance values
        /// </summary>
        public string WhiteBalanceRange => string.Join(",", System.Enum.GetNames(typeof(ControlAwbMode)).Where(s => s != ControlAwbMode.WarmFluorescent.ToString())).Replace("Off", "Manual");

        /// <summary>
        /// Apply the values from a specified white balance preset
        /// </summary>
        /// <param name="presetName"> The preset which you wish to apply </param>
        public void ApplyWhiteBalancePreset(string presetName)
        {
            var preset = whiteBalancePresets.FirstOrDefault(p => p.PresetName == presetName);
            if (preset == default(WhiteBalancePreset))
            {
                OnNotify("White Balance Preset: " + presetName + " does not exist.");
                return;
            }

            //UpdateWhiteBalance(manualWhiteBalance.Item1);
            UpdateR(preset.R);
            UpdateG(preset.G);
            UpdateB(preset.B);
        }

        /// <summary>
        /// Enabled auto exposure
        /// </summary>
        [RemoteCommand]
        [Alias("AutoExpose")]
        public void AutoExposure()
        {
            if (!IsAutoExposure)
            {
                IsAutoExposure = true;
                captureSession.Repeat();
            }

            OnNotify("Exposure:Auto");
            OnNotify($"{nameof(IsAutoExposure)}:{IsAutoExposure}");
            OnNotify($"{nameof(ISO)}:{ISO}");
            OnNotify($"{nameof(ShutterSpeed)}:{ShutterSpeed}");
            OnNotify($"{nameof(ExposureValue)}:{ExposureValue}");
        }

        /// <summary>
        /// Clears all the custom white balance presets
        /// </summary>
        [RemoteCommand]
        public void ClearWhiteBalancePresets()
        {
            whiteBalancePresets.Clear();
            foreach (var item in Settings.LoadAll("WhiteBalancePresets"))
            {
                Settings.Remove($@"WhiteBalancePresets/{item.Name}");
            }

            whiteBalancePresets.Add(aquoreaMK2);
            whiteBalancePresets.Add(aquoreaMK3);
            whiteBalancePresets.Add(shallowWaterPreset);
            NotifyWhiteBalancePresetChange();
        }

        /// <summary>
        /// Adjust the strobe values relative to the current ISO and Shuttter with the set number of stops
        /// </summary>
        [RemoteCommand]
        public void Compensate()
        {
            Compensate(ExposureStops);
        }

        /// <summary>
        /// Adjust the strobe values relative to the current ISO and Shuttter when strobing with the
        /// given number of stops (0 to 15). of stops
        /// </summary>
        /// <param name="stops"> Number of stops to step down the exposure level during strobe. </param>
        [RemoteCommand]
        public void Compensate(int stops)
        {
            // reset compensated values
            StrobeShutter = exposureTimes.Nearest(ShutterSpeed);
            StrobeISO = ISOValues.Nearest(ISO);

            for (int i = 0; i < System.Math.Abs(stops); i++)
            {
                // get to 1/30 shutter
                if (StrobeShutter > new Fraction(1, 30).ToNanoseconds())
                {
                    StrobeShutter = Stop(-1, StrobeShutter, exposureTimes.ToList());
                    continue;
                }

                // lower ISO to 50
                if (StrobeISO > 50)
                {
                    StrobeISO = Stop(-1, StrobeISO, ISOValues.ToList());
                    continue;
                }

                // increase shutter speed
                StrobeShutter = Stop(-1, StrobeShutter, exposureTimes.ToList());
            }
        }

        /// <summary>
        /// Decrement the current exposure value
        /// </summary>
        [RemoteCommand]
        [CancelWhen(nameof(IsAutoExposure), false)]
        public void DecreaseExposureValue()
        {
            UpdateExposure(ExposureValue - 1);
        }

        /// <summary>
        /// Increment the current exposure value
        /// </summary>
        [RemoteCommand]
        [CancelWhen(nameof(IsAutoExposure), false)]
        public void IncreaseExposureValue()
        {
            UpdateExposure(ExposureValue + 1);
        }

        /// <summary>
        /// Loads the settings from the settings service
        /// </summary>
        public override void LoadSettings()
        {
            // Call base load settings first and then do custom loading tasks that might overwrite
            // the base functionality
            base.LoadSettings();

            // this is likely the first time you're running if you don't have an is auto exposure
            // value set
            if (Settings.TryLoad(nameof(IsAutoExposure), out bool isAuto) && !isAuto)
            {
                var shutter = Settings.TryLoad(nameof(ShutterSpeed), out long tempShutter) ? tempShutter : ShutterSpeed;
                var iso = Settings.TryLoad(nameof(ISO), out int tempIso) ? tempIso : ISO;

                // need to switch to auto exposure first, there's a bug that prevent auto focus from
                // mov AutoExposure();
                ManualExposure();

                ISO = iso;
                ShutterSpeed = shutter;
            }
            else
            {
                if (Settings.TryLoad(nameof(ExposureValue), out int tempExposure))
                {
                    ExposureValue = tempExposure;
                }
            }

            if (Settings.TryLoad(nameof(WhiteBalance), out string wb))
            {
                if (new StringToAWB().TryConvert(wb, out object mode))
                {
                    WhiteBalance = (ControlAwbMode)mode;

                    if (WhiteBalance == ControlAwbMode.Off)
                    {
                        if (Settings.TryLoad(nameof(R), out float r))
                        {
                            R = r;
                        }

                        if (Settings.TryLoad(nameof(G), out float g))
                        {
                            G = g;
                        }

                        if (Settings.TryLoad(nameof(B), out float b))
                        {
                            B = b;
                        }
                    }
                }

                whiteBalancePresets.Clear();

                foreach (var item in Settings.LoadAll("WhiteBalancePresets"))
                {
                    whiteBalancePresets.Add(new WhiteBalancePreset(
                        float.Parse(item.Attributes["R"]),
                        float.Parse(item.Attributes["G"]),
                        float.Parse(item.Attributes["B"]),
                        item.Name));
                }

                whiteBalancePresets.Add(aquoreaMK2);
                whiteBalancePresets.Add(aquoreaMK3);
                whiteBalancePresets.Add(shallowWaterPreset);
            }

            if (Settings.TryLoad(nameof(SelectedWhiteBalance), out string val))
            {
                if (val.IsValidJson())
                {
                    SelectedWhiteBalance = val;
                }
            }
        }

        /// <summary>
        /// Locks Auto Exposure values
        /// </summary>
        [RemoteCommand]
        public void LockAE()
        {
            IsAELocked = true;
            captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ControlAeLock), IsAELocked);
            captureSession.Repeat();
        }

        /// <summary>
        /// Enable manual exposure
        /// </summary>
        [RemoteCommand]
        [Alias("ManualExpose")]
        public void ManualExposure()
        {
            if (IsAutoExposure)
            {
                var shutter = ShutterSpeed;
                IsAutoExposure = false;

                ISO = ISOValues.Nearest(ISO);
                ShutterSpeed = shutter;

                captureSession.Repeat();
            }

            OnNotify("Exposure:Manual");
            OnNotify($"{nameof(IsAutoExposure)}:{IsAutoExposure}");
            OnNotify($"{nameof(ISO)}:{ISO}");
            OnNotify($"{nameof(ShutterSpeed)}:{ShutterSpeed}");
            OnNotify($"{nameof(ExposureValue)}:{ExposureValue}");
        }

        /// <summary>
        /// Removes a white balance preset from the list of available presets
        /// </summary>
        /// <param name="presetName"> The name of the preset you wish to remove </param>
        [RemoteCommand]
        public void RemoveWhiteBalancePreset(string presetName)
        {
            var preset = whiteBalancePresets.FirstOrDefault(p => p.PresetName == presetName);

            if (whiteBalancePresets.Contains(preset))
            {
                whiteBalancePresets.Remove(preset);
            }

            foreach (var item in Settings.LoadAll("WhiteBalancePresets"))
            {
                if (item.Name == presetName)
                {
                    Settings.Remove($@"WhiteBalancePresets/{item.Name}");
                }
            }

            LoadSettings();

            OnNotify($"Removed white balance preset -> {preset.PresetName}");
            NotifyWhiteBalancePresetChange();
        }

        /// <summary>
        /// Update the values for a given preset
        /// </summary>
        /// <param name="presetName"> The name of the preset you want to update </param>
        [RemoteCommand]
        public void SaveWhiteBalancePreset(string presetName)
        {
            var input = new WhiteBalancePreset(R, G, B, presetName);

            if (presetName.Equals(AddNewPresetString))
            {
                OnNotify("Invalid selection");
            }

            var presetExists = whiteBalancePresets.Any(p => p.PresetName == presetName);

            if (!presetExists && GetAllWhiteBalances().Where(wb => wb.Item2 == false).Count() >= 10)
            {
                OnNotify("At this time only a maximum of 10 white balance presets can exist at a time.  If you want to add more you will need to first remove some.", MessageTypes.Alert);
                UpdateWhiteBalance(manualWhiteBalance.Item1);
                NotifyWhiteBalancePresetChange();
                return;
            }

            if (!presetExists)
            {
                whiteBalancePresets.Add(input);
            }
            else
            {
                var preset = whiteBalancePresets.FirstOrDefault(p => p.PresetName == presetName);
                preset.R = input.R;
                preset.G = input.G;
                preset.B = input.B;
            }

            Settings.Update(
                @"WhiteBalancePresets/" + presetName,
                attributes: new Dictionary<string, string>() { { "R", input.R.ToString() }, { "G", input.G.ToString() }, { "B", input.B.ToString() } });

            NotifyWhiteBalancePresetChange();
            UpdateWhiteBalance(presetName);
        }

        /// <summary>
        /// Unlocks Auto Exposure values
        /// </summary>
        [RemoteCommand]
        public void UnlockAE()
        {
            IsAELocked = false;
            captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ControlAeLock), IsAELocked);

            captureSession.Repeat();
        }

        /// <summary>
        /// Update the B channel of white balance
        /// </summary>
        /// <param name="value"> Value to update (Range: 0 to 3) </param>
        [RemoteCommand]
        [Alias("B")]
        public void UpdateB(float value)
        {
            B = value;
            captureSession.Repeat();
        }

        /// <summary>
        /// Updates Auto Exposure value to given an EV.
        /// </summary>
        /// <param name="value"> EV value to use (-12 to 12) </param>
        [RemoteCommand]
        [Alias("ExposureValue")]
        [CancelWhen(nameof(IsAutoExposure), false)]
        public void UpdateExposure(int value)
        {
            ExposureValue = value;

            captureSession.Repeat();
        }

        /// <summary>
        /// Update green channel of white balance
        /// </summary>
        /// <param name="value"> G channel value (Range: 0 to 3) </param>
        [RemoteCommand]
        [Alias("G")]
        public void UpdateG(float value)
        {
            G = value;
            captureSession.Repeat();
        }

        /// <summary>
        /// Updates ISO with given value
        /// </summary>
        /// <param name="value"> ISO value Between 50-800 </param>
        [RemoteCommand]
        [Alias("ISO")]
        [CancelWhen(nameof(IsAutoExposure), true)]
        public void UpdateISO(int value)
        {
            ISO = value;
            captureSession.Repeat();
        }

        /// <summary>
        /// Update white balance red channel
        /// </summary>
        /// <param name="value"> Red channel value (Range: 0 to 3) </param>
        [RemoteCommand]
        [Alias("R")]
        public void UpdateR(float value)
        {
            R = value;
            captureSession.Repeat();
        }

        /// <summary>
        /// Updates the shutter speed given a value in nanoseconds.
        /// </summary>
        /// <param name="value">
        /// Shutter speed in nanoseconds. (Range between <see cref="MinShutter" /> and <see
        /// cref="MaxShutter" />)
        /// </param>
        [RemoteCommand]
        [Alias("ShutterSpeed")]
        [CancelWhen(nameof(IsAutoExposure), true)]
        public void UpdateShutterSpeed(long value)
        {
            ShutterSpeed = value;

            captureSession.Repeat();
        }

        /// <summary>
        /// Updates White Balance given a value
        /// </summary>
        /// <param name="value"> The name of the preset you want to update </param>
        [RemoteCommand]
        [Alias("WhiteBalance")]
        public void UpdateWhiteBalance(string value) =>
            UpdateWhiteBalance(value, !GetAllWhiteBalances().First(wb => wb.Item1.Equals(value)).Item2);

        /// <summary>
        /// Generate the ISO stops from the min and max ISO range
        /// </summary>
        /// <param name="min"> Minimum ISO value </param>
        /// <param name="max"> Maximum ISO value </param>
        /// <returns> Collection of ISO stops between the min and max values </returns>
        private static IEnumerable<int> GenerateIsoStops(int min, int max)
        {
            if (min == 0 || max == 0)
            {
                yield break;
            }

            var valueToAdd = min;

            while (valueToAdd <= max)
            {
                yield return valueToAdd;
                valueToAdd *= 2;
            }
        }

        /// <summary>
        /// Get the new stopped value relative to the given value
        /// </summary>
        /// <param name="stops"> Number of stops to perform </param>
        /// <param name="value"> Value to stop from </param>
        /// <param name="range"> Range of available values to use </param>
        /// <returns>
        /// New value the number of stops away from the given value. E.g. value = 100, stops = -1,
        /// returns 50
        /// </returns>
        private static int Stop(int stops, int value, IList<int> range)
        {
            var index = range.IndexOf(value);
            var stopIndex = index + stops;
            stopIndex = stopIndex.Clamp(0, range.Count - 1);
            return range[stopIndex];
        }

        /// <summary>
        /// Get the new stopped value relative to the given value
        /// </summary>
        /// <param name="stops"> Number of stops to perform </param>
        /// <param name="value"> Value to stop from </param>
        /// <param name="range"> Range of available values to use </param>
        /// <returns>
        /// New value the number of stops away from the given value. E.g. value = 100, stops = -1,
        /// returns 50
        /// </returns>
        private static long Stop(int stops, long value, IList<long> range)
        {
            var index = range.IndexOf(value);
            var stopIndex = index + stops;
            stopIndex = stopIndex.Clamp(0, range.Count - 1);
            return range[stopIndex];
        }

        /// <summary>
        /// Gets a list of all white balances and a bool representing whether or not the preset is readonly
        /// </summary>
        /// <returns>
        /// A list of all white balances and a bool representing whether or not the preset is readonly
        /// </returns>
        private List<Tuple<string, bool>> GetAllWhiteBalances()
        {
            //var tupleA = new Tuple<string, bool>("Hello", false);
            //var tupleB = new Tuple<string, bool>("World", true);

            //var listOfTuples = new List<Tuple<string, bool>>();

            //listOfTuples.Add(tupleA);
            //listOfTuples.Add(tupleB);

            //return listOfTuples;

            var availableWhiteBalances = new List<Tuple<string, bool>>();

            foreach (var wb in whiteBalancePresets)
            {
                if (ReadOnlyPresets.Contains(wb.PresetName))
                {
                    availableWhiteBalances.Add(new Tuple<string, bool>(wb.PresetName, true));
                    continue;
                }

                availableWhiteBalances.Add(new Tuple<string, bool>(wb.PresetName, false));
            }

            availableWhiteBalances.AddRange(from val in System.Enum.GetNames(typeof(ControlAwbMode))
                                            where val != ControlAwbMode.WarmFluorescent.ToString()
                                            select val.Equals("Off")
                                                ? manualWhiteBalance
                                                : new Tuple<string, bool>(val, true));

            return availableWhiteBalances;
        }

        /// <summary>
        /// Gets a <see cref="List{String}" /> that contains all the custom white balance presets
        /// </summary>
        /// <returns>
        /// a <see cref="List{String}" /> that contains all the custom white balance presets
        /// </returns>
        private IEnumerable<string> GetWhiteBalancePresetNames()
        {
            var output = new List<string>();

            foreach (var wb in whiteBalancePresets)
            {
                output.Add(wb.PresetName);
            }

            return output;
        }

        /// <summary>
        /// Check the camera state to see if it's pre captured
        /// </summary>
        /// <returns> True if the camera is in a pre capture state </returns>
        private bool IsPrecaptured() => AEState == ControlAEState.Precapture || AEState == ControlAEState.FlashRequired;

        private void NotifyWhiteBalancePresetChange() =>
                    OnNotify($"{nameof(AllAvailableWhiteBalances)}:{JsonConvert.SerializeObject(GetAllWhiteBalances())}");

        /// <summary>
        /// Event that fires when capture is completed
        /// </summary>
        /// <param name="result"> The <see cref="ICaptureResult" /> </param>
        private void PreviewHandler_CaptureCompleted(ICaptureResult result)
        {
            if (WhiteBalance != ControlAwbMode.Off)
            {
                var rgb = (RggbChannelVector)result.Get(new SubCCaptureResultKey(CaptureResult.ColorCorrectionGains));
                R = (float)System.Math.Round(rgb.Red, 1);
                G = (float)System.Math.Round(rgb.GreenEven, 1);
                B = (float)System.Math.Round(rgb.Blue, 1);
            }

            if (!IsAutoExposure)
            {
                return;
            }

            var a = result.Get(new SubCCaptureResultKey(CaptureResult.ControlAeState));
            AEState = (ControlAEState)(int)a;
            ISO = (int)result.Get(new SubCCaptureResultKey(CaptureResult.SensorSensitivity));
            var nearest = exposureTimes.Nearest((long)result.Get(new SubCCaptureResultKey(CaptureResult.SensorExposureTime)));
            ShutterSpeed = nearest;
        }

        /// <summary>
        /// Updates white balance to a given value
        /// </summary>
        /// <param name="value"> The value to set the white balance </param>
        /// <param name="isPreset"> Whether or not the white balance is a preset </param>
        private void UpdateWhiteBalance(string value, bool isPreset)
        {
            if (WhiteBalancePresetNames.Contains(value))
            {
                WhiteBalance = ControlAwbMode.Off;
                ApplyWhiteBalancePreset(value);
                SelectedWhiteBalance = JsonConvert.SerializeObject(GetAllWhiteBalances().First(t => t.Item1.Equals(value)));
                return;
            }
            else if (value == "Manual")
            {
                value = "Off";
            }

            if (!new StringToAWB().TryConvert(value, out var mode) || value.Equals(AddNewPresetString))
            {
                OnNotify("Invalid auto white balance setting specified.  No change was made.");
                return;
            }

            WhiteBalance = (ControlAwbMode)mode;

            if (!isPreset && WhiteBalance.Equals(ControlAwbMode.Off))
            {
                SelectedWhiteBalance = JsonConvert.SerializeObject(manualWhiteBalance);
            }
            else if (!isPreset)
            {
                SelectedWhiteBalance = JsonConvert.SerializeObject(GetAllWhiteBalances().First(t => t.Item1.Equals(WhiteBalance.ToString())));
            }

            captureSession.Repeat();
        }
    }
}