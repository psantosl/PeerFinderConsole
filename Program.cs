using System;
using System.Runtime.InteropServices;
using Windows.Networking.Proximity;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Threading;

namespace App
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            SetCurrentProcessExplicitAppUserModelID("PeerFinderApp");

            Program p = new Program();

            if (args[0] == "host")
            {
                p.RunHost();
                return;
            }

            p.RunClient();
        }

        void RunClient()
        {
            PeerFinder.Role = PeerRole.Client;
            PeerFinder.AllowWiFiDirect = true;

            if ((Windows.Networking.Proximity.PeerFinder.SupportedDiscoveryTypes &
                 Windows.Networking.Proximity.PeerDiscoveryTypes.Browse) !=
                 Windows.Networking.Proximity.PeerDiscoveryTypes.Browse)
            {
                Console.WriteLine("Peer discovery using Wi-Fi Direct is not supported.\n");
                return;
            }

            PeerFinder.Start();

            var peerInfoCollection = PeerFinder.FindAllPeersAsync().AsTask().Result;

            if (peerInfoCollection.Count == 0)
            {
                Console.WriteLine("No peers found");
                return;
            }

            foreach( var info in peerInfoCollection)
            {
                Console.WriteLine("Peer found: {0}", info.DisplayName);

                Windows.Networking.Sockets.StreamSocket socket =
                    PeerFinder.ConnectAsync(info).AsTask().Result;

                Sender(socket);
            }
        }

        void RunHost()
        {
            if ((Windows.Networking.Proximity.PeerFinder.SupportedDiscoveryTypes &
                 Windows.Networking.Proximity.PeerDiscoveryTypes.Browse) !=
                 Windows.Networking.Proximity.PeerDiscoveryTypes.Browse)
            {
                Console.WriteLine("Peer discovery using Wi-Fi Direct is not supported.\n");
            }

            PeerFinder.DisplayName = "Modok-the-app";
            PeerFinder.AllowWiFiDirect = true;

            PeerFinder.ConnectionRequested += ConnectionRequested;

            PeerFinder.TriggeredConnectionStateChanged += TriggeredConnectionStateChanged;

            PeerFinder.Role = PeerRole.Host;

            PeerFinder.Start();

            Console.ReadLine();
        }

        void ConnectionRequested(
            object sender,
            Windows.Networking.Proximity.ConnectionRequestedEventArgs e)
        {
            Console.WriteLine(
                "Connection requested by " + e.PeerInformation.DisplayName + ". " +
                "Will be accepted automatically.");

            try
            {
                Receiver(e.PeerInformation);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in receiver: {0}", ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        static void Receiver(PeerInformation peerInfo)
        {
            Console.WriteLine("Receiver waiting to connect async");

            Windows.Networking.Sockets.StreamSocket socket =
                PeerFinder.ConnectAsync(peerInfo).AsTask().Result;

            Console.WriteLine("Receiver connection happened");

            Windows.Storage.Streams.DataReader r = new Windows.Storage.Streams.DataReader(socket.InputStream);

            Console.WriteLine("Datareader created");

            int ini = Environment.TickCount;

            Console.WriteLine("Loaded async {0} bytes", r.LoadAsync(8).AsTask().Result);

            long totalToRead = r.ReadInt64();

            Console.WriteLine("Going to read {0} bytes", totalToRead);

            long read = 0;

            byte[] buffer = new byte[512*1024];

            while (read < totalToRead)
            {
                long toRead = totalToRead - read;

                if (toRead >= buffer.Length)
                {
                    toRead = buffer.Length;
                }
                else
                {
                    buffer = new byte[toRead];
                }

                Console.WriteLine("Loaded async {0} bytes", r.LoadAsync((uint)toRead).AsTask().Result);

                r.ReadBytes(buffer);

                read += toRead;
            }

            Console.WriteLine("Read {0} bytes in {1} sec",
                totalToRead, (Environment.TickCount - ini) / 1000);
        }

        void TriggeredConnectionStateChanged(
            object sender,
            Windows.Networking.Proximity.TriggeredConnectionStateChangedEventArgs e)
        {
            Console.WriteLine("Peer found! " + e.Id);

            if (e.State == Windows.Networking.Proximity.TriggeredConnectState.PeerFound)
            {
                Console.WriteLine("Peer found. You may now pull your devices out of proximity.\n");
            }
            if (e.State == Windows.Networking.Proximity.TriggeredConnectState.Completed)
            {
                Console.WriteLine("Connected. You may now send a message.\n");
                Sender(e.Socket);
            }
        }

        void Sender(Windows.Networking.Sockets.StreamSocket socket)
        {
            Windows.Storage.Streams.DataWriter w = new Windows.Storage.Streams.DataWriter(socket.OutputStream);

            int ini = Environment.TickCount;

            long totalToSend = 64 * 1024 * 1024;

            w.WriteInt64(totalToSend);

            Console.WriteLine("Going to send {0} bytes", totalToSend);

            long sent = 0;

            byte[] buffer = new byte[512 * 1024];

            while (sent < totalToSend)
            {
                long toSend = totalToSend - sent;

                if (toSend >= buffer.Length)
                {
                    toSend = buffer.Length;
                }
                else
                {
                    buffer = new byte[toSend];
                }

                w.WriteBytes(buffer);

                sent += toSend;
            }

            Console.WriteLine("stored async: {0} bytes", w.StoreAsync().AsTask().Result);

            if (w.FlushAsync().AsTask().Result)
                Console.WriteLine("Sent correctly");

            Console.WriteLine("Written {0} bytes in {1} sec",
                totalToSend, (Environment.TickCount - ini) / 1000);

        }

        //https://social.msdn.microsoft.com/Forums/en-US/ce649545-9ec6-45da-a4ee-71b9f2bed156/using-metro-win8-sdk-to-build-desktop-style-application?forum=winappswithnativecode

        [DllImport("shell32.dll")]
        public static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

    }
}
