//-----------------------------------------------------------------------
// <copyright file="StillHandler.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Camera
{
    using Android.Graphics;
    using Android.Hardware.Camera2;
    using Android.Media;
    using Android.OS;
    using SubCTools.Attributes;
    using SubCTools.Converters;
    using SubCTools.DataTypes;
    using SubCTools.Droid.Converters;
    using SubCTools.Droid.Enums;
    using SubCTools.Droid.EventArguments;
    using SubCTools.Droid.Extensions;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Droid.Listeners;
    using SubCTools.Droid.Models;
    using SubCTools.Droid.Tools;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using static SubCTools.Helpers.Paths;

    /// <summary>
    /// Responsible for taking stills with the Rayfin.
    /// </summary>
    public class StillHandler : DroidBase, IPictureTaker
    {
        private const string DefaultContinuousDirectory = "/Continuous";

        /// <summary>
        /// Default rate the camera will burst pictures
        /// </summary>
        private const int DefaultContinuousRate = 2;

        private const ImageFormatType DefaultImageFormat = ImageFormatType.Jpeg;

        private const int DefaultJpegQuality = 95;

        /// <summary>
        /// The default amount for <see cref="MaxFilesPerDirectory"/> when there is no value
        /// </summary>
        private const int DefaultMaxFilesPerDirectory = 1_000;

        private const string DefaultStillDirectory = "/Stills";

        /// <summary>
        /// Default name for a image file.
        /// </summary>
        private const string DefaultStillName = "${yyyy}-${MM}-${dd} - ${hh}${mm}${ss}";

        /// <summary>
        /// Maximum rate the camera can burst pictures
        /// </summary>
        private const int MaxContinuousRate = 4;

        /// <summary>
        /// Minimum rate the camera can burst pictures
        /// </summary>
        private const int MinContinuousRate = 1;

        /// <summary>
        /// Object to lock on to prevent multiple calls to the image reader set
        /// </summary>
        private static readonly object ReaderSync = new object();

        /// <summary>
        /// SubCCaptureSession isntance used to generate image captures from the sensor
        /// </summary>
        private readonly SubCCaptureSession captureSession;

        /// <summary>
        /// Camera Characteristics used to raw images
        /// </summary>
        private readonly CameraCharacteristics characteristics;

        /// <summary>
        /// Generate new image readers from the grabber
        /// </summary>
        private readonly IImageReaderGrabber imageReaderGrabber;

        private readonly IImageSaver imageSaver;

        private double average = 120;

        /// <summary>
        /// The number of bursted images
        /// </summary>
        private long burstCount = 0;

        private int burstDirectoryImageCount = 0;
        private int burstDirectoryIndex = 0;

        /// <summary>
        /// Directory to save the burst images
        /// </summary>
        private DirectoryInfo continuousDirectory = new DirectoryInfo(DefaultContinuousDirectory);

        /// <summary>
        /// Unparsed name to use for burst images
        /// </summary>
        private string continuousName = DefaultStillName + ".${fff}";

        /// <summary>
        /// Current Burst rate
        /// </summary>
        private int continuousRate = DefaultContinuousRate;

        private int directoryIndex = 0;

        /// <summary>
        /// Image format to save images, raw or jpeg
        /// </summary>
        private ImageFormatType imageFormat = DefaultImageFormat;

        /// <summary>
        /// Is the camera currently bursting images
        /// </summary>
        private bool? isContinuous = false;

        private int iterations = 0;

        private int jpegQuality = DefaultJpegQuality;

        /// <summary>
        /// Resolution of the jpeg images
        /// </summary>
        private Size jpegResolution;

        private int maxFilesPerDirectory = DefaultMaxFilesPerDirectory;

        /// <summary>
        /// Resolution of the raw images
        /// </summary>
        private Size rawResolution;

        /// <summary>
        /// Unparsed name to use for stills
        /// </summary>
        private string stillName = DefaultStillName;

        /// <summary>
        /// Directory to save the still images
        /// </summary>
        private DirectoryInfo stillsDirectory = new DirectoryInfo(DefaultStillDirectory);

        /// <summary>
        /// Token used to cancel bursting
        /// </summary>
        private CancellationToken token;

        /// <summary>
        /// Tokensource used to cancel bursting
        /// </summary>
        private CancellationTokenSource tokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="StillHandler"/> class.
        /// </summary>
        /// <param name="jpegResolutions">Available jpeg resolutions</param>
        /// <param name="rawResolutions">Available raw resolutions</param>
        /// <param name="captureSession">Capture session used to capture images from the sensor</param>
        /// <param name="characteristics">Camera characteristics for raw images</param>
        public StillHandler(
           IEnumerable<Size> jpegResolutions,
           IEnumerable<Size> rawResolutions,
           SubCCaptureSession captureSession,
           CameraCharacteristics characteristics)
           : this(
                 new SubCImageReaderGrabber(),
                 jpegResolutions,
                 rawResolutions,
                 null,
                 captureSession,
                 characteristics,
                 new SubCImageSaver())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StillHandler"/> class.
        /// </summary>
        /// <param name="jpegResolutions">Available jpeg resolutions</param>
        /// <param name="rawResolutions">Available raw resolutions</param>
        /// <param name="settings">Settings service to save all the still settings</param>
        /// <param name="captureSession">Capture session used to capture images from the sensor</param>
        /// <param name="characteristics">Camera characteristics for raw images</param>
        public StillHandler(
            IEnumerable<Size> jpegResolutions,
            IEnumerable<Size> rawResolutions,
            ISettingsService settings,
            SubCCaptureSession captureSession,
            CameraCharacteristics characteristics)
            : this(
                  new SubCImageReaderGrabber(),
                  jpegResolutions,
                  rawResolutions,
                  settings,
                  captureSession,
                  characteristics,
                  new SubCImageSaver())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StillHandler"/> class.
        /// </summary>
        /// <param name="imageReaderGrabber">Implementation of image reader grabber to use</param>
        /// <param name="jpegResolutions">Available jpeg resolutions</param>
        /// <param name="rawResolutions">Available raw resolutions</param>
        /// <param name="settings">Settings service to save all the still settings</param>
        /// <param name="captureSession">Capture session used to capture images from the sensor</param>
        /// <param name="characteristics">Camera characteristics for raw images</param>
        /// <param name="imageSaver">Object responsible for saving the images to the disk</param>
        public StillHandler(
            IImageReaderGrabber imageReaderGrabber,
            IEnumerable<Size> jpegResolutions,
            IEnumerable<Size> rawResolutions,
            ISettingsService settings,
            SubCCaptureSession captureSession,
            CameraCharacteristics characteristics,
            IImageSaver imageSaver)
            : base(settings)
        {
            this.imageReaderGrabber = imageReaderGrabber;
            this.captureSession = captureSession;
            this.characteristics = characteristics;
            this.imageSaver = imageSaver ?? throw new ArgumentNullException(nameof(imageSaver));
            var handlerThread = new HandlerThread("Still handler");
            handlerThread.Start();

            JPEGResolutions = jpegResolutions;
            RawResolutions = rawResolutions;

            // set the default resolution to the largest size
            jpegResolution = JPEGResolutions.First();
            rawResolution = RawResolutions.First();
        }

        /// <summary>
        /// Event to fire when the image format changes.
        /// </summary>
        public event EventHandler<ImageFormatType> ImageFormatChanged;

        /// <summary>
        /// Event to fire when the jpeg resolution changes
        /// </summary>
        public event EventHandler<Size> JPEGResolutionChanged;

        /// <summary>
        /// Event to fire when a still is saved
        /// </summary>
        public event EventHandler<string> PictureTaken;

        /// <summary>
        /// Event to fire after a still has been captured by the sensor
        /// </summary>
        public event EventHandler<string> StillCaptured;

        /// <summary>
        /// Event to fire after a still has been captured by the sensor
        /// </summary>
        public event EventHandler StoppedContinuous;

        /// <summary>
        /// Gets all the available JPEG resolutions
        /// </summary>
        public static IEnumerable<Size> JPEGResolutions { get; private set; } = new Size[] { };

        /// <summary>
        /// Gets or sets the directory for bursting
        /// </summary>
        [Savable]
        [RemoteState]
        [PropertyConverter(typeof(StringToDirectoryInfo))]
        public DirectoryInfo BurstDirectory
        {
            get => continuousDirectory;
            set
            {
                Set(nameof(BurstDirectory), ref continuousDirectory, value.RemoveIllegalPathCharacters(true).LimitDirectoryLength());
                GetBurstDirectory(0);
            }
        }

        /// <summary>
        /// Gets or sets the name for bursting images
        /// </summary>
        [Savable]
        [RemoteState]
        public string BurstName
        {
            get => continuousName;
            set
            {
                Set(nameof(BurstName), ref continuousName, UpdateStillName(value));
            }
        }

        /// <summary>
        /// Gets or sets the rate at which to burst images
        /// </summary>
        [Savable]
        [RemoteState]
        [Alias("ContinuousRate")]
        public int BurstRate
        {
            get => continuousRate;
            set
            {
                value = value.Clamp(MinContinuousRate, MaxContinuousRate);
                Set(nameof(BurstRate), ref continuousRate, value);
            }
        }

        /// <summary>
        /// Gets or sets the still directory.
        /// </summary>
        public string Directory { get => StillsDirectory.FullName; set => StillsDirectory = new DirectoryInfo(value); }

        /// <summary>
        /// Gets or sets for of images taken
        /// </summary>
        [Savable]
        [RemoteState(true)]
        [PropertyConverter(typeof(StringToImageFormatType))]
        [CancelWhen(nameof(IsBursting), true)]
        [CancelWhen(nameof(RecordingHandler.IsRecording), true)]
        [CancelWhen(nameof(Rayfin.CanTakePicture), false)]
        public ImageFormatType ImageFormat
        {
            get => imageFormat;
            set
            {
                if (Set(nameof(ImageFormat), ref imageFormat, value))
                {
                    UpdateReader();
                    OnNotify($"{nameof(ImageFormat)}:{ImageFormat}");
                    ImageFormatChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Gets the image reader.
        /// </summary>
        public IImageReader ImageReader { get; private set; }

        /// <summary>
        /// Gets the value on whether the camera is bursting. True if bursting, false if not, null if it is transitioning between the two
        /// </summary>
        [RemoteState(true)]
        public bool? IsBursting
        {
            get => isContinuous;
            private set
            {
                if (Set(nameof(IsBursting), ref isContinuous, value))
                {
                    StoppedContinuous?.Invoke(this, EventArgs.Empty);
                    OnNotify($"{nameof(IsBursting)}:{IsBursting?.ToString() ?? "null"}");
                }
            }
        }

        /// <summary>
        /// Gets or sets the compression rate of the jpeg image.  100% means no compression.
        /// </summary>
        [Savable]
        [RemoteState]
        [Alias("JpegQuality")]
        public int JPEGQuality
        {
            get => jpegQuality;
            set
            {
                Set(nameof(JPEGQuality), ref jpegQuality, value.Clamp(1));
            }
        }

        /// <summary>
        /// Gets or sets the resolution of the image in pixels x pixels
        /// </summary>
        [Savable]
        [RemoteState(true)]
        [PropertyConverter(typeof(StringToStillResolutionConverter))]
        public Size JPEGResolution
        {
            get => jpegResolution;
            set
            {
                if (Set(nameof(JPEGResolution), ref jpegResolution, value))
                {
                    JPEGResolutionChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of pictures to save per directory, Negative value means unlimited
        /// </summary>
        [Savable]
        [RemoteState]
        public int MaxFilesPerDirectory
        {
            get => maxFilesPerDirectory;
            set => Set(nameof(MaxFilesPerDirectory), ref maxFilesPerDirectory, value);
        }

        /// <summary>
        /// Gets or sets the name of the picture.
        /// </summary>
        public string PictureName { get => StillName; set => UpdateStillName(value); }

        /// <summary>
        /// Gets or sets the resolution of the image in pixels x pixels
        /// </summary>
        [Savable]
        [RemoteState]
        [PropertyConverter(typeof(StringToSizeConverter))]
        public Size RawResolution
        {
            get => rawResolution;
            set => Set(nameof(RawResolution), ref rawResolution, value);
        }

        /// <summary>
        /// Gets all the available raw resolutions
        /// </summary>
        public IEnumerable<Size> RawResolutions { get; }

        /// <summary>
        /// Gets or sets the still filename which may contain tags
        /// </summary>
        [Savable]
        [RemoteState]
        public string StillName
        {
            get => stillName;
            set
            {
                Set(nameof(StillName), ref stillName, UpdateStillName(value));
            }
        }

        /// <summary>
        /// Gets or sets the stills directory which may contain tags
        /// </summary>
        [Savable]
        [RemoteState]
        [PropertyConverter(typeof(StringToDirectoryInfo))]
        public DirectoryInfo StillsDirectory
        {
            get => stillsDirectory;
            set
            {
                directoryIndex = 0;
                Set(nameof(StillsDirectory), ref stillsDirectory, value.RemoveIllegalPathCharacters(true).LimitDirectoryLength());
                DroidSystem.ShellSync($@"setprop rayfin.still.directory {value}");
                stillsDirectory.ParseDirectoryAddSeqNum(out directoryIndex, directoryIndex, MaxFilesPerDirectory);
            }
        }

        /// <summary>
        /// Generates a directory name for the next burst image
        /// </summary>
        /// <param name="startIndex">Index to start numbering the directories</param>
        /// <returns>The parsed directory name</returns>
        public DirectoryInfo GetBurstDirectory(int startIndex)
        {
            var dir = new DirectoryInfo(System.IO.Path.Combine(DroidSystem.BaseDirectory, BurstDirectory.FullName.Trim('/')));
            var parsedDir = dir.ParseDirectoryAddSeqNum(out burstDirectoryIndex, startIndex, MaxFilesPerDirectory);
            burstDirectoryImageCount = System.IO.Directory.Exists(parsedDir.FullName) ? System.IO.Directory.GetFiles(parsedDir.FullName, "*.*", SearchOption.TopDirectoryOnly).Count() : 0;
            return parsedDir;
        }

        /// <summary>
        /// Generates a file name for the next image.
        /// </summary>
        /// <returns>File as System.IO.FileInfo</returns>
        public FileInfo GetFile()
        {
            var dir = new DirectoryInfo(System.IO.Path.Combine(DroidSystem.BaseDirectory, StillsDirectory.FullName.Trim('/')));

            if (dir.Exists)
            {
                DroidSystem.ShellSync($"chmod 777 {dir.FullName}");
            }

            var parsedDir = dir.ParseDirectoryAddSeqNum(out directoryIndex, directoryIndex, MaxFilesPerDirectory);
            return GetFile(parsedDir, StillName);
        }

        /// <summary>
        /// Reset the current still name back to it's default
        /// </summary>
        [RemoteCommand]
        [Alias("LoadStillDefaults", "StillNameToDefault")]
        public void LoadDefaults()
        {
            StillName = DefaultStillName;
            StillsDirectory = new DirectoryInfo(DefaultStillDirectory);
            BurstDirectory = new DirectoryInfo(DefaultContinuousDirectory);
            BurstRate = DefaultContinuousRate;

            // JPEGResolution = JPEGResolutions.First();
            OnNotify(this, new NotifyEventArgs($"{nameof(StillName)}:{StillName}", MessageTypes.Information));
            OnNotify(this, new NotifyEventArgs($"{nameof(StillsDirectory)}:{StillsDirectory}", MessageTypes.Information));
            OnNotify(this, new NotifyEventArgs($"{nameof(BurstDirectory)}:{BurstDirectory}", MessageTypes.Information));
            OnNotify(this, new NotifyEventArgs($"{nameof(BurstRate)}:{BurstRate}", MessageTypes.Information));
        }

        /// <summary>
        /// Load all the settings
        /// </summary>
        public override void LoadSettings()
        {
            base.LoadSettings();
            UpdateReader();
        }

        /// <summary>
        /// Start continuously shooting stills
        /// </summary>
        public async void StartContinuous()
        {
            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;

            IsBursting = true;

            await Task.Run(
            async () =>
            {
                var burstDirectory = GetBurstDirectory(burstDirectoryIndex);
                burstDirectory.CreateIfMissing();

                while (!token.IsCancellationRequested)
                {
                    var startTime = DateTime.Now;

                    try
                    {
                        await TakeStillAsync(GetFile(burstDirectory, BurstName));
                        if (++burstDirectoryImageCount >= MaxFilesPerDirectory)
                        {
                            burstDirectoryIndex++;
                            burstDirectory = GetBurstDirectory(burstDirectoryIndex);
                            burstDirectory.CreateIfMissing();
                        }
                    }
                    catch (Exception e)
                    {
                        OnNotify(e.Message);
                    }
                    var waitTime = TimeSpan.FromSeconds(1.0 / BurstRate) - (DateTime.Now - startTime);

                    if (waitTime > TimeSpan.Zero)
                    {
                        await Task.Delay(waitTime);
                    }
                }
            }, token);

            IsBursting = false;
        }

        /// <summary>
        /// Start continuous picture sync.
        /// </summary>
        /// <param name="startTime">Time to start taking pictures.</param>
        [RemoteCommand]
        [PropertyConverter(typeof(StringToDateTime))]
        public async void StartContinuousSync(DateTime startTime)
        {
            StartContinuousSync(startTime, 0, 1000);
        }

        /// <summary>
        /// Start continuously taking pictures synced with another camera.
        /// </summary>
        /// <param name="startTime">Time to start taking picture.</param>
        /// <param name="offset">Amount to offset thes tart time.</param>
        /// <param name="delay">The delay in between pictures.</param>
        [PropertyConverter(typeof(StringToDateTime))]
        public async void StartContinuousSync(DateTime startTime, int offset, int delay)
        {
            startTime += TimeSpan.FromMilliseconds(offset);

            IsBursting = true;

            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;

            while (!tokenSource.IsCancellationRequested)
            {
                _ = TakeStill(startTime);
                startTime += TimeSpan.FromMilliseconds(delay);
            }

            IsBursting = false;
        }

        /// <summary>
        /// Stop continuously taking pictures
        /// </summary>
        public void StopContinuous()
        {
            if (!IsBursting ?? false)
            {
                return;
            }

            tokenSource?.Cancel();
        }

        /// <summary>
        /// Take a still precisely at the given time
        /// </summary>
        /// <param name="time">Date and time to take a still</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task TakeStill(DateTime time)
        {
            var tcs = new TaskCompletionSource<bool>();

            // create the builder with the proper settings
            var builder = captureSession.CreateCaptureBuilder(SubCCameraTemplate.ZeroShutterLag);
            builder.AddTarget(new SubCSurface(ImageReader.Surface));
            builder.Set(CaptureRequest.JpegQuality, (sbyte)JPEGQuality);

            var request = builder.Build();

            // create the stills directory if it's missing
            Droid.Extensions.DirectoryInfoExtensions.CreateIfMissing(new DirectoryInfo(System.IO.Path.Combine(DroidSystem.BaseDirectory, StillsDirectory.FullName.Trim('/'))));

            var captureTime = DateTime.Now;

            var callback = new SubCCaptureCallback();
            var handler = new EventHandler<CaptureEventArgs>((s, e) =>
            {
                if (e == null)
                {
                    OnNotify("There was an error capturing the still, please try again.");
                    return;
                }

                iterations++;

                var file = new FileInfo(System.IO.Path.Combine(DroidSystem.BaseDirectory, StillsDirectory.FullName.Trim('/'), $"{captureTime.Year}.{captureTime.Month}.{captureTime.Day} {captureTime.Hour}.{captureTime.Minute}.{captureTime.Second}.{captureTime.Millisecond.ToString("000")}.{(ImageFormat == ImageFormatType.Jpeg ? "jpg" : "dng")}"));
                StillCaptured?.Invoke(this, file.FullName);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                SaveImage(file, e.CaptureResult);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            });

            callback.CaptureCompleted += handler;
            callback.CaptureFailed += (s, e) => handler(s, null);

            var sessionCallbackThread = new HandlerThread("SessionCallbackThread");
            sessionCallbackThread.Start();

            var sessionCallbackHandler = new Handler(sessionCallbackThread.Looper);

            var timeToTakeStill = time - DateTime.Now;

            // time already passed
            if (timeToTakeStill < TimeSpan.Zero)
            {
                Capture();
                return;
            }

            // schedule the timer to capture the still
            var timer = new Timer(x => Capture(), null, timeToTakeStill, Timeout.InfiniteTimeSpan);

            void Capture()
            {
                captureTime = DateTime.Now;
                captureSession.Capture(request, callback, new SubCHandler(sessionCallbackHandler));
                tcs.TrySetResult(true);
            }

            await tcs.Task;
        }

        /// <summary>
        /// Take a picture
        /// </summary>
        /// <returns>True if picture was taken, false if it failed</returns>
        [CancelWhen(nameof(RecordingHandler.IsRecording4K), true)]
        public async Task TakeStillAsync()
        {
            await TakeStillAsync(captureSession.CreateCaptureBuilder(SubCCameraTemplate.StillCapture));
        }

        /// <summary>
        /// Take a still.
        /// </summary>
        /// <param name="builder">Capture builder to use for the picture.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [CancelWhen(nameof(RecordingHandler.IsRecording4K), true)]
        public async Task TakeStillAsync(ICaptureBuilder builder)
        {
            var pictureFile = GetFile();

            if (pictureFile == null)
            {
                OnNotify("Unable to create picture file", MessageTypes.Error);
                return;
            }

            pictureFile.Directory.CreateIfMissing();

            await TakeStillAsync(builder, pictureFile);
        }

        /// <summary>
        /// Take a still with the given file name
        /// </summary>
        /// <param name="file">File to save the still</param>
        /// <returns>Task</returns>
        [CancelWhen(nameof(RecordingHandler.IsRecording4K), true)]
        public async Task TakeStillAsync(FileInfo file)
        {
            await TakeStillAsync(captureSession.CreateCaptureBuilder(SubCCameraTemplate.StillCapture), file);
        }

        /// <summary>
        /// Update the image format of the stills
        /// </summary>
        /// <param name="imageFormat">New image format to use</param>
        public void UpdateImageFormat(ImageFormatType imageFormat)
        {
            if (ImageFormat.ToString() == imageFormat.ToString())
            {
                return;
            }

            ImageFormat = imageFormat;
        }

        /// <summary>
        /// Writes the following exif data to the file.
        /// </summary>
        /// <param name="filename">The file to write the exif data</param>
        /// <param name="tags">The collection of tags to write to the file in the format {<see cref="ExifInterface"/> Tag, Value}</param>
        public void WriteExif(string filename, Dictionary<string, string> tags)
        {
            // can't save exif information to dngs
            if (filename.EndsWith("dng"))
            {
                return;
            }

            var exif = new ExifInterface(filename);

            foreach (var tag in tags)
            {
                exif.SetAttribute(tag.Key, tag.Value);
            }

            exif.SaveAttributes();
        }

        /// <summary>
        /// Write exif data to the picture.
        /// </summary>
        /// <param name="filename">Name of the file to write the exif data to.</param>
        /// <param name="tag">Exif tag.</param>
        /// <param name="value">Value of data.</param>
        public void WriteExif(string filename, string tag, string value)
        {
            var tags = new Dictionary<string, string>
            {
                { tag, value }
            };
            WriteExif(filename, tags);
        }

        /// <summary>
        /// Parse the input name and directory to get a image file to save
        /// </summary>
        /// <param name="directory">Directory to save still</param>
        /// <param name="name">Name of still</param>
        /// <returns>Parsed directory and file name</returns>
        private FileInfo GetFile(DirectoryInfo directory, string name)
        {
            var processedDirectory = directory.FullName.Replace("\\", "/").Replace(DroidSystem.BaseDirectory, string.Empty).TrimStart('/');
            var dirPath = new DirectoryInfo(System.IO.Path.Combine(DroidSystem.BaseDirectory, processedDirectory));

            var stillFileName = $"{name}." + (ImageFormat == ImageFormatType.Jpeg ? "jpg" : "dng");
            var stillPath = new Java.IO.File(System.IO.Path.Combine(dirPath.FullName, stillFileName));

            return new FileInfo(stillPath.AbsolutePath).ParseFileAddSeqNum();
        }

        /// <summary>
        /// Save the image on its own thread
        /// </summary>
        /// <param name="file">File to save the image</param>
        /// <param name="result">Result from the capture event</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SaveImage(FileInfo file, ICaptureResult result)
        {
            var image = await Helpers.Camera.GetImageAsync(ImageReader);

            if (image == null)
            {
                throw new ArgumentNullException("Image was null");
            }

            OnNotify("Saving...");

            if (ImageFormat == ImageFormatType.RawSensor)
            {
                imageSaver.SaveRaw(characteristics, result, image, file);
                PictureTaken?.Invoke(this, file.FullName);
            }
            else
            {
                await Task.Run(() =>
               {
                   imageSaver.SaveJpeg(image, file);

                   if (IsBursting == true)
                   {
                       burstCount++;
                       if (burstCount % 25 == 0)
                       {
                           CheckImage();
                       }
                   }
                   else
                   {
                       CheckImage();
                   }

                   PictureTaken?.Invoke(this, file.FullName);

                   void CheckImage()
                   {
                       if (GreenPictureDetector.ImageIsGreen(file))
                       {
                           OnNotify("Warning:  Empty data found in image.", MessageTypes.Error);
                       }
                   }
               });
            }
        }

        /// <summary>
        /// Save the image on its own thread
        /// </summary>
        /// <param name="file">File to save the image</param>
        /// <param name="result">Result from the capture event</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SaveImageAsync(FileInfo file, ICaptureResult result) => await Task.Run(() => SaveImage(file, result));

        /// <summary>
        /// Take a still with the name of the supplied file, assumes the directory exists
        /// </summary>
        /// <param name="builder">Builder to use to set all the picture properties</param>
        /// <param name="file">File to save the image</param>
        /// <returns>True if the picture was taken, false if it failed</returns>
        /// <exception cref="DirectoryNotFoundException">Throws if directory doesn't exist</exception>
        /// <exception cref="Exception">Throws if the image failed to acquire from the buffer</exception>
        private async Task TakeStillAsync(ICaptureBuilder builder, FileInfo file)
        {
            builder.AddTarget(new SubCSurface(ImageReader.Surface));

            if (captureSession.Surfaces.ContainsKey(SurfaceTypes.Recording))
            {
                builder.AddTarget(captureSession.Surfaces[SurfaceTypes.Recording]);
            }

            builder.Set(new SubCCaptureRequestKey(CaptureRequest.JpegQuality).Key, (sbyte)JPEGQuality);

            if (!file.Directory.Exists)
            {
                throw new DirectoryNotFoundException("Create directory before calling");
            }

            CaptureEventArgs result;

            try
            {
                result = await captureSession.Capture(builder);
            }
            catch
            {
                OnNotify($"Picture: {file} failed to save. Capture failed. Please try again.", MessageTypes.Error);
                return;
            }

            if (result == null && (!IsBursting ?? false))
            {
                OnNotify($"Picture: {file} failed to save. Capture result failed. Please try again.", MessageTypes.Error);
                return;
            }

            StillCaptured?.Invoke(this, file.FullName);

            try
            {
                if ((IsBursting ?? false) && ImageFormat == ImageFormatType.Jpeg)
                {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    SaveImage(file, result.CaptureResult);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
                else
                {
                    await SaveImage(file, result.CaptureResult);

                    if (!IsBursting ?? false)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        Task.Run(() =>
                        {
                            var thumbnail = new Thumbnail(file).GenerateThumbnail();
                            OnNotify($"Thumbnail saved: {thumbnail}");
                        });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                }
            }
            catch (ArgumentNullException)
            {
                throw new Exception($"Picture: {file.Name} failed to save. Unable to acquire buffer. Please try again.");
            }
        }

        /// <summary>
        /// Create a new reader if it's null, or any of the properties changed
        /// </summary>
        private async void UpdateReader()
        {
            lock (ReaderSync)
            {
                var resolution = ImageFormat == ImageFormatType.Jpeg ? jpegResolution : rawResolution;

                if (ImageReader != null
                    && ImageReader.ImageFormat == ImageFormat
                    && ImageReader.Width == resolution.Width
                    && ImageReader.Height == resolution.Height)
                {
                    return;
                }

                // cleans out Reader so we don't have to wait for GC
                ImageReader?.Close();
                ImageReader = imageReaderGrabber.NewInstance(resolution.Width, resolution.Height, ImageFormat, 2);
            }

            await captureSession.UpdateSurface(SurfaceTypes.Still, new SubCSurface(ImageReader.Surface));

            try
            {
                // The below line was commented out in a commit labeled 'Don't change resolution on Grenadier (Mar 6, 2019) but this code is executed also on rayfin and froze the feed when switching to jpeg.
                // If it needs to be commented out again please leave a comment about why.
                captureSession.Repeat();
            }
            catch (CameraAccessException e)
            {
                OnNotify("The camera has encountered a fatal error\nRebooting...", MessageTypes.Error);
                SubCLogger.Instance.Write($"{DateTime.Now}:{e.Message}\n\n", "CE3.log", @"/storage/emulated/0/Logs/");
                Thread.Sleep(500);
                DroidSystem.ShellSync("reboot");
            }
        }

        /// <summary>
        /// Changes the stillname
        /// </summary>
        /// <param name="value">String that contains the desired name for the images</param>
        /// <returns>Returns itself unless the name is invalid, then it returns the default name</returns>
        private string UpdateStillName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return DefaultStillName;
            }

            value = value.RemoveIllegalFileOrFolderCharacters(FileSystem.Any, true);
            return value.Substring(0, Math.Min(value.Length, ValidFilename.MaxLength));
        }
    }
}