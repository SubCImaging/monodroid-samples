using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Renderscripts;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace SubCTools.Droid.Camera
{
    internal static class DateTimeHelperClass
    {
        private static readonly System.DateTime Jan1st1970 = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        internal static long CurrentUnixTimeMillis()
        {
            return (long)(System.DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }
    }

    public class HDRProcessor
    {
        private const string TAG = "HDRProcessor";

        private Context context = null;
        private RenderScript rs = null; // lazily created, so we don't take up resources if application isn't using HDR

        private enum HDRAlgorithm
        {
            HDRALGORITHM_AVERAGE,
            HDRALGORITHM_STANDARD
        }

        public HDRProcessor(Context context)
        {
            this.context = context;
        }

        internal virtual void onDestroy()
        {
            // need to destroy context, otherwise this isn't necessarily garbage collected - we had tests failing with out of memory
            // problems e.g. when running MainTests as a full set with Camera2 API. Although we now reduce the problem by creating
            // the rs lazily, it's still good to explicitly clear.
            rs?.Destroy(); // on Android M onwards this is a NOP - instead we call RenderScript.releaseAllContexts(); in MainActivity.onDestroy()

        }

        /// <summary>
        /// Given a set of data Xi and Yi, this function estimates a relation between X and Y
        ///  using linear least squares.
        ///  We use it to modify the pixels of images taken at the brighter or darker exposure
        ///  levels, to estimate what the pixel should be at the "base" exposure.
        /// </summary>
        private class ResponseFunction
        {
            internal float parameter_A = 0.0f;
            internal float parameter_B = 0.0f;

            /// <summary>
            /// Computes the response function.
            /// We pass the context, so this inner class can be made static. </summary>
            /// <param name="x_samples"> List of Xi samples. Must be at least 3 samples. </param>
            /// <param name="y_samples"> List of Yi samples. Must be same length as x_samples. </param>
            /// <param name="weights"> List of weights. Must be same length as x_samples. </param>
            internal ResponseFunction(Context context, int id, IList<double?> x_samples, IList<double?> y_samples, IList<double?> weights)
            {
                if (x_samples.Count != y_samples.Count)
                {

                    // throw RuntimeException, as this is a programming error
                    throw new Java.Lang.Exception();
                }
                else if (x_samples.Count != weights.Count)
                {

                    // throw RuntimeException, as this is a programming error
                    throw new Java.Lang.Exception();
                }
                else if (x_samples.Count <= 3)
                {
                    // throw RuntimeException, as this is a programming error
                    throw new Java.Lang.Exception();
                }

                // linear Y = AX + B
                bool done = false;
                double sum_wx = 0.0;
                double sum_wx2 = 0.0;
                double sum_wxy = 0.0;
                double sum_wy = 0.0;
                double sum_w = 0.0;
                for (int i = 0; i < x_samples.Count; i++)
                {
                    double x = x_samples[i].Value;
                    double y = y_samples[i].Value;
                    double w = weights[i].Value;
                    sum_wx += w * x;
                    sum_wx2 += w * x * x;
                    sum_wxy += w * x * y;
                    sum_wy += w * y;
                    sum_w += w;
                }

                double A_numer = sum_wy * sum_wx - sum_w * sum_wxy;
                double A_denom = sum_wx * sum_wx - sum_w * sum_wx2;

                if (Java.Lang.Math.Abs(A_denom) < 1.0e-5)
                {

                    // will fall back to linear Y = AX
                }
                else
                {
                    parameter_A = (float)(A_numer / A_denom);
                    parameter_B = (float)((sum_wy - parameter_A * sum_wx) / sum_w);

                    // we don't want a function that is not monotonic, or can be negative!
                    if (parameter_A < 1.0e-5)
                    {

                    }
                    else if (parameter_B < 1.0e-5)
                    {

                    }
                    else
                    {
                        done = true;
                    }
                }

                if (!done)
                {

                    // linear Y = AX
                    double numer = 0.0;
                    double denom = 0.0;
                    for (int i = 0; i < x_samples.Count; i++)
                    {
                        double x = x_samples[i].Value;
                        double y = y_samples[i].Value;
                        double w = weights[i].Value;
                        numer += w * x * y;
                        denom += w * x * x;
                    }


                    if (denom < 1.0e-5)
                    {

                        parameter_A = 1.0f;
                    }
                    else
                    {
                        parameter_A = (float)(numer / denom);
                        // we don't want a function that is not monotonic!
                        if (parameter_A < 1.0e-5)
                        {

                            parameter_A = 1.0e-5f;
                        }
                    }
                    parameter_B = 0.0f;
                }
            }
        }

        /// <summary>
        /// Converts a list of bitmaps into a HDR image, which is then tonemapped to a final RGB image. </summary>
        /// <param name="bitmaps"> The list of bitmaps, which should be in order of increasing brightness (exposure).
        ///                The resultant image is stored in the first bitmap. The remainder bitmaps will have
        ///                recycle() called on them.
        ///                Currently only supports a list of 3 images, the 2nd should be at the desired exposure
        ///                level for the resultant image.
        ///                The bitmaps must all be the same resolution. </param>
        public virtual void processHDR(IList<Bitmap> bitmaps)
        {

            int n_bitmaps = bitmaps.Count;
            if (n_bitmaps != 3)
            {

                // throw RuntimeException, as this is a programming error
                throw new Java.Lang.Exception();
            }
            for (int i = 1; i < n_bitmaps; i++)
            {
                if (bitmaps[i].Width != bitmaps[0].Width || bitmaps[i].Height != bitmaps[0].Height)
                {

                    throw new Java.Lang.Exception();
                }
            }

            //final HDRAlgorithm algorithm = HDRAlgorithm.HDRALGORITHM_AVERAGE;
            const HDRAlgorithm algorithm = HDRAlgorithm.HDRALGORITHM_STANDARD;

            switch (algorithm)
            {
                case HDRAlgorithm.HDRALGORITHM_AVERAGE:
                    processHDRAverage(bitmaps);
                    break;
                case HDRAlgorithm.HDRALGORITHM_STANDARD:
                    processHDRCore(bitmaps);
                    break;
                default:

                    // throw RuntimeException, as this is a programming error
                    throw new Java.Lang.Exception();
            }
        }

        /// <summary>
        /// Creates a ResponseFunction to estimate how pixels from the in_bitmap should be adjusted to
        ///  match the exposure level of out_bitmap.
        /// </summary>
        private ResponseFunction createFunctionFromBitmaps(int id, Bitmap in_bitmap, Bitmap out_bitmap)
        {

            IList<double?> x_samples = new List<double?>();
            IList<double?> y_samples = new List<double?>();
            IList<double?> weights = new List<double?>();

            const int n_samples_c = 100;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int n_w_samples = (int)Math.sqrt(n_samples_c);
            int n_w_samples = (int)Java.Lang.Math.Sqrt(n_samples_c);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int n_h_samples = n_samples_c/n_w_samples;
            int n_h_samples = n_samples_c / n_w_samples;

            double avg_in = 0.0;
            double avg_out = 0.0;
            for (int y = 0; y < n_h_samples; y++)
            {
                double alpha = ((double)y + 1.0) / ((double)n_h_samples + 1.0);
                int y_coord = (int)(alpha * in_bitmap.Height);
                for (int x = 0; x < n_w_samples; x++)
                {
                    double beta = ((double)x + 1.0) / ((double)n_w_samples + 1.0);
                    int x_coord = (int)(beta * in_bitmap.Width);
                    /*if( MyDebug.LOG )
						Log.d(TAG, "sample response from " + x_coord + " , " + y_coord);*/
                    int in_col = in_bitmap.GetPixel(x_coord, y_coord);
                    int out_col = out_bitmap.GetPixel(x_coord, y_coord);
                    double in_value = averageRGB(in_col);
                    double out_value = averageRGB(out_col);
                    avg_in += in_value;
                    avg_out += out_value;
                    x_samples.Add(in_value);
                    y_samples.Add(out_value);
                }
            }
            avg_in /= x_samples.Count;
            avg_out /= x_samples.Count;
            bool is_dark_exposure = avg_in < avg_out;

            {
                // calculate weights
                double min_value = x_samples[0].Value;
                double max_value = x_samples[0].Value;
                for (int i = 1; i < x_samples.Count; i++)
                {
                    double value = x_samples[i].Value;
                    if (value < min_value)
                    {
                        min_value = value;
                    }
                    if (value > max_value)
                    {
                        max_value = value;
                    }
                }
                double med_value = 0.5 * (min_value + max_value);

                double min_value_y = y_samples[0].Value;
                double max_value_y = y_samples[0].Value;
                for (int i = 1; i < y_samples.Count; i++)
                {
                    double value = y_samples[i].Value;
                    if (value < min_value_y)
                    {
                        min_value_y = value;
                    }
                    if (value > max_value_y)
                    {
                        max_value_y = value;
                    }
                }
                double med_value_y = 0.5 * (min_value_y + max_value_y);

                for (int i = 0; i < x_samples.Count; i++)
                {
                    double value = x_samples[i].Value;
                    double value_y = y_samples[i].Value;
                    if (is_dark_exposure)
                    {
                        // for dark exposure, also need to worry about the y values (which will be brighter than x) being overexposed
                        double weight = (value <= med_value) ? value - min_value : max_value - value;
                        double weight_y = (value_y <= med_value_y) ? value_y - min_value_y : max_value_y - value_y;
                        if (weight_y < weight)
                        {
                            weight = weight_y;
                        }
                        weights.Add(weight);
                    }
                    else
                    {
                        double weight = (value <= med_value) ? value - min_value : max_value - value;
                        weights.Add(weight);
                    }
                }
            }

            return new ResponseFunction(context, id, x_samples, y_samples, weights);
        }

        /// <summary>
        /// Calculates average of RGB values for the supplied color.
        /// </summary>
        private double averageRGB(int color)
        {
            int r = (color & 0xFF0000) >> 16;
            int g = (color & 0xFF00) >> 8;
            int b = (color & 0xFF);
            return (r + g + b) / 3.0;
            //return 0.27*r + 0.67*g + 0.06*b;
        }

        /// <summary>
        /// Calculates the luminance for an RGB colour.
        /// </summary>
        /*private double calculateLuminance(double r, double g, double b) {
			double value = 0.27*r + 0.67*g + 0.06*b;
			return value;
		}*/

        /*final float A = 0.15f;
		final float B = 0.50f;
		final float C = 0.10f;
		final float D = 0.20f;
		final float E = 0.02f;
		final float F = 0.30f;
		final float W = 11.2f;
		
		float Uncharted2Tonemap(float x) {
			return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
		}*/

        /// <summary>
        /// Converts a HDR brightness to a 0-255 value. </summary>
        /// <param name="hdr"> The input HDR brightness. </param>
        /// //<param name="l_avg"> The log average luminance of the HDR image. That is, exp( sum{log(Li)}/N ). </param>
        private void tonemap(int[] rgb, float[] hdr)
        {
            // simple clamp:
            /*for(int i=0;i<3;i++) {
				rgb[i] = (int)hdr[i];
				if( rgb[i] > 255 )
					rgb[i] = 255;
			}*/
            /*
			// exponential:
			final double exposure_c = 1.2 / 255.0;
			int rgb = (int)(255.0*(1.0 - Math.exp(- hdr * exposure_c)));
			*/
            // Reinhard (Global):
            //final float scale_c = l_avg / 0.5f;
            //final float scale_c = l_avg / 0.8f; // lower values tend to result in too dark pictures; higher values risk over exposed bright areas
            //final float scale_c = l_avg / 1.0f;
            const float scale_c = 255.0f;
            //for(int i=0;i<3;i++)
            //	rgb[i] = (int)(255.0 * ( hdr[i] / (scale_c + hdr[i]) ));
            float max_hdr = hdr[0];
            if (hdr[1] > max_hdr)
            {
                max_hdr = hdr[1];
            }
            if (hdr[2] > max_hdr)
            {
                max_hdr = hdr[2];
            }
            float scale = 255.0f / (scale_c + max_hdr);
            for (int i = 0; i < 3; i++)
            {
                //float ref_hdr = 0.5f * ( hdr[i] + max_hdr );
                //float scale = 255.0f / ( scale_c + ref_hdr );
                rgb[i] = (int)(scale * hdr[i]);
            }
            // Uncharted 2 Hable
            /*final float exposure_bias = 2.0f / 255.0f;
			final float white_scale = 255.0f / Uncharted2Tonemap(W);
			for(int i=0;i<3;i++) {
				float curr = Uncharted2Tonemap(exposure_bias * hdr[i]);
				rgb[i] = (int)(curr * white_scale);
			}*/
        }

        private class HDRWriterThread : Java.Lang.Thread
        {
            private readonly HDRProcessor outerInstance;

            internal int y_start = 0, y_stop = 0;
            internal IList<Bitmap> bitmaps;
            internal ResponseFunction[] response_functions;
            //float avg_luminance = 0.0f;

            internal int n_bitmaps = 0;
            internal Bitmap bm = null;
            internal int[][] buffers = null;

            internal HDRWriterThread(HDRProcessor outerInstance, int y_start, int y_stop, IList<Bitmap> bitmaps, ResponseFunction[] response_functions)
            {
                //, float avg_luminance
                this.outerInstance = outerInstance;

                this.y_start = y_start;
                this.y_stop = y_stop;
                this.bitmaps = bitmaps;
                this.response_functions = response_functions;
                //this.avg_luminance = avg_luminance;

                this.n_bitmaps = bitmaps.Count;
                this.bm = bitmaps[0];
                this.buffers = new int[n_bitmaps][];
                for (int i = 0; i < n_bitmaps; i++)
                {
                    buffers[i] = new int[bm.Width];
                }
            }

            public virtual void run()
            {
                float[] hdr = new float[3];
                int[] rgb = new int[3];

                for (int y = y_start; y < y_stop; y++)
                {

                    // read out this row for each bitmap
                    for (int i = 0; i < n_bitmaps; i++)
                    {
                        bitmaps[i].GetPixels(buffers[i], 0, bm.Width, 0, y, bm.Width, 1);
                    }
                    for (int x = 0; x < bm.Width; x++)
                    {
                        //int this_col = buffer[c];
                        outerInstance.calculateHDR(hdr, n_bitmaps, buffers, x, response_functions);
                        outerInstance.tonemap(rgb, hdr);
                        //, avg_luminance
                        int new_col = (rgb[0] << 16) | (rgb[1] << 8) | rgb[2];
                        buffers[0][x] = new_col;
                    }
                    bm.SetPixels(buffers[0], 0, bm.Width, 0, y, bm.Width, 1);
                }
            }
        }

        /// <summary>
        /// Core implementation of HDR algorithm.
        ///  Requires Android 4.4 (API level 19, Kitkat), due to using Renderscript without the support libraries.
        ///  And we now need Android 5.0 (API level 21, Lollipop) for forEach_Dot with LaunchOptions.
        ///  Using the support libraries (set via project.properties renderscript.support.mode) would bloat the APK
        ///  by around 1799KB! We don't care about pre-Android 4.4 (HDR requires CameraController2 which requires
        ///  Android 5.0 anyway; even if we later added support for CameraController1, we can simply say HDR requires
        ///  Android 5.0).
        /// </summary>
        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @TargetApi(android.os.Build.VERSION_CODES.LOLLIPOP) private void processHDRCore(java.util.List<android.graphics.Bitmap> bitmaps)
        private void processHDRCore(IList<Bitmap> bitmaps)
        {

            long time_s = DateTimeHelperClass.CurrentUnixTimeMillis();

            int n_bitmaps = bitmaps.Count;
            Bitmap bm = bitmaps[0];
            const int base_bitmap = 1; // index of the bitmap with the base exposure
            ResponseFunction[] response_functions = new ResponseFunction[n_bitmaps]; // ResponseFunction for each image (the ResponseFunction entry can be left null to indicate the Identity)
                                                                                     /*int [][] buffers = new int[n_bitmaps][];
																					 for(int i=0;i<n_bitmaps;i++) {
																						 buffers[i] = new int[bm.getWidth()];
																					 }*/
                                                                                     //float [] hdr = new float[3];
                                                                                     //int [] rgb = new int[3];

            // compute response_functions
            for (int i = 0; i < n_bitmaps; i++)
            {
                ResponseFunction function = null;
                if (i != base_bitmap)
                {
                    function = createFunctionFromBitmaps(i, bitmaps[i], bitmaps[base_bitmap]);
                }
                response_functions[i] = function;
            }

            //final boolean use_renderscript = false;
            const bool use_renderscript = true;

            // write new hdr image
            if (use_renderscript)
            {

                if (rs == null)
                {
                    this.rs = RenderScript.Create(context);

                }
                // create allocations
                Allocation[] allocations = new Allocation[n_bitmaps];
                for (int i = 0; i < n_bitmaps; i++)
                {
                    allocations[i] = Allocation.CreateFromBitmap(rs, bitmaps[i]);
                }

                // create RenderScript
                //ScriptC_process_hdr processHDRScript = new ScriptC_process_hdr(rs);

                // set allocations
                //processHDRScript.set_bitmap1(allocations[1]);
                //processHDRScript.set_bitmap2(allocations[2]);

                // set response functions
                //processHDRScript.set_parameter_A0(response_functions[0].parameter_A);
                //processHDRScript.set_parameter_B0(response_functions[0].parameter_B);
                // no response function for middle image
                //processHDRScript.set_parameter_A2(response_functions[2].parameter_A);
                //processHDRScript.set_parameter_B2(response_functions[2].parameter_B);

                // set globals
                //final float tonemap_scale_c = avg_luminance / 0.8f; // lower values tend to result in too dark pictures; higher values risk over exposed bright areas
                const float tonemap_scale_c = 255.0f;
                // Higher tonemap_scale_c values means darker results from the Reinhard tonemapping.
                // Colours brighter than 255-tonemap_scale_c will be made darker, colours darker than 255-tonemap_scale_c will be made brighter
                // (tonemap_scale_c==255 means therefore that colours will only be made darker).

                //processHDRScript.set_tonemap_scale(tonemap_scale_c);


                //processHDRScript.forEach_hdr(allocations[0], allocations[0]);

                // bitmaps.get(0) now stores the HDR image, so free up the rest of the memory asap - we no longer need the remaining bitmaps
                for (int i = 1; i < bitmaps.Count; i++)
                {
                    Bitmap bitmap = bitmaps[i];
                    bitmap.Recycle();
                }

                adjustHistogram(allocations[0], bm.Width, bm.Height, time_s);

                allocations[0].CopyTo(bm);

            }
            //else
            //{

            //    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //    //ORIGINAL LINE: final int n_threads = Runtime.getRuntime().availableProcessors();
            //    int n_threads = Runtime.GetRuntime().AvailableProcessors();

            //    // create threads
            //    HDRWriterThread[] threads = new HDRWriterThread[n_threads];
            //    for (int i = 0; i < n_threads; i++)
            //    {
            //        int y_start = (i * bm.Height) / n_threads;
            //        int y_stop = ((i + 1) * bm.Height) / n_threads;
            //        threads[i] = new HDRWriterThread(this, y_start, y_stop, bitmaps, response_functions);
            //    }
            //    // start threads
            //    if (MyDebug.LOG)
            //    {
            //        Log.d(TAG, "start threads");
            //    }
            //    for (int i = 0; i < n_threads; i++)
            //    {
            //        threads[i].Start();
            //    }
            //    // wait for threads to complete
            //    if (MyDebug.LOG)
            //    {
            //        Log.d(TAG, "wait for threads to complete");
            //    }
            //    try
            //    {
            //        for (int i = 0; i < n_threads; i++)
            //        {
            //            threads[i].Join();
            //        }
            //    }
            //    catch (InterruptedException e)
            //    {
            //        if (MyDebug.LOG)
            //        {
            //            Log.e(TAG, "exception waiting for threads to complete");
            //        }
            //        Console.WriteLine(e.ToString());
            //        Console.Write(e.StackTrace);
            //    }
            //    // bitmaps.get(0) now stores the HDR image, so free up the rest of the memory asap:
            //    for (int i = 1; i < bitmaps.Count; i++)
            //    {
            //        Bitmap bitmap = bitmaps[i];
            //        bitmap.recycle();
            //    }
            //}

            //if (MyDebug.LOG)
            //{
            //    Log.d(TAG, "time for processHDRCore: " + (DateTimeHelperClass.CurrentUnixTimeMillis() - time_s));
            //}
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @TargetApi(android.os.Build.VERSION_CODES.LOLLIPOP) private void adjustHistogram(android.renderscript.Allocation allocation, int width, int height, long time_s)
        private void adjustHistogram(Allocation allocation, int width, int height, long time_s)
        {
            //const bool adjust_histogram = false;
            //final boolean adjust_histogram = true;

            //if (adjust_histogram)
            //{
            //    // create histogram
            //    int[] histogram = new int[256];
            //    Allocation histogramAllocation = Allocation.CreateSized(rs, Element.I32(rs), 256);
            //    //final boolean use_custom_histogram = false;
            //    const bool use_custom_histogram = true;
            //    if (MyDebug.LOG)
            //    {
            //        Log.d(TAG, "time before creating histogram: " + (DateTimeHelperClass.CurrentUnixTimeMillis() - time_s));
            //    }
            //    if (use_custom_histogram)
            //    {
            //        if (MyDebug.LOG)
            //        {
            //            Log.d(TAG, "create histogramScript");
            //        }
            //        ScriptC_histogram_compute histogramScript = new ScriptC_histogram_compute(rs);
            //        if (MyDebug.LOG)
            //        {
            //            Log.d(TAG, "bind histogram allocation");
            //        }
            //        histogramScript.bind_histogram(histogramAllocation);
            //        if (MyDebug.LOG)
            //        {
            //            Log.d(TAG, "call histogramScript");
            //        }
            //        histogramScript.forEach_histogram_compute(allocation);
            //    }
            //    else
            //    {
            //        ScriptIntrinsicHistogram histogramScript = ScriptIntrinsicHistogram.create(rs, Element.U8_4(rs));
            //        histogramScript.Output = histogramAllocation;
            //        if (MyDebug.LOG)
            //        {
            //            Log.d(TAG, "call histogramScript");
            //        }
            //        histogramScript.forEach_Dot(allocation); // use forEach_dot(); using forEach would simply compute a histogram for red values!
            //    }
            //    if (MyDebug.LOG)
            //    {
            //        Log.d(TAG, "time after creating histogram: " + (DateTimeHelperClass.CurrentUnixTimeMillis() - time_s));
            //    }

            //    //histogramAllocation.setAutoPadding(true);
            //    histogramAllocation.copyTo(histogram);

            //    int[] c_histogram = new int[256];
            //    c_histogram[0] = histogram[0];
            //    for (int x = 1; x < 256; x++)
            //    {
            //        c_histogram[x] = c_histogram[x - 1] + histogram[x];
            //    }
            //    /*if( MyDebug.LOG ) {
            //        for(int x=0;x<256;x++) {
            //            Log.d(TAG, "histogram[" + x + "] = " + histogram[x] + " cumulative: " + c_histogram[x]);
            //        }
            //    }*/
            //    histogramAllocation.copyFrom(c_histogram);

            //    ScriptC_histogram_adjust histogramAdjustScript = new ScriptC_histogram_adjust(rs);
            //    histogramAdjustScript.set_c_histogram(histogramAllocation);

            //    if (MyDebug.LOG)
            //    {
            //        Log.d(TAG, "call histogramAdjustScript");
            //    }
            //    histogramAdjustScript.forEach_histogram_adjust(allocation, allocation);
            //    if (MyDebug.LOG)
            //    {
            //        Log.d(TAG, "time after histogramAdjustScript: " + (DateTimeHelperClass.CurrentUnixTimeMillis() - time_s));
            //    }
            //}

            //final boolean adjust_histogram_local = false;
            const bool adjust_histogram_local = true;

            if (adjust_histogram_local)
            {
                // Contrast Limited Adaptive Histogram Equalisation
                // Note we don't fully equalise the histogram, rather the resultant image is the mid-point of the non-equalised and fully-equalised images
                // See https://en.wikipedia.org/wiki/Adaptive_histogram_equalization#Contrast_Limited_AHE
                // Also see "Adaptive Histogram Equalization and its Variations" ( http://www.cs.unc.edu/Research/MIDAG/pubs/papers/Adaptive%20Histogram%20Equalization%20and%20Its%20Variations.pdf ),
                // Pizer, Amburn, Austin, Cromartie, Geselowitz, Greer, ter Haar Romeny, Zimmerman, Zuiderveld (1987).

                // create histograms
                Allocation histogramAllocation = Allocation.CreateSized(rs, Element.I32(rs), 256);

                //ScriptC_histogram_compute histogramScript = new ScriptC_histogram_compute(rs);

                //histogramScript.bind_histogram(histogramAllocation);

                //final int n_tiles_c = 8;
                const int n_tiles_c = 4;
                //final int n_tiles_c = 1;
                int[] c_histogram = new int[n_tiles_c * n_tiles_c * 256];
                for (int i = 0; i < n_tiles_c; i++)
                {
                    double a0 = ((double)i) / (double)n_tiles_c;
                    double a1 = ((double)i + 1.0) / (double)n_tiles_c;
                    int start_x = (int)(a0 * width);
                    int stop_x = (int)(a1 * width);
                    if (stop_x == start_x)
                    {
                        continue;
                    }
                    for (int j = 0; j < n_tiles_c; j++)
                    {
                        double b0 = ((double)j) / (double)n_tiles_c;
                        double b1 = ((double)j + 1.0) / (double)n_tiles_c;
                        int start_y = (int)(b0 * height);
                        int stop_y = (int)(b1 * height);
                        if (stop_y == start_y)
                        {
                            continue;
                        }
                        /*if( MyDebug.LOG )
							Log.d(TAG, i + " , " + j + " : " + start_x + " , " + start_y + " to " + stop_x + " , " + stop_y);*/
                        Script.LaunchOptions launch_options = new Script.LaunchOptions();
                        launch_options.SetX(start_x, stop_x);
                        launch_options.SetY(start_y, stop_y);

                        /*if( MyDebug.LOG )
							Log.d(TAG, "call histogramScript");*/
                        //histogramScript.invoke_init_histogram();
                        //histogramScript.forEach_histogram_compute(allocation, launch_options);

                        int[] histogram = new int[256];
                        histogramAllocation.CopyTo(histogram);


                        // clip histogram, for Contrast Limited AHE algorithm
                        int n_pixels = (stop_x - start_x) * (stop_y - start_y);
                        int clip_limit = (5 * n_pixels) / 256;
                        /*if( MyDebug.LOG )
							Log.d(TAG, "clip_limit: " + clip_limit);*/
                        {
                            // find real clip limit
                            int bottom = 0, top = clip_limit;
                            while (top - bottom > 1)
                            {
                                int middle = (top + bottom) / 2;
                                int sum = 0;
                                for (int x = 0; x < 256; x++)
                                {
                                    if (histogram[x] > middle)
                                    {
                                        sum += (histogram[x] - clip_limit);
                                    }
                                }
                                if (sum > (clip_limit - middle) * 256)
                                {
                                    top = middle;
                                }
                                else
                                {
                                    bottom = middle;
                                }
                            }
                            clip_limit = (top + bottom) / 2;
                            /*if( MyDebug.LOG )
								Log.d(TAG, "updated clip_limit: " + clip_limit);*/
                        }
                        int n_clipped = 0;
                        for (int x = 0; x < 256; x++)
                        {
                            if (histogram[x] > clip_limit)
                            {
                                n_clipped += (histogram[x] - clip_limit);
                                histogram[x] = clip_limit;
                            }
                        }
                        int n_clipped_per_bucket = n_clipped / 256;
                        /*if( MyDebug.LOG ) {
							Log.d(TAG, "n_clipped: " + n_clipped);
							Log.d(TAG, "n_clipped_per_bucket: " + n_clipped_per_bucket);
						}*/
                        for (int x = 0; x < 256; x++)
                        {
                            histogram[x] += n_clipped_per_bucket;
                        }

                        int histogram_offset = 256 * (i * n_tiles_c + j);
                        c_histogram[histogram_offset] = histogram[0];
                        for (int x = 1; x < 256; x++)
                        {
                            c_histogram[histogram_offset + x] = c_histogram[histogram_offset + x - 1] + histogram[x];
                        }
                        /*if( MyDebug.LOG ) {
							for(int x=0;x<256;x++) {
								Log.d(TAG, "histogram[" + x + "] = " + histogram[x] + " cumulative: " + c_histogram[histogram_offset+x]);
							}
						}*/
                    }
                }

                Allocation c_histogramAllocation = Allocation.CreateSized(rs, Element.I32(rs), n_tiles_c * n_tiles_c * 256);
                c_histogramAllocation.CopyFrom(c_histogram);
                //ScriptC_histogram_adjust histogramAdjustScript = new ScriptC_histogram_adjust(rs);
                //histogramAdjustScript.set_c_histogram(c_histogramAllocation);
                //histogramAdjustScript.set_n_tiles(n_tiles_c);
                //histogramAdjustScript.set_width(width);
                //histogramAdjustScript.set_height(height);

            }
        }

        private static readonly float weight_scale_c = (float)((1.0 - 1.0 / 127.5) / 127.5);

        // If this algorithm is changed, also update the Renderscript version in process_hdr.rs
        private void calculateHDR(float[] hdr, int n_bitmaps, int[][] buffers, int x, ResponseFunction[] response_functions)
        {
            float hdr_r = 0.0f, hdr_g = 0.0f, hdr_b = 0.0f;
            float sum_weight = 0.0f;
            for (int i = 0; i < n_bitmaps; i++)
            {
                int color = buffers[i][x];
                float r = (float)((color & 0xFF0000) >> 16);
                float g = (float)((color & 0xFF00) >> 8);
                float b = (float)(color & 0xFF);
                float avg = (r + g + b) / 3.0f;
                // weight_scale_c chosen so that 0 and 255 map to a non-zero weight of 1.0/127.5
                float weight = 1.0f - weight_scale_c * Java.Lang.Math.Abs(127.5f - avg);
                //double weight = 1.0;
                /*if( MyDebug.LOG && x == 1547 && y == 1547 )
					Log.d(TAG, "" + x + "," + y + ":" + i + ":" + r + "," + g + "," + b + " weight: " + weight);*/
                if (response_functions[i] != null)
                {
                    // faster to access the parameters directly
                    /*float parameter = response_functions[i].parameter;
					r *= parameter;
					g *= parameter;
					b *= parameter;*/
                    float parameter_A = response_functions[i].parameter_A;
                    float parameter_B = response_functions[i].parameter_B;
                    r = parameter_A * r + parameter_B;
                    g = parameter_A * g + parameter_B;
                    b = parameter_A * b + parameter_B;
                }
                hdr_r += weight * r;
                hdr_g += weight * g;
                hdr_b += weight * b;
                sum_weight += weight;
            }
            hdr_r /= sum_weight;
            hdr_g /= sum_weight;
            hdr_b /= sum_weight;
            hdr[0] = hdr_r;
            hdr[1] = hdr_g;
            hdr[2] = hdr_b;
        }

        /* Initial test implementation - for now just doing an average, rather than HDR.
		 */
        private void processHDRAverage(IList<Bitmap> bitmaps)
        {

            long time_s = DateTimeHelperClass.CurrentUnixTimeMillis();

            Bitmap bm = bitmaps[0];
            int n_bitmaps = bitmaps.Count;
            int[] total_r = new int[bm.Width * bm.Height];
            int[] total_g = new int[bm.Width * bm.Height];
            int[] total_b = new int[bm.Width * bm.Height];
            for (int i = 0; i < bm.Width * bm.Height; i++)
            {
                total_r[i] = 0;
                total_g[i] = 0;
                total_b[i] = 0;
            }
            //int [] buffer = new int[bm.getWidth()*bm.getHeight()];
            int[] buffer = new int[bm.Width];
            for (int i = 0; i < n_bitmaps; i++)
            {
                //bitmaps.get(i).getPixels(buffer, 0, bm.getWidth(), 0, 0, bm.getWidth(), bm.getHeight());
                for (int y = 0, c = 0; y < bm.Height; y++)
                {

                    bitmaps[i].GetPixels(buffer, 0, bm.Width, 0, y, bm.Width, 1);
                    for (int x = 0; x < bm.Width; x++, c++)
                    {
                        //int this_col = buffer[c];
                        int this_col = buffer[x];
                        total_r[c] += this_col & 0xFF0000;
                        total_g[c] += this_col & 0xFF00;
                        total_b[c] += this_col & 0xFF;
                    }
                }
            }

            // write:
            for (int y = 0, c = 0; y < bm.Height; y++)
            {

                for (int x = 0; x < bm.Width; x++, c++)
                {
                    total_r[c] /= n_bitmaps;
                    total_g[c] /= n_bitmaps;
                    total_b[c] /= n_bitmaps;
                    //int col = Color.rgb(total_r[c] >> 16, total_g[c] >> 8, total_b[c]);
                    int col = (total_r[c] & 0xFF0000) | (total_g[c] & 0xFF00) | total_b[c];
                    buffer[x] = col;
                }
                bm.SetPixels(buffer, 0, bm.Width, 0, y, bm.Width, 1);
            }

            // bitmaps.get(0) now stores the HDR image, so free up the rest of the memory asap:
            for (int i = 1; i < bitmaps.Count; i++)
            {
                Bitmap bitmap = bitmaps[i];
                bitmap.Recycle();
            }
        }
    }
}