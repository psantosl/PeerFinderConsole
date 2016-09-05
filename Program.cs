using System;
using System.Runtime.InteropServices;
using Windows.Networking.Proximity;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

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
            using (Stream streamWrite = socket.OutputStream.AsStreamForWrite())
            using (BinaryWriter writer = new BinaryWriter(streamWrite))
            using (Stream streamRead = socket.InputStream.AsStreamForRead())
            using (BinaryReader reader = new BinaryReader(streamRead))
            {
                int ini = Environment.TickCount;

                long totalToSend = 64 * 1024 * 1024;

                writer.Write(totalToSend);

                Console.WriteLine("Going to send {0} bytes", totalToSend);

                long sent = 0;

                byte[] buffer = new byte[4 * 1024 * 1024];

                while (sent < totalToSend)
                {
                    long toSend = totalToSend - sent;

                    if (toSend >= buffer.Length)
                    {
                        toSend = buffer.Length;
                    }

                    writer.Write(buffer, 0, (int)toSend);

                    Console.WriteLine("{0}/{1}", sent, totalToSend);

                    writer.Flush();

                    reader.ReadBoolean();

                    sent += toSend;
                }

                Console.WriteLine("Written {0} bytes in {1} sec",
                    totalToSend, (Environment.TickCount - ini) / 1000);
            }
        }

        static void Receiver(PeerInformation peerInfo)
        {
            Console.WriteLine("Receiver waiting to connect async");

            Windows.Networking.Sockets.StreamSocket socket =
                PeerFinder.ConnectAsync(peerInfo).AsTask().Result;

            Console.WriteLine("Receiver connection happened");

            int ini = Environment.TickCount;

            using (Stream streamRead = socket.InputStream.AsStreamForRead())
            using (Stream streamWrite = socket.OutputStream.AsStreamForWrite())
            using (BinaryReader reader = new BinaryReader(streamRead))
            using (BinaryWriter writer = new BinaryWriter(streamWrite))
            {
                long totalToRead = reader.ReadInt64();

                Console.WriteLine("Going to read {0} bytes", totalToRead);

                long read = 0;

                byte[] buffer = new byte[4* 1024 * 1024];

                while (read < totalToRead)
                {
                    long toRead = totalToRead - read;

                    if (toRead >= buffer.Length)
                    {
                        toRead = buffer.Length;
                    }

                    reader.Read(buffer, 0, (int)toRead);

                    Console.WriteLine("{0}/{1}", read, totalToRead);

                    read += toRead;

                    writer.Write(true);

                    writer.Flush();
                }

                Console.WriteLine("Read {0} bytes in {1} sec",
                    totalToRead, (Environment.TickCount - ini) / 1000);
            }
        }

        //https://social.msdn.microsoft.com/Forums/en-US/ce649545-9ec6-45da-a4ee-71b9f2bed156/using-metro-win8-sdk-to-build-desktop-style-application?forum=winappswithnativecode

        [DllImport("shell32.dll")]
        public static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

    }
}
