namespace SubCTools.Droid.Managers
{
    using Android.Net.Nsd;
    using SubCTools.Communicators;
    using SubCTools.Droid.Communicators;
    using SubCTools.Droid.Helpers;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// Class for handling all ethernet based communications
    /// </summary>
    public class EthernetManager
    {
        private readonly CameraInfo cameraInfo;
        private readonly FeatureToggler featureToggler;
        private readonly SubCServiceDiscovery serviceDiscovery;
        private UdpClient client;

        private IPEndPoint endPoint;

        public EthernetManager(
                    NsdManager nsdManager,
            ISettingsService settings,
            CameraInfo cameraInfo,
            FeatureToggler featureToggler)
        {
            this.featureToggler = featureToggler;
            serviceDiscovery = new SubCServiceDiscovery(nsdManager, cameraInfo);
            serviceDiscovery.Register();

            // this will route any data to and from the udp connection
            var udpRouter = new EthernetRouter(new SubCUDP(cameraInfo.UDPAddress.Port));

            // this will route any data to and from the tcp connection
            var tcpRouter = new EthernetRouter(new SubCTCPServer(cameraInfo.TCPAddress.Port, 1));
            tcpRouter.IsConnectedChanged += Router_IsConnectedChanged;

            MessageRouter.Instance.Add(tcpRouter);
            MessageRouter.Instance.Add(udpRouter);

            MessageIOC.Instance.Add(MessageTypes.Information | MessageTypes.Error | MessageTypes.Alert, udpRouter);
            MessageIOC.Instance.Add(MessageTypes.Information | MessageTypes.Error | MessageTypes.Alert, tcpRouter);

            if (DroidSystem.ShellSync("getprop rayfin.rom.version") == string.Empty)
            {
                CheckStaticIP();
            }

            //var listeningPort = 3702;
            //var endPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 3702);
            client = new UdpClient(3702)
            {
                //EnableBroadcast = true,
                //MulticastLoopback = true
            };

            client.JoinMulticastGroup(IPAddress.Parse("239.255.255.250"));

            client.BeginReceive(OnReceive, null);

            this.cameraInfo = cameraInfo;
        }

        public UDPServicer UDPServicer { get; }

        public void UpdateCameraInfo(CameraInfo info)
        {
            serviceDiscovery.UpdateCameraInfo(info);
        }

        private void CheckStaticIP()
        {
            if (File.Exists("/data/local/tmp/eth0conf"))
            {
                SubCLogger.Instance.Write($"Setting static IP", "debug.txt", DroidSystem.LogDirectory);
                var staticIP = new IPAddress(0);
                if (IPAddress.TryParse(NetworkTools.ParseIP(DroidSystem.ShellSync("cat /data/local/tmp/eth0conf")), out staticIP))
                {
                    NetworkTools.AssignStaticIp(staticIP);
                }
            }
        }

        private string MakeResponse(string relatesTo)
        {
            var ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .Where(i => i != new IPAddress(new byte[] { 127, 0, 0, 1 }))
                .FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "IP Error";

            string response =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                                      + "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:SOAP-ENC=\"http://www.w3.org/2003/05/soap-encoding\" xmlns:wsa=\"http://schemas.xmlsoap.org/ws/2004/08/addressing\" xmlns:d=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\" xmlns:dn=\"http://www.onvif.org/ver10/network/wsdl\">"
                                      + "<SOAP-ENV:Header>"
                                      + "<wsa:MessageID>uuid:5fe46ad2-33e0-11eb-ad2e-00408cde7b76</wsa:MessageID>"
                                      + "<wsa:RelatesTo>" + relatesTo + "</wsa:RelatesTo>"
                                      + "<wsa:To SOAP-ENV:mustUnderstand=\"true\">http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous</wsa:To>"
                                      + "<wsa:Action SOAP-ENV:mustUnderstand=\"true\">http://schemas.xmlsoap.org/ws/2005/04/discovery/ProbeMatches</wsa:Action>"
                                      + "<d:AppSequence SOAP-ENV:mustUnderstand=\"true\" MessageNumber=\"124710\" InstanceId=\"1604601784\">"
                                      + "</d:AppSequence>"
                                      + "</SOAP-ENV:Header>"
                                      + "<SOAP-ENV:Body>"
                                      + "<d:ProbeMatches>"
                                      + "<d:ProbeMatch>"
                                      + "<wsa:EndpointReference>"
                                      + "<wsa:Address>urn:uuid:2735be50-4713-11e8-a320-00408cde7b76</wsa:Address>"
                                      + "</wsa:EndpointReference>"
                                      + "<d:Types>dn:NetworkVideoTransmitter</d:Types>"
                                      + "<d:Scopes>onvif://www.onvif.org/type/video_encoder onvif://www.onvif.org/Profile/Streaming onvif://www.onvif.org/type/audio_encoder onvif://www.onvif.org/hardware/Rayfin onvif://www.onvif.org/name/SubC%20Imaging onvif://www.onvif.org/location/ </d:Scopes>"
                                      + "<d:XAddrs>http://" + ip + ":8000/onvif/device_service</d:XAddrs>"
                                      + "<d:MetadataVersion>1</d:MetadataVersion>"
                                      + "</d:ProbeMatch>"
                                      + "</d:ProbeMatches>"
                                      + "</SOAP-ENV:Body>"
                                     + "</SOAP-ENV:Envelope>";
            return response;
        }

        private void OnReceive(IAsyncResult ar)
        {
            string message;

            message = Encoding.ASCII.GetString(client.EndReceive(ar, ref endPoint));
            message = message.TrimEnd('\0');

            // Console.WriteLine("Information: " + message);

            if (message.Contains("NetworkVideoTransmitter") && featureToggler.IsFeatureOn("Onvif"))
            {
                //var xml = new XmlDocument();
                //xml.LoadXml(message);

                var idMatch = Regex.Match(message, @"MessageID>(.*)</.*:MessageID>");

                if (idMatch.Success)
                {
                    var rx = MakeResponse(idMatch.Groups[1].Value);
                    client.Send(Encoding.ASCII.GetBytes(rx), rx.Length, endPoint);
                }

                //
            }

            client.BeginReceive(OnReceive, null);
        }

        private void Router_IsConnectedChanged(object sender, bool connected)
        {
#if !DEBUG
            if (!connected)
            {
                Android.Util.Log.Debug("Rayfin.ConnectionChanged", $"RAYFIN IS CONNECTED: {connected}");
                Task.Delay(50);
                DroidSystem.ShellSync("setprop ctl.restart adbd");
            }
#endif
        }
    }
}