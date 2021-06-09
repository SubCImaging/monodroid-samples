namespace SubCTools.Droid.Handlers
{
    using System;
    using Android.OS;
    using Android.Util;
    using Veg.Mediacapture.Sdk;
    using static Veg.Mediacapture.Sdk.MediaCapture;

    public class MediaCaptureSdkHandler : Handler
    {
        private const string Tag = "MediaCaptureSdkHandler";
        private const int CapOpened = 700;

        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        private MediaCapture capturer;
        private bool isSurfaceCreated;
        private String rtmpUrl;
        private Func<bool> isRecording;
        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        public MediaCaptureSdkHandler(MediaCapture capturer, ref bool isSurfaceCreated, ref string rtmpUrl, Func<bool> isRecording)
        {
            this.capturer = capturer;
            this.isSurfaceCreated = isSurfaceCreated;
            this.rtmpUrl = rtmpUrl;
            this.isRecording = isRecording;
        }

        public void SetCapturer(MediaCapture capture)
        {
            this.capturer = capture;
        }

        public override void HandleMessage(Message msg)
        {
            CaptureNotifyCodes status = (CaptureNotifyCodes)msg.Obj;
            String strText = null;

            if (status.Equals(CaptureNotifyCodes.CapOpened))
            {
                strText = "Opened";
            }
            else if (status.Equals(CaptureNotifyCodes.CapSurfaceCreated))
            {
                strText = "Camera surface created surfaceView=" + capturer.SurfaceView;
                isSurfaceCreated = true;
            }
            else if (status.Equals(CaptureNotifyCodes.CapSurfaceDestroyed))
            {
                strText = "Camera surface destroyed";
                isSurfaceCreated = false;
            }
            else if (status.Equals(CaptureNotifyCodes.CapStarted))
            {
                strText = "Started";
            }
            else if (status.Equals(CaptureNotifyCodes.CapStopped))
            {
                strText = "Stopped";
            }
            else if (status.Equals(CaptureNotifyCodes.CapClosed))
            {
                strText = "Closed";
            }
            else if (status.Equals(CaptureNotifyCodes.CapError))
            {
                strText = "Error";
            }
            else if (status.Equals(CaptureNotifyCodes.CapTime))
            {
                if (isRecording())
                {
                    int rtmp_status = capturer.StreamStatus;
                    int dur = (int)(long)capturer.Duration / 1000;
                    int v_cnt = capturer.VideoPackets;
                    int a_cnt = capturer.AudioPackets;
                    long v_pts = capturer.LastVideoPTS;
                    long a_pts = capturer.LastAudioPTS;
                    int nreconnects = capturer.StatReconnectCount;
                    long actual_bitrate = capturer.GetPropLong(PlayerRecordStat.ForType(PlayerRecordStat.PpRecordStatActualBitrate));
                    long actual_framerate = capturer.GetPropLong(PlayerRecordStat.ForType(PlayerRecordStat.PpRecordStatActualFramerate));

                    String sss = "";
                    String sss2 = "";
                    int min = dur / 60;
                    int sec = dur - (min * 60);
                    sss = String.Format("%02d:%02d", min, sec);
                    sss += ". Audio OFF";
                    if (rtmp_status == (-999))
                    {
                        sss = "Streaming stopped. DEMO VERSION limitation";
                        capturer.Stop();
                    }
                    else
                    if (rtmp_status != (-1))
                    {

                        if (capturer.UseRtspServer)
                        {
                            sss += ". RTSP ON (" + capturer.RTSPAddr + ")";
                            sss2 += "v:" + v_cnt + " a:" + a_cnt + " rcc:" + nreconnects;
                        }
                        else
                        {
                            sss += ". RTMP " + ((rtmp_status == 0) ? "ON ( " + rtmpUrl + " )" : "Err:" + rtmp_status);
                            //sss += ". RTMP "+ ((rtmp_status == 0)?"ON ":"Err:"+rtmp_status);
                            if (rtmp_status == (-5))
                            {
                                sss += " Server not connected ( " + rtmpUrl + " )";
                            }
                            else if (rtmp_status == (-12))
                            {
                                sss += " Out of memory";
                            }
                            sss2 += "v:" + v_cnt + " a:" + a_cnt + " rcc:" + nreconnects + "  " + actual_bitrate + "kbps : " + actual_framerate + "fps";
                            sss2 += "\nv_pts: " + v_pts + " a_pts: " + a_pts + " delta: " + (v_pts - a_pts);
                        }

                    }
                    else
                    {
                        // rtmp_status == (-1)
                        sss += ". Connecting ...";
                    }

                    String sss3 = "";
                    int rec_status = capturer.RECStatus;
                    if (rec_status != -1)
                    {
                        if (rec_status == (-999))
                        {
                            sss = "Streaming stopped. DEMO VERSION limitation";
                            capturer.Stop();
                        }
                        else
                        if (rec_status != 0 && rec_status != (-999))
                        {
                            sss3 += "REC Err:" + rec_status;
                        }
                        else
                            sss3 += "REC ON. " + capturer.GetPropString(PlayerRecordStat.ForType(PlayerRecordStat.PpRecordStatFileName));
                    }
                }
            }

            if (strText != null)
            {
                Log.Info(Tag, "=Status handleMessage str=" + strText);
            }
        }
    }
}