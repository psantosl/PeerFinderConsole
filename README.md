# PeerFinderConsole
Shows how to use the [PeerFinder](https://msdn.microsoft.com/en-us/library/windows/apps/windows.networking.proximity.peerfinder) proximity API from a C# Console app.

PeerFinder allows you to create wifi-direct sockets very easily. With wifi-direct you can connect to laptops at the application level even if they're not on the same wifi network, it is a direct ad-hoc connection between the two. It is extremely handy to share data when you are outside a reliable network.

But, for some reason, all the samples are for UWP or Windows Store apps, which is a pain. And all the PeerFinder methods show the same code fragment as an example, which is all poluted with UI code.

## Important point to make PeerFinder work on Console
* You need to invoke this ```SetCurrentProcessExplicitAppUserModelID``` for the PeerFinder to work. This is key, otherwise NOTHING works. It is explained [here](https://social.msdn.microsoft.com/Forums/en-US/ce649545-9ec6-45da-a4ee-71b9f2bed156/using-metro-win8-sdk-to-build-desktop-style-application?forum=winappswithnativecode) and I didn't find it in the documentation.
* You need to edit the csproj to add the <TargetPlatformVersion>8.1</TargetPlatformVersion> to be able to reference WinRT

Once you do all that, the code is trivial, but for some reason the folks at Microsoft writing the examples thought Console apps (and servers) wouldn't benefit from using this API...

## Sample output
You run ```PeerFinder host``` on one laptop and ```PeerFinder client``` on the other, and then something as follows will be displayed:

### Host side
```
PeerFinder.exe host
Connection requested by backyard. Will be accepted automatically.
Receiver waiting to connect async
Receiver connection happened
Going to read 67108864 bytes
0/67108864
33554432/67108864
Received 64 MB in 6 secs = 10.67 MB/s
```

### Client side
```
PeerFinder.exe client
Peer found: PeerFinderConsoleApp
Going to send 67108864 bytes
Sent 64 MB in 6 secs = 10.67 MB/s
```

I obtained 10.67MB/s using 2 laptops directly connected. My local LAN is restricted to a poor 20mbps that gives about 2-3MB/s so wifi-direct is great on such scenarios.


