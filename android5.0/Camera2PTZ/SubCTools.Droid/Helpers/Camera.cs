using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.IO;
using Java.Lang;
using Java.Util;
using SubCTools.Droid.Callbacks;
using SubCTools.Droid.Enums;
using SubCTools.Droid.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubCTools.Droid.Helpers
{
    /// <summary>
    /// A class for interacting with the camera media capture functionality.
    /// </summary>
    public static class Camera
    {
        /// <summary>
        /// A default filename string.
        /// </summary>
        public const string DefaultFileName = "yyyy-MM-dd - hh:mm:ss.SSS";

        /// <summary>
        /// Helper to scale down the still thumbnail
        /// </summary>
        /// <param name="options"><see cref="BitmapFactory"/> options</param>
        /// <param name="reqWidth">Required width.</param>
        /// <param name="reqHeight">Required height.</param>
        /// <returns>Size of the sample in bytes.</returns>
        public static int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
        {
            int height = options.OutHeight;
            int width = options.OutWidth;
            int inSampleSize = 1;

            if (height > reqHeight || width > reqWidth)
            {
                int halfHeight = height / 2;
                int halfWidth = width / 2;

                while ((halfHeight / inSampleSize) >= reqHeight && (halfWidth / inSampleSize) >= reqWidth)
                {
                    inSampleSize *= 2;
                }
            }

            return inSampleSize;
        }

        /// <summary>
        /// Returns true if permissions allow the supplied file to be written.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>A bool.</returns>
        public static bool CanWriteToDirectory(FileInfo file)
        {
            // make a new folder with the same name as the file to preserve length, remove the extension and add temp in front.
            var javaFile = new Java.IO.File(file.FullName);
            var success = false;

            try
            {
                var counter = 0l;
                while (javaFile.Exists())
                {
                    var newName = $@"{javaFile.Parent}/{counter.ToString()}_{file.Name}";
                    if (!javaFile.RenameTo(new Java.IO.File(newName)))
                    {
                        return false;
                    }

                    counter++;
                }

                success = javaFile.CreateNewFile();
                if (success)
                {
                    javaFile.Delete();
                    return true;
                }
            }
            catch
            {
            }

            return success;
        }

        /// <summary>
        /// Choose the optimal size for ... something?
        /// </summary>
        /// <param name="choices"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="aspectRatio"></param>
        /// <returns></returns>
        public static Size ChooseOptimalSize(Size[] choices, int width, int height, Size aspectRatio)
        {
            var bigEnough = new List<Size>();
            int w = aspectRatio.Width;
            int h = aspectRatio.Height;
            foreach (Size option in choices)
            {
                if (option.Height == option.Width * h / w && option.Width >= width && option.Height >= height)
                {
                    bigEnough.Add(option);
                }
            }

            if (bigEnough.Count > 0)
            {
                return (Size)Collections.Min(bigEnough, new CompareSizesByArea());
            }
            else
            {
                return choices[0];
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="choices"></param>
        /// <returns></returns>
        public static Size ChooseVideoSize(Size[] choices)
        {
            foreach (Size size in choices)
            {
                if (size.Width == size.Height * 4 / 3 && size.Width <= 1000)
                {
                    return size;
                }
            }

            return choices[choices.Length - 1];
        }

        /// <summary>
        /// Takes a list of jpegs and converts them to a list of bitmaps.
        /// </summary>
        /// <param name="jpeg_images">list of jpegs.</param>
        /// <returns>list of bitmaps.</returns>
        public static List<Bitmap> ConvertToBitmaps(List<byte[]> jpeg_images)
        {
            BitmapFactory.Options mutable_options = new BitmapFactory.Options();
            mutable_options.InMutable = true; // first bitmap needs to be writable
            BitmapFactory.Options options = new BitmapFactory.Options();
            options.InMutable = false; // later bitmaps don't need to be writable

            LoadBitmapThread[] threads = new LoadBitmapThread[jpeg_images.Count];
            for (int i = 0; i < jpeg_images.Count; i++)
            {
                threads[i] = new LoadBitmapThread(i == 0 ? mutable_options : options, jpeg_images[i]);
            }

            // start threads
            for (int i = 0; i < jpeg_images.Count; i++)
            {
                threads[i].Start();
            }

            // wait for threads to complete
            bool ok = true;

            try
            {
                for (int i = 0; i < jpeg_images.Count; i++)
                {
                    threads[i].Join();
                }
            }
            catch (InterruptedException e)
            {
                ok = false;
            }

            List<Bitmap> bitmaps = new List<Bitmap>();
            for (int i = 0; i < jpeg_images.Count && ok; i++)
            {
                Bitmap bitmap = threads[i].Bitmap;
                if (bitmap == null)
                {
                    ok = false;
                }

                bitmaps.Add(bitmap);
            }

            if (!ok)
            {
                for (int i = 0; i < jpeg_images.Count; i++)
                {
                    if (threads[i].Bitmap != null)
                    {
                        threads[i].Bitmap.Recycle();
                        threads[i].Bitmap = null;
                    }
                }

                bitmaps.Clear();
                GC.Collect();
                return null;
            }

            return bitmaps;
        }

        /// <summary>
        /// Creates a new directory for storing media.
        /// </summary>
        /// <param name="file">A file containing the path.</param>
        /// <returns>A directory info.</returns>
        public static DirectoryInfo CreateNewMediaDirectory(Java.IO.File file)
        {
            var currentDirString = file.Parent.Remove(0, file.Parent.Length - 3);
            var parent = new DirectoryInfo(file.Parent);
            int i = 1;
            if (!int.TryParse(currentDirString, out i))
            {
                i = 1;
            }

            // loop through, keep increasing the number until you find one that's empty
            DirectoryInfo newDir;
            while ((newDir = new DirectoryInfo(System.IO.Path.Combine(parent.Parent.ToString(), i.ToString("000")))).Exists)
            {
                if (CanWriteToDirectory(new FileInfo(System.IO.Path.Combine(newDir.FullName, file.Name))))
                {
                    break;
                }

                i++;

                if (i > 999)
                {
                    // you've run out of directory, fail!
                    throw new System.Exception("Ran out of subdirectories");
                }
            }

            // you can create the new directory
            newDir.Create();

            return newDir;
        }

        /// <summary>
        /// Creates a new directory for storing media.
        /// </summary>
        /// <param name="file">A file containing the path.</param>
        /// <returns>A directory info.</returns>
        public static DirectoryInfo CreateNewMediaDirectory(FileInfo file)
        {
            var directory = file.Directory.ToString();
            var filename = file.Name;
            var diskSpaceManager = new DiskSpaceManager(new AndroidDiskSpaceMonitor());
            var parent = new DirectoryInfo(directory).Parent;
            var dummyFile = new Java.IO.File(System.IO.Path.Combine(DroidSystem.BaseDirectory, directory.TrimStart('/'), filename));
            int i = 1;
            int.TryParse(directory.Remove(0, directory.Length - 3), out i);

            // loop through, keep increasing the number until you find one that's empty
            DirectoryInfo newDir;
            while ((newDir = new DirectoryInfo(System.IO.Path.Combine(DroidSystem.BaseDirectory, parent.Name, i.ToString("000")))).Exists)
            {
                if (CanWriteToDirectory(new FileInfo(System.IO.Path.Combine(newDir.FullName, filename))))
                {
                    break;
                }

                i++;

                if (i > 999)
                {
                    // you've run out of directory, fail!
                    throw new System.Exception("Ran out of subdirectories");
                }
            }

            // you can create the new directory
            newDir.Create();

            return new DirectoryInfo(System.IO.Path.Combine(parent.FullName, i.ToString("000")));
        }

        /// <summary>
        /// Helper to scale down the still thumbnail
        /// </summary>
        /// <param name="path">Image file path.</param>
        /// <param name="reqWidth">Required width.</param>
        /// <param name="reqHeight">Required height.</param>
        /// <returns>A Bitmap.</returns>
        public static Bitmap DecodeSampledBitmapFromResource(string path, int reqWidth, int reqHeight)
        {
            var options = new BitmapFactory.Options()
            {
                InJustDecodeBounds = true
            };

            BitmapFactory.DecodeFile(path, options);

            options.InSampleSize = CalculateInSampleSize(options, reqWidth, reqHeight);

            options.InJustDecodeBounds = false;
            return BitmapFactory.DecodeFileAsync(path, options).Result;
        }

        /// <summary>
        /// Grabs an image from the <see cref="IImageReader"/>.
        /// </summary>
        /// <param name="reader">the ImageReader</param>
        /// <returns>A task of Image.</returns>
        public static async Task<Image> GetImageAsync(IImageReader reader) => await Task.Run(async () =>
        {
            Image i = null;

            var attempts = 0;

            while ((i = reader.AcquireLatestImage()) == null && attempts < 200)
            {
                await Task.Delay(10);
                attempts++;
            }

            return i;
        });

        /// <summary>
        /// Converts a resolution string into a <see cref="Size"/>.
        /// </summary>
        /// <param name="resolution">A resolution string.</param>
        /// <returns>A <see cref="Size"/> object.</returns>
        public static Size GetResolution(string resolution)
        {
            var match = Regex.Match(resolution, @"(\d{3,4})x(\d{3,4})");
            return match.Success
                ? new Size(Convert.ToInt32(match.Groups[1].Value), Convert.ToInt32(match.Groups[2].Value))
                : null;
        }

        /// <summary>
        /// Returns true if the given resolution string is a valid resolution.
        /// </summary>
        /// <param name="resolution">a string representing the resolution.</param>
        /// <param name="resolutions">A list of valid resolutions.</param>
        /// <returns>True if the resolution is valid.</returns>
        public static bool IsValidResolution(string resolution, IEnumerable<Size> resolutions)
        {
            var resolutionSize = GetResolution(resolution);

            if (resolutionSize == null)
            {
                //this.OnNotify($"{resolution} is in improper format");
                return false;
            }

            if (!resolutions.Any(s => s.Width == resolutionSize.Width && s.Height == resolutionSize.Height))
            {
                //this.OnNotify($"{resolution} is invalid, please select a resolution from the resolutions list");
                return false;
            }

            return true;
        }

        //public static void ConfigureTransform(this TextureView mTextureView, int viewWidth, int viewHeight, )
        //{
        //    //Activity activity = Activity;
        //    if (null == mTextureView || null == mPreviewSize || null == activity)
        //    {
        //        return;
        //    }
        //    //var rotation = (int)activity.WindowManager.DefaultDisplay.Rotation;
        //    Matrix matrix = new Matrix();
        //    RectF viewRect = new RectF(0, 0, viewWidth, viewHeight);
        //    RectF bufferRect = new RectF(0, 0, mPreviewSize.Height, mPreviewSize.Width);
        //    float centerX = viewRect.CenterX();
        //    float centerY = viewRect.CenterY();
        //    if ((int)SurfaceOrientation.Rotation90 == rotation || (int)SurfaceOrientation.Rotation270 == rotation)
        //    {
        //        bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
        //        matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
        //        float scale = Math.Max((float)viewHeight / mPreviewSize.Height, (float)viewWidth / mPreviewSize.Width);
        //        matrix.PostScale(scale, scale, centerX, centerY);
        //        matrix.PostRotate(90 * (rotation - 2), centerX, centerY);
        //    }
        //    else if ((int)SurfaceOrientation.Rotation180 == rotation)
        //    {
        //        matrix.PostRotate(180, centerX, centerY);
        //    }
        //    mTextureView.SetTransform(matrix);
        //}

        /// <summary>
        /// Gets a <see cref="CameraDevice"/> from a <see cref="Context"/> and a cameraId.
        /// </summary>
        /// <param name="cameraId">Camera Id.</param>
        /// <param name="context">Android Context.</param>
        /// <param name="backgroundHandler">A background handler.</param>
        /// <returns>A task of CameraDevice.</returns>
        public static Task<CameraDevice> OpenCameraAsync(string cameraId, Context context, Handler backgroundHandler = null)
            => OpenCameraAsync(cameraId, (CameraManager)context.GetSystemService(Context.CameraService), new CameraStateCallback(), backgroundHandler);

        /// <summary>
        /// Gets a <see cref="CameraDevice"/> from a <see cref="Context"/> and a cameraId.
        /// </summary>
        /// <param name="cameraId">Camera Id.</param>
        /// <param name="context">Android Context.</param>
        /// <param name="stateCallback">A <see cref="CameraStateCallback"/>.</param>
        /// <param name="backgroundHandler">A background handler.</param>
        /// <returns>A task of CameraDevice.</returns>
        public static Task<CameraDevice> OpenCameraAsync(string cameraId, Context context, CameraStateCallback stateCallback, Handler backgroundHandler = null)
            => OpenCameraAsync(cameraId, (CameraManager)context.GetSystemService(Context.CameraService), stateCallback, backgroundHandler);

        /// <summary>
        /// Gets a <see cref="CameraDevice"/> from a <see cref="Context"/> and a cameraId.
        /// </summary>
        /// <param name="cameraId">Camera Id.</param>
        /// <param name="cameraManager">A <see cref="CameraManager"/>.</param>
        /// <returns>A task of CameraDevice.</returns>
        public static Task<CameraDevice> OpenCameraAsync(string cameraId, CameraManager cameraManager)
        {
            // create a new background thread to open the camera on
            var backgroundThread = new HandlerThread("CameraBackground");
            backgroundThread.Start();
            var handler = new Handler(backgroundThread.Looper);

            return OpenCameraAsync(cameraId, cameraManager, new CameraStateCallback(), handler);
        }

        /// <summary>
        /// Gets a <see cref="CameraDevice"/> from a <see cref="Context"/> and a cameraId.
        /// </summary>
        /// <param name="cameraId">Camera Id.</param>
        /// <param name="cameraManager">A <see cref="CameraManager"/>.</param>
        /// <param name="backgroundHandler">A background handler.</param>
        /// <returns>A task of CameraDevice.</returns>
        public static Task<CameraDevice> OpenCameraAsync(string cameraId, CameraManager cameraManager, Handler backgroundHandler)
            => OpenCameraAsync(cameraId, cameraManager, new CameraStateCallback(), backgroundHandler);

        /// <summary>
        /// Gets a <see cref="CameraDevice"/> from a <see cref="Context"/> and a cameraId.
        /// </summary>
        /// <param name="cameraId">Camera Id.</param>
        /// <param name="cameraManager">A <see cref="CameraManager"/>.</param>
        /// <param name="stateCallback">A <see cref="CameraStateCallback"/>.</param>
        /// <param name="backgroundHandler">A background handler.</param>
        /// <returns>A task of CameraDevice.</returns>
        public static async Task<CameraDevice> OpenCameraAsync(string cameraId, CameraManager cameraManager, CameraStateCallback stateCallback, Handler backgroundHandler)
        {
            var tcs = new TaskCompletionSource<CameraDevice>();
            var handler = new EventHandler<CameraDevice>((s, e) =>
            {
                tcs.SetResult(e);
            });

            stateCallback.Opened += handler;

            try
            {
                cameraManager.OpenCamera(cameraId, stateCallback, backgroundHandler);
            }
            catch (Java.IO.IOException e)
            {
                throw new System.Exception(e.ToString());
            }

            var c = await tcs.Task;

            stateCallback.Opened -= handler;

            return c;
        }

        public static bool SaveImage(FileInfo file, byte[] bytes)
        {
            if (file == null)
            {
                throw new ArgumentNullException("File cannot be null");
            }

            if (!new Java.IO.File(file.DirectoryName).Exists())
            {
                throw new DirectoryNotFoundException("Create directory before trying to save");
            }

            using (var output = new FileOutputStream(new Java.IO.File(file.FullName)))
            {
                try
                {
                    output.Write(bytes);
                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    output?.Close();
                }
            }
        }

        public static void TakeBitmap(this TextureView preview, int width, int height, bool filter)
        {
            var bmp = Bitmap.CreateScaledBitmap(preview.GetBitmap(640, 480), 640, 360, filter);

            var filename = System.IO.Path.Combine(DroidSystem.BaseDirectory, "Stills", "BUMP.png");

            System.IO.FileStream fo = null;
            try
            {
                fo = new System.IO.FileStream(filename, FileMode.OpenOrCreate);
                bmp.Compress(Bitmap.CompressFormat.Png, 100, fo); // bmp is your Bitmap instance
                                                                  // PNG is a lossless format, the compression factor (100) is ignored
            }
            catch (System.Exception e)
            {
                //e.printStackTrace();
            }
            finally
            {
                try
                {
                    if (fo != null)
                    {
                        fo.Close();
                    }
                }
                catch //(IOException e)
                {
                    //e.printStackTrace();
                }
            }
        }

        //public static bool RemoteSave(FileInfo file, byte[] bytes)
        //{
        //    if (file == null) return false;
        //    var name = file.FullName.Split('/')[file.FullName.Split('/').Length - 1];
        //    return true;
        //}
        public static void TransformImage(this TextureView textureView, int viewWidth, int viewHeight, Activity activity, Size previewSize)
            => textureView.TransformImage(viewWidth, viewHeight, activity.WindowManager.DefaultDisplay.Rotation, previewSize);

        /// <summary>
        ///
        /// </summary>
        /// <param name="viewWidth"></param>
        /// <param name="viewHeight"></param>
        public static void TransformImage(
            this TextureView textureView,
            int viewWidth,
            int viewHeight,
            SurfaceOrientation rotation,
            Size previewSize)
        {
            if (previewSize == null || textureView == null)
            {
                return;
            }

            var matrix = new Matrix();

            var viewRect = new RectF(0, 0, previewSize.Width, previewSize.Height);//viewWidth, viewHeight);
            //var bufferRect = new RectF(0, 0, previewSize.Height, previewSize.Width);

            var centerX = viewRect.CenterX();
            var centerY = viewRect.CenterY();
            //if (rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270)
            //{
            //bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
            //matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
            //float scale = System.Math.Max((float)viewHeight / previewSize.Height, (float)viewHeight / previewSize.Width);
            //matrix.PostScale(scale, scale, centerX, centerY);
            //matrix.PostRotate(90 * ((int)rotation - 2), centerX, centerY);
            //}

            //matrix.PostScale(-1, 1, centerX, centerY);

            matrix.SetScale(0, 0, centerX, centerY);

            textureView.SetTransform(matrix);
        }

        public static void WriteJpeg(Image image, FileInfo file)
        {
            var buffer = image.GetPlanes().First().Buffer;
            var bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);

            Camera.SaveImage(file, bytes);

            image.Close();
        }

        public static void WriteRaw(
            CameraCharacteristics characteristics,
            CaptureResult result,
            Image image,
            FileInfo file)
        {
            var dngCreator = new DngCreator(characteristics, result);

            using (var output = file.OpenWrite())
            {
                try
                {
                    dngCreator.WriteImage(output, image);
                }
                catch (Java.IO.IOException e)
                {
                    //OnNotify($"Camera capture session {e.StackTrace}");
                }
                finally
                {
                    output.Close();
                }
            }

            image.Close();
        }
    }
}