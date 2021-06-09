using SubCTools.EventArgsLib;
using System;
using System.Windows.Threading;

namespace SubCTools
{
    public class RecordingTimer
    {
        readonly DispatcherTimer recTimer = new DispatcherTimer();

        TimeSpan recordingDuration;

        DateTime startTime;
        DateTime? pauseTime;

        TimeSpan pauseDuration;

        int recordingSeconds = 0,
            amountOfSplits = 0,
            splitTime = 10;
        
        public RecordingTimer()
        {
            recTimer.Interval = TimeSpan.FromMilliseconds(100);
            recTimer.Tick += RecTimer_Tick;
        }

        public event EventHandler<EventArgsT<int>> RecordingSecondsUpdated;
        public event EventHandler SplitTimerTick;

        /// <summary>
        /// Period to create new files
        /// </summary>
        public int SplitTime
        {
            get
            {
                return splitTime;
            }
            set
            {
                splitTime = value;
            }
        }

        /// <summary>
        /// How many times the file has been split so far
        /// </summary>
        public int AmountOfSplits
        {
            get
            {
                return amountOfSplits;
            }
        }

        /// <summary>
        /// Expected length of the currently recording video
        /// </summary>
        public double VideoLength =>  RecordingDuration.TotalSeconds - (SplitTime * 60 * AmountOfSplits);
            
        /// <summary>
        /// Total recording duration
        /// </summary>
        public TimeSpan RecordingDuration
        {
            get
            {
                return recordingDuration;
            }
            set
            {
                if (recordingDuration != value)
                {
                    recordingDuration = value;
                    RecordingSeconds = (int)value.TotalSeconds;
                }
            }
        }

        public int RecordingSeconds
        {
            get
            {
                return recordingSeconds;
            }
            set
            {
                if (recordingSeconds != value)
                {
                    recordingSeconds = value;
                    RecordingSecondsUpdated?.Invoke(this, new EventArgsT<int>(value));
                    
                    if (value == 0)
                        return;
                    
                    //only split when you're divisible by the split time
                    var remainder = value % (splitTime * 60);
                    if (remainder == 0)
                    {
                        Split();
                    }
                }
            }
        }

        /// <summary>
        /// Is the timer started
        /// </summary>
        public bool IsStarted
        {
            get
            {
                return recTimer.IsEnabled;
            }
        }

        /// <summary>
        /// Start timer
        /// </summary>
        public void Start()
        {
            if (pauseTime != null)
            {
                //append the pause duration to know the cumlitive pause time
                pauseDuration += DateTime.Now.Subtract(pauseTime.Value);
            }
            else
            {
                //reset the duration back to 0
                //RecordingDurationUpdated.SafeInvoke<int>(this, 0);
                RecordingDuration = TimeSpan.FromSeconds(0);
                startTime = DateTime.Now;
            }

            recTimer.Start();
        }

        /// <summary>
        /// Pause timer
        /// </summary>
        public void Pause()
        {
            recTimer.Stop();
            pauseTime = DateTime.Now;
        }

        /// <summary>
        /// Stop timer
        /// </summary>
        public void Stop()
        {
            Pause();
            Reset();
        }

        /// <summary>
        /// Reset all the varibles back to 0
        /// </summary>
        void Reset()
        {
            startTime = DateTime.Now;
            //timeInSeconds = 0;
            amountOfSplits = 0;
            pauseTime = null;
            pauseDuration = TimeSpan.FromSeconds(0);
            recordingDuration = TimeSpan.FromSeconds(0);
        }

        void RecTimer_Tick(object sender, EventArgs e)
        {
            //get the recording duration by subtracting now, from the start time.
            //If the file has been paused, subtract the cumlitive pause duration
            RecordingDuration = DateTime.Now.Subtract(startTime).Subtract(pauseDuration);
        }

        /// <summary>
        /// Time for a file splt
        /// </summary>
        void Split()
        {
            SplitTimerTick?.Invoke(this, EventArgs.Empty);
            amountOfSplits++;
        }
    }
}
