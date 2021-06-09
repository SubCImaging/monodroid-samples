// <copyright file="TemperatureLogger.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid.Helpers
{
    using SubCTools.Droid.Interfaces;
    using System;
    using System.Text;
    using System.Timers;

    public class TemperatureLogger
    {
        private const int BufferSize = 75;

        private readonly TimeSpan measurementInterval = TimeSpan.FromSeconds(5);

        private const string fileLocation = "/storage/emulated/0/Logs/temperature_ring.log";

        private readonly IShell shell;

        private readonly Timer timer = new Timer();

        private int ringIndex = 0;

        private bool ringFull;

        private const int maxThermalZone = 37;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemperatureLogger"/> class.
        /// </summary>
        /// <param name="shell"></param>
        public TemperatureLogger(IShell shell)
        {
            this.shell = shell;

            updateRingIndex();

            ringFull = CountFileLines(fileLocation).Equals(BufferSize);

            timer.Interval = measurementInterval.TotalMilliseconds;
            timer.Elapsed += (s, e) => LogTemperature();
            timer.Start();
        }

        public void LogTemperature()
        {
            var builder = new StringBuilder();
            builder.Append(DateTime.Now.ToString().Replace('/', '-'));

            for (var i = 0; i < maxThermalZone; i++)
            {
                var temp = shell.ShellSync($"cat /sys/class/thermal/thermal_zone{i}/temp").TrimEnd();
                builder.Append($",{temp}");
            }

            WriteToRingBuffer(builder.ToString());
        }

        private void WriteToRingBuffer(string line)
        {
            if (!ringFull)
            {
                shell.ShellSync($@"echo ""{line}"" >> {fileLocation}");

                if (CountFileLines(fileLocation) == BufferSize)
                {
                    ringIndex = 0;
                    ringFull = true;
                }
            }
            else
            {
                updateRingIndex();
                var command = $@"sed -i '{ringIndex + 1}s/.*/{line}/' {fileLocation}";
                shell.ShellSync(command);
                if (ringIndex.Equals(BufferSize - 1))
                {
                    ringIndex = 0;
                }
                else
                {
                    ringIndex++;
                }

                shell.ShellSync($"setprop persist.rayfin.temprb.index {ringIndex}");
            }
        }

        private void updateRingIndex()
        {
            var index = shell.ShellSync("getprop persist.rayfin.temprb.index").TrimEnd();

            if (int.TryParse(index, out var ringIndex))
            {
                this.ringIndex = ringIndex;
            }
        }

        private int CountFileLines(string file)
        {
            var lines = shell.ShellSync($"wc -l < {file}");

            if (int.TryParse(lines, out var numberOfLines))
            {
                return numberOfLines;
            }
            else
            {
                // File must not exist?
                return 0;
            }
        }
    }
}
