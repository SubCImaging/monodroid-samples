using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Messaging.Models
{
    [Flags]
    public enum MessageTypes
    {
        Default = 1, //00000001
        Information = 1 << 1,//00000010 
        Critical = 1 << 2, //000000100
        Warning = 1 << 3,
        Debug = 1 << 4,
        Transmit = 1 << 5,
        Receive = 1 << 6,
        SubCCommand = 1 << 7,
        CameraState = 1 << 8,
        Help = 1 << 9,
        Connection = 1 << 10,
        Gauntlet = 1 << 11,
        Error = 1 << 12,
        RecordingTime = 1 << 13
    }
}
