using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace SubCTools.Droid.Tools
{
    public static class NIST
    {
        //**************************************************************************
        //NAME:      GetNISTDateTime
        //PURPOSE:   Creates a TCP client to connect to the NIST server, gets the 
        //data from the server, and parses that data into a string that can be used 
        //for display or calculation.
        //**************************************************************************
        public static DateTime GetNISTDateTime(string server="time.nist.gov", int port = 13)
        {
            bool bGoodConnection = false;

            //Create and instance of a TCP client that will connect to the server 
            //and get the information it offers
            System.Net.Sockets.TcpClient tcpClientConnection = new System.Net.Sockets.TcpClient();

            //Attempt to connect to the NIST server. If it succeeds, the flag is set 
            //to collect the information from the server If it fails, try again

            try
            {
                tcpClientConnection.Connect(server, port);
                bGoodConnection = true;
            }
            catch
            {
                bGoodConnection = false;
            }


            //Don't continue if you haven't got a good connection
            if (bGoodConnection == true)
            {
                //Attempt to get the data streaming from the NIST server
                try
                {
                    NetworkStream netStream = tcpClientConnection.GetStream();

                    //Check the flag the states if you can read the stream or not
                    if (netStream.CanRead)
                    {
                        //Get the size of the buffer
                        byte[] bytes = new byte[tcpClientConnection.ReceiveBufferSize];

                        //Read in the stream to the length of the buffer
                        netStream.Read(bytes, 0, tcpClientConnection.ReceiveBufferSize);

                        //Read the Bytes as ASCII values and build the stream
                        // of charaters that are the date and time from NIST. 
                        var sNISTDateTimeFull = Encoding.ASCII.GetString(bytes).Substring(0, 50);

                        //Convert the string to a date time value
                        var subStringNISTDateTimeShort = sNISTDateTimeFull.Substring(7, 17);
                        return DateTime.Parse("20" + subStringNISTDateTimeShort);
                    }
                    else //If the data stream was unreadable, do the following
                    {

                        //Advise the user of the situation
                        tcpClientConnection.Close(); //close the client stream
                        netStream.Close(); //close the network stream
                        throw new Exception();
                    }

                    //Uses the Close public method to close the network stream and socket.
                    tcpClientConnection.Close();
                    throw new Exception();
                }
                catch
                {
                    throw new Exception();
                }
                throw new Exception();
            }
            else
            {
                throw new Exception();
            }
        }
    }
}