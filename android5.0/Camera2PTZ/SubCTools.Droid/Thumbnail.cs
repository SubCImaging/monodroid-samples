//-----------------------------------------------------------------------
// <copyright file="Thumbnail.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid
{
    using Android.App;
    using Android.Content;
    using Android.Graphics;
    using Android.Media;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Droid.Enums;
    using SubCTools.Droid.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Creates a thumbnail of an image
    /// </summary>
    public class Thumbnail : DroidBase
    {
        /// <summary>
        /// The default location to store raw thumbnails relative to the location of the original still
        /// </summary>
        private const string RawThumbnailsSubdirectory = "RawThumbnails";

        /// <summary>
        /// The default location to store jpeg thumbnails relative to the location of the original still
        /// </summary>
        private const string ThumbnailsSubdirectory = "Thumbnails";

        /// <summary>
        /// The path to the original still file
        /// </summary>
        private string sourceFile;

        /// <summary>
        /// the path to the thumbnail
        /// </summary>
        private string targetFile;

        /// <summary>
        /// the thumbnail height in pixels
        /// </summary>
        private int thumbHeight;

        /// <summary>
        /// the thumbnail width in pixels
        /// </summary>
        private int thumbWidth;

        /// <summary>
        /// Initializes a new instance of the <see cref="Thumbnail"/> class.
        /// </summary>
        /// <param name="sourcePath">original still</param>
        /// <param name="destinationPath">the location to store the thumbnail</param>
        /// <param name="width">The width in pixels to make the thumbnail</param>
        /// <param name="height">The height in pixels to make the thumbnail</param>
        public Thumbnail(string sourcePath, string destinationPath, int width, int height)
        {
            ThumbWidth = width;
            ThumbHeight = height;

            SourceFile = sourcePath;
            TargetFile = destinationPath;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Thumbnail"/> class.
        /// </summary>
        /// <param name="sourcePath">original still</param>
        /// <param name="destinationPath">the location to store the thumbnail</param>
        public Thumbnail(string sourcePath, string destinationPath)
            : this(sourcePath, destinationPath, 110, 80)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Thumbnail"/> class.
        /// </summary>
        /// <param name="sourcePath">original still</param>
        public Thumbnail(FileInfo sourcePath)
            : this(sourcePath.FullName, System.IO.Path.Combine(sourcePath.DirectoryName, sourcePath.Name.ToLower().EndsWith(".dng") ? RawThumbnailsSubdirectory : ThumbnailsSubdirectory, sourcePath.Name))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Thumbnail"/> class.
        /// </summary>
        /// <param name="sourcePath">original still</param>
        /// <param name="width">The width in pixels to make the thumbnail</param>
        /// <param name="height">The height in pixels to make the thumbnail</param>
        public Thumbnail(FileInfo sourcePath, int width, int height) : this(
                    sourcePath.FullName,
                    System.IO.Path.Combine(sourcePath.DirectoryName, sourcePath.Name.ToLower().EndsWith(".dng") ? RawThumbnailsSubdirectory : ThumbnailsSubdirectory, sourcePath.Name),
                    width,
                    height)
        {
        }

        /// <summary>
        /// Gets the path to the original file
        /// </summary>
        public string SourceFile
        {
            get => sourceFile;
            private set => Set(nameof(SourceFile), ref sourceFile, value);
        }

        /// <summary>
        /// Gets the path to the thumbnail
        /// </summary>
        public string TargetFile
        {
            get => targetFile;
            private set => Set(nameof(TargetFile), ref targetFile, value);
        }

        /// <summary>
        /// Gets the thumbnail height in pixels
        /// </summary>
        public int ThumbHeight
        {
            get => thumbHeight;
            private set => Set(nameof(ThumbHeight), ref thumbHeight, value);
        }

        /// <summary>
        /// Gets the thumbnail width in pixels
        /// </summary>
        public int ThumbWidth
        {
            get => thumbWidth;
            private set => Set(nameof(ThumbWidth), ref thumbWidth, value);
        }

        /// <summary>
        /// Generates the thumbnail file
        /// </summary>
        /// <returns>The path to the thumbnail</returns>
        public string GenerateThumbnail()
        {
            // thumbnails are jpegs so this switches the extension if neccessary
            if (TargetFile.ToLower().EndsWith(".dng"))
            {
                TargetFile = TargetFile.Replace(".dng", ".jpg");
            }

            if (File.Exists(TargetFile))
            {
                File.Delete(TargetFile);
            }

            if (!File.Exists(TargetFile) && File.Exists(SourceFile))
            {
                var options = new BitmapFactory.Options()
                {
                    InJustDecodeBounds = false,
                    InPurgeable = true,
                };

                using (var image = BitmapFactory.DecodeFile(SourceFile, options))
                {
                    if (image != null)
                    {
                        var sourceSize = new Size((int)image.GetBitmapInfo().Width, (int)image.GetBitmapInfo().Height);

                        var maxResizeFactor = Math.Min((double)ThumbWidth / sourceSize.Width, (double)ThumbHeight / sourceSize.Height);

                        (new FileInfo(TargetFile)).Directory.CreateIfMissing();

                        var width = (int)(maxResizeFactor * sourceSize.Width);
                        var height = (int)(maxResizeFactor * sourceSize.Height);

                        using (var bitmapScaled = Android.Graphics.Bitmap.CreateScaledBitmap(image, width, height, true))
                        {
                            using (System.IO.Stream outStream = File.Create(TargetFile))
                            {
                                if (TargetFile.ToLower().EndsWith("png"))
                                {
                                    bitmapScaled.Compress(Android.Graphics.Bitmap.CompressFormat.Png, 100, outStream);
                                }
                                else
                                {
                                    bitmapScaled.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, 60, outStream);
                                }
                            }

                            bitmapScaled.Recycle();
                        }

                        image.Recycle();

                        if (DroidSystem.StorageType == RayfinStorageType.NAS)
                        {
                            DroidSystem.UserChmod(777, TargetFile);
                        }

                        return TargetFile;
                    }
                }

                return string.Empty;
            }

            return string.Empty;
        }
    }
}