//-----------------------------------------------------------------------
// <copyright file="VideoController.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe / Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid
{
    using SubCTools.Attributes;
    using SubCTools.Enums;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// <see cref="VideoController" /> class that manages all the logic for handling the output
    /// resolution and management of the converters that convert the HDMI signal in the Rayfin to composite/SDI/fiber.
    /// </summary>
    public class VideoController : DroidBase
    {
        /// <summary>
        /// The default video mode for the <see cref="VideoConverter.BlackmagicMicroHDMISDI" />. 1920X1080@60Hz
        /// </summary>
        private const int BlackmagicSDIDefaultMode = 16;

        /// <summary>
        /// The default video mode for the <see cref="VideoConverter.Chrontel" /> converter. NTSC
        /// </summary>
        private const int ChrontelDefaultMode = 3;

        /// <summary>
        /// The default <see cref="VideoConverter" /> I chose the chrontel since all systems prior
        /// to HDC were composite.
        /// </summary>
        private const VideoConverter DefaultVideoConverter = VideoConverter.Chrontel;

        /// <summary>
        /// The default video mode for the <see cref="VideoConverter.FiberOpticConverter" />
        /// converter. 2160p30
        /// </summary>
        private const int FiberDefaultMode = 128;

        //TODO: Change to TimeSpan

        /// <summary>
        /// The time it takes the system to change resolution and for the resolution to take effect.
        /// </summary>
        private const int ResolutionChangeTime = 18;

        /// <summary>
        /// The lock object to keep it from changing resolutions while it's currently changing.
        /// </summary>
        private static readonly object OutputSync = new object();

        /// <summary>
        /// Dictionary containing plain english video formats. Not sure of the exact refresh rates,
        /// 30 might be 29.97, 60 might be 59.94, and 24 might be 23.976, I'm not sure at the time
        /// of writing this (2018-07-17).
        /// </summary>
        private static Dictionary<string, VideoFormat> outputFormats
            = new Dictionary<string, VideoFormat>()
            {
                { "VGA", VideoFormat.VGA },
                { "NTSC", VideoFormat.NTSC },
                { "PAL", VideoFormat.PAL },
                { "1280x720 @ 50p", VideoFormat.HD50 },
                { "1280x720 @ 60p", VideoFormat.HD60 },
                { "1920x1080 @ 24p", VideoFormat.FHD24 },
                { "1920x1080 @ 25p", VideoFormat.FHD25 },
                { "1920x1080 @ 30p", VideoFormat.FHD30 },
                { "1920x1080 @ 50p", VideoFormat.FHD50 },
                { "1920x1080 @ 60p", VideoFormat.FHD60 },
                { "3840x2160 @ 24p", VideoFormat.UHD24 },
                { "3840x2160 @ 25p", VideoFormat.UHD25 },
                { "3840x2160 @ 30p", VideoFormat.UHD30 },
                { "4096 × 2160 @ 24p", VideoFormat.DCI4K24 },
                { "4096 × 2160 @ 25p", VideoFormat.DCI4K25 },
            };

        /// <summary>
        /// Dictionary containing plain english video converters.
        /// </summary>
        private static Dictionary<string, VideoConverter> videoConverters
            = new Dictionary<string, VideoConverter>()
            {
                { "Composite",  VideoConverter.Chrontel },
                { "SDI", VideoConverter.BlackmagicMicroHDMISDI },
                { "Fiber Optic", VideoConverter.FiberOpticConverter }
            };

        /// <summary>
        /// The GPIO pin associated with <see cref="hdmiPower" />.
        /// </summary>
        private readonly SubCGPIO hdmiPower = new SubCGPIO(61);

        /// <summary>
        /// The GPIO pin associated with <see cref="resetPin" />
        /// </summary>
        private readonly SubCGPIO resetPin = new SubCGPIO(125);

        //TODO: Use VideoFormat enums
        //TODO: Unchanging, const
        //TODO: Look in to readonly list

        /// <summary>
        /// The <see cref="int[]" /> of supported video modes for the <see
        /// cref="VideoConverter.BlackmagicMicroHDMISDI" />.
        /// </summary>
        private int[] blackmagicSDISupportedModes = new int[] { 4, 16, 19, 31, 32, 33, 34 };

        /// <summary>
        /// The <see cref="int[]" /> of supported video modes for the <see
        /// cref="VideoConverter.Chrontel" />.
        /// </summary>
        private int[] chrontelSupportedModes = new int[] { 3, 18 };

        /// <summary>
        /// The <see cref="int[]" /> of supported video modes for the <see
        /// cref="VideoConverter.FiberOpticConverter" />
        /// </summary>
        private int[] fiberSupportedModes = new int[] { 3, 4, 16, 19, 31, 128, 129, 130 };

        /// <summary>
        /// The current format the system is outputting in a human readable format.
        /// </summary>
        private string outputFormat;

        /// <summary>
        /// The <see cref="resetTime" /> for the <see cref="SubCGPIO" /> pin.
        /// </summary>
        private int resetTime = 250;

        /// <summary>
        /// The last saved OutputFormat that is loaded on startup.
        /// </summary>
        private int savedOutputFormat;

        //TODO: Array of VideoFormat

        /// <summary>
        /// <see cref="int[]" /> holding the current supported modes.
        /// </summary>
        private int[] supportedModes;

        /// <summary>
        /// The <see cref="System.Timers.Timer" /> that controls how quickly the system can change resolutions.
        /// </summary>
        private System.Timers.Timer videoChangeTimer = new System.Timers.Timer()
        {
            AutoReset = false,
            Interval = TimeSpan.FromSeconds(ResolutionChangeTime).TotalMilliseconds
        };

        /// <summary>
        /// The <see cref="VideoConverter" /> that the system currently has installed.
        /// </summary>
        private VideoConverter videoConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoController" /> class.
        /// </summary>
        /// <param name="settings"><see cref="ISettingsService" /> settings</param>
        public VideoController(ISettingsService settings)
            : base(settings)
        {
            // Configure GPIO
            hdmiPower.Output();
            hdmiPower.On();
            resetPin.Output();

            // Get converter / supported video modes.
            videoConverter = GetVideoConverter();
            supportedModes = GetSupportedVideoModes();

            // Load settings.
            LoadSettings();

            videoChangeTimer.Elapsed += (s, e) => OutputFormat = GetVideoMode().ToString();

            // If the saved video format is supported set it as the output.
            if (GetSupportedVideoModes().Contains(savedOutputFormat))
            {
                SetVideoMode((VideoFormat)savedOutputFormat);
            }
            else
            {
                SetOutputToDefaultFormat();
            }
        }

        /// <summary>
        /// Gets the <see cref="outputFormats" /> resolutions in human readable format.
        /// </summary>
        public static IEnumerable<string> OutputFormats { get; }
            = outputFormats.Keys;

        //TODO: Don't hold state here that's already held in OS

        /// <summary>
        /// Gets the Output Format of the HDMI signal inside the Rayfin.
        /// </summary>
        [RemoteState]
        public string OutputFormat
        {
            get => outputFormat;
            private set
            {
                if (Set(nameof(OutputFormat), ref outputFormat, value))
                {
                    OnNotify($"{nameof(OutputFormat)}:{OutputFormat}");
                }
            }
        }

        /// <summary>
        /// Gets or sets the time to wait between GPIO states in milliseconds
        /// </summary>
        [Savable]
        public int ResetTime
        {
            get => resetTime;
            set => Set(nameof(ResetTime), ref resetTime, value);
        }

        //TODO: Add SupportedVideoOutputModes alias to GetSupportedVideoModeStrings

        /// <summary>
        /// Gets or sets a <see cref="string" /> that saves the <see cref="VideoFormat" /> so it can
        /// be re initialized on startup.
        /// </summary>
        [Savable]
        public int SavedOutputFormat
        {
            get => savedOutputFormat;
            set => Set(nameof(SavedOutputFormat), ref savedOutputFormat, value);
        }

        /// <summary>
        /// Contains a plain english list of supported video modes in the following format
        /// {width}x{height} @ {hZ}p
        /// </summary>
        public IEnumerable<string> SupportedVideoOutputModes => GetSupportedVideoModeStrings();

        /// <summary>
        /// Returns the type of video converter currently installed in the rayfin.
        /// </summary>
        [RemoteState]
        public string VideoConverterType => videoConverters.FirstOrDefault(o => o.Value == videoConverter).Key.ToString();

        /// <summary>
        /// Turns off power to HDMI converter(GPIO_61)
        /// </summary>
        [RemoteCommand]
        public void HDMIPowerOff()
        {
            hdmiPower.Off();
            OnNotify("HDMI Off");
        }

        /// <summary>
        /// Turns on power to HDMI converter(GPIO_61)
        /// </summary>
        [RemoteCommand]
        public void HDMIPowerOn()
        {
            hdmiPower.On();
            OnNotify("HDMI On");
        }

        /// <summary>
        /// Sets the output video mode to a given format.
        /// </summary>
        /// <param name="format">Video format to set</param>
        [RemoteCommand]
        [Alias("OutputFormat")]
        public void Output(string format)
        {
            if (!string.IsNullOrEmpty(format) && outputFormats.ContainsKey(format))
            {
                SetVideoMode(outputFormats[format]);
            }
            else
            {
                OnNotify($"format [{format ?? "'blank'"}] doesn't exist.  Please try another video format");
            }
        }

        /// <summary>
        /// Sets video output mode to NTSC (Standard Definition)
        /// </summary>
        [RemoteCommand]
        public void OutputNTSC() => SetVideoMode(VideoFormat.NTSC);

        /// <summary>
        /// Sets video output mode to PAL (Standard Definition)
        /// </summary>
        [RemoteCommand]
        public void OutputPAL()
        {
            if (supportedModes.Contains((int)VideoFormat.PAL))
            {
                SetVideoMode(VideoFormat.PAL);
            }
            else
            {
                OnNotify("PAL is not supported by the video converter, please select another format",
                    Messaging.Models.MessageTypes.Error);
            }
        }

        /// <summary>
        /// Resets the HDMI(Simulates a replug to update the edid).
        /// </summary>
        /// <returns>A <see cref="Task" /></returns>
        public async Task ResetHDMI()
        {
            await Task.Delay(ResetTime);
            resetPin.Off();
            await Task.Delay(ResetTime);
            resetPin.On();
        }

        /// <summary>
        /// Sets the Reset Pin High(GPIO_125) which sets the Chrontel composite converter to PAL mode.
        /// </summary>
        [RemoteCommand]
        public void ResetPinHigh()
        {
            resetPin.On();
        }

        /// <summary>
        /// Sets the Reset Pin High(GPIO_125) which sets the Chrontel composite converter to NTSC mode.
        /// </summary>
        [RemoteCommand]
        public void ResetPinLow()
        {
            resetPin.Off();
        }

        /// <summary>
        /// Sets the hw.hdmi.resolution global prop in the system.
        /// </summary>
        /// <param name="value">The <see cref="VideoFormat" /> to set the resolution to.</param>
        [RemoteCommand]
        public void Resolution(int value)
        {
            DroidSystem.ShellSync($"setprop hw.hdmi.resolution {value}");
        }

        /// <summary>
        /// Sets the <see cref="VideoFormat" /> on the device with the given <see cref="mode" />.
        /// </summary>
        /// <param name="mode">The <see cref="VideoFormat" /> to use.</param>
        public void Resolution(VideoFormat mode) => Resolution((int)mode);

        /// <summary>
        /// Returns a list of VideoFormats that the Rayfin / Converter combo support.
        /// </summary>
        /// <returns>A list of supported video modes in VideoFormat type</returns>
        private int[] GetSupportedVideoModes()
        {
            switch (videoConverter)
            {
                case VideoConverter.Chrontel:
                    return chrontelSupportedModes;

                case VideoConverter.BlackmagicMicroHDMISDI:
                    return blackmagicSDISupportedModes;

                case VideoConverter.FiberOpticConverter:
                    return fiberSupportedModes;

                default:
                    return chrontelSupportedModes;
            }
        }

        /// <summary>
        /// Returns a list of VideoFormats that the Rayfin / Converter combo support.
        /// </summary>
        /// <returns>A list of supported video modes in String type</returns>
        private List<string> GetSupportedVideoModeStrings()
        {
            var output = new List<string>();

            foreach (VideoFormat format in GetSupportedVideoModes())
            {
                if (outputFormats.ContainsValue(format))
                {
                    output.Add(outputFormats.FirstOrDefault(o => o.Value == format).Key.ToString());
                }
            }

            return output;
        }

        /// <summary>
        /// Sets the field videoConverter with the value stored in the prop file.
        /// </summary>
        /// <returns>The <see cref="VideoConverter" /> that is set in the system.</returns>
        private VideoConverter GetVideoConverter()
        {
            var converterString = DroidSystem.ShellSync("getprop rayfin.video.converter");
            if (converterString == string.Empty)
            {
                OnNotify("No Video Converter Prop Detected", Messaging.Models.MessageTypes.Error);
                return DefaultVideoConverter;
            }

            if (int.TryParse(converterString, out int result) && Enum.IsDefined(typeof(VideoConverter), result))
            {
                return (VideoConverter)result;
            }
            else
            {
                OnNotify("Invalid Video Converter Detected", Messaging.Models.MessageTypes.Error);
                return DefaultVideoConverter;
            }
        }

        /// <summary>
        /// Gets the current <see cref="VideoFormat" /> from the system so we know what <see
        /// cref="VideoFormat" /> is currently being displayed.
        /// </summary>
        /// <returns><see cref="VideoFormat" /> that the system is currently outputting.</returns>
        private VideoFormat GetVideoMode() =>
            (VideoFormat)Enum.Parse(typeof(VideoFormat), DroidSystem.ShellSync(@"cat /sys/class/graphics/fb0/video_mode"));

        /// <summary>
        /// Resets the GPIO on the <see cref="VideoConverter.Chrontel" />.
        /// </summary>
        private void SetChrontelGPIO(VideoFormat format)
        {
            switch (format)
            {
                case (VideoFormat.PAL):
                    ResetPinLow();
                    Thread.Sleep(resetTime);
                    ResetPinHigh();
                    break;

                case (VideoFormat.NTSC):
                    ResetPinHigh();
                    Thread.Sleep(resetTime);
                    ResetPinLow();
                    break;
            }
        }

        /// <summary>
        /// Sets the <see cref="VideoFormat" /> output to the default mode. It should do this if
        /// there is no saved format or the saved format is invalid.
        /// </summary>
        private void SetOutputToDefaultFormat()
        {
            switch (videoConverter)
            {
                case VideoConverter.BlackmagicMicroHDMISDI:
                    Output(BlackmagicSDIDefaultMode.ToString());
                    break;

                case VideoConverter.FiberOpticConverter:
                    Output(FiberDefaultMode.ToString());
                    break;

                case VideoConverter.Chrontel:
                    Output(ChrontelDefaultMode.ToString());
                    break;
            }
        }

        /// <summary>
        /// Sets the HDMI output of the rayfin to match the given videoFormat
        /// </summary>
        /// <param name="videoFormat">Video format to set to the Rayfin</param>
        private void SetVideoMode(VideoFormat videoFormat)
        {
            if (!supportedModes.Contains((int)videoFormat))
            {
                OnNotify("Unsupported Resolution");
                return;
            }

            if (OutputSync.IsLocked())
            {
                OnNotify("Video mode is currently being set, it can take up to 30s for the change to take effect.", Messaging.Models.MessageTypes.Error);
                return;
            }

            SavedOutputFormat = (int)videoFormat;

            Task.Run(() =>
            {
                lock (OutputSync)
                {
                    var format = outputFormats.FirstOrDefault(o => o.Value == videoFormat).Key.ToString();

                    // Set the hdmi edid in Android.
                    Android.Util.Log.Debug("SubCRayfin.SubCRayfin.VideoController", $"Setting resolution to {format}");

                    DroidSystem.ShellSync($"setprop rayfin.hdmi.resolution {(int)videoFormat}");

                    // Kill power to the converter while the resolution changes.
                    HDMIPowerOff();
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    Resolution(videoFormat);
                    Thread.Sleep(TimeSpan.FromSeconds(1));

                    if (videoConverter == VideoConverter.Chrontel)
                    {
                        // Set the chrontels GPIO
                        SetChrontelGPIO(videoFormat);
                    }

                    // Reset power to converter to simulate a replug so the edid information
                    // exchange takes place.
                    HDMIPowerOn();
                    videoChangeTimer.Start();

                    // Wait 25s for the video change to take place(Takes roughly 15 seconds but set
                    // it to 25 to be safe).
                    Thread.Sleep(TimeSpan.FromSeconds(25));
                    OutputFormat = outputFormats.FirstOrDefault(o => o.Value == GetVideoMode()).Key.ToString();
                }
            });
        }
    }
}