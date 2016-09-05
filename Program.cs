using System;
using System.Runtime.InteropServices;
using Windows.Networking.Proximity;
using System.IO;

namespace PeerFinderConsole
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            SetCurrentProcessExplicitAppUserModelID("PeerFinderApp");

            if (args[0] == "host")
            {
                HostRole host = new HostRole();
                host.Run();
                return;
            }

            ClientRole client = new ClientRole();
            client.Run();
        }

        class ClientRole
        {
            internal void Run()
            {
                PeerFinder.Role = PeerRole.Client;
                PeerFinder.AllowWiFiDirect = true;
                PeerFinder.TriggeredConnectionStateChanged += TriggeredConnectionStateChanged;

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

                foreach (var info in peerInfoCollection)
                {
                    Console.WriteLine("Peer found: {0}", info.DisplayName);

                    Windows.Networking.Sockets.StreamSocket socket =
                        PeerFinder.ConnectAsync(info).AsTask().Result;

                    Sender(socket);
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

                    byte[] buffer = new byte[totalToSend];

                    writer.Write(buffer);

                    writer.Flush();

                    reader.ReadBoolean();

                    float MB = totalToSend / 1024 / 1024;
                    float secs = (Environment.TickCount - ini) / 1000;

                    Console.WriteLine("Sent {0} MB in {1} secs = {2:#0.##} MB/s",
                        MB, secs, MB / secs);
                }
            }

            void TriggeredConnectionStateChanged(
                object sender,
                Windows.Networking.Proximity.TriggeredConnectionStateChangedEventArgs e)
            {
                // THIS METHOD IS NEVER INVOKED, I don't know why yet

                Console.WriteLine("Peer found! " + e.Id);

                if (e.State == Windows.Networking.Proximity.TriggeredConnectState.PeerFound)
                {
                    Console.WriteLine("Peer found. You may now pull your devices out of proximity.\n");
                }
                if (e.State == Windows.Networking.Proximity.TriggeredConnectState.Completed)
                {
                    Console.WriteLine("Connected. You may now send a message.\n");
                }
            }
        }

        class HostRole
        {
            internal void Run()
            {
                if ((Windows.Networking.Proximity.PeerFinder.SupportedDiscoveryTypes &
                     Windows.Networking.Proximity.PeerDiscoveryTypes.Browse) !=
                     Windows.Networking.Proximity.PeerDiscoveryTypes.Browse)
                {
                    Console.WriteLine("Peer discovery using Wi-Fi Direct is not supported.\n");
                }

                PeerFinder.Role = PeerRole.Host;

                PeerFinder.DisplayName = "PeerFinderConsoleApp";
                PeerFinder.AllowWiFiDirect = true;

                PeerFinder.ConnectionRequested += ConnectionRequested;

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

                int ini = Environment.TickCount;

                using (Stream streamRead = socket.InputStream.AsStreamForRead())
                using (Stream streamWrite = socket.OutputStream.AsStreamForWrite())
                using (BinaryReader reader = new BinaryReader(streamRead))
                using (BinaryWriter writer = new BinaryWriter(streamWrite))
                {
                    long totalToRead = reader.ReadInt64();

                    Console.WriteLine("Going to read {0} bytes", totalToRead);

                    long read = 0;

                    byte[] buffer = new byte[32 * 1024 * 1024];

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

                    float MB = totalToRead / 1024 / 1024;
                    float secs = (Environment.TickCount - ini) / 1000;

                    Console.WriteLine("Received {0} MB in {1} secs = {2:#0.##} MB/s",
                        MB, secs, MB / secs);
                }
            }
        }

        //https://social.msdn.microsoft.com/Forums/en-US/ce649545-9ec6-45da-a4ee-71b9f2bed156/using-metro-win8-sdk-to-build-desktop-style-application?forum=winappswithnativecode

        [DllImport("shell32.dll")]
        static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);
    }
}
