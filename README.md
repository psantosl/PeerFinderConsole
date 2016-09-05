# PeerFinderConsole
Shows how to use the [PeerFinder](https://msdn.microsoft.com/en-us/library/windows/apps/windows.networking.proximity.peerfinder) proximity API from a C# Console app.

PeerFinder allows you to create wifi-direct sockets very easily.

But, for some reason, all the samples are for UWP or Windows Store apps, which is a pain. And all the PeerFinder methods show the same code fragment as an example, which is all poluted with UI code.

## Important point to make PeerFinder work on Console
* You need to invoke this ```SetCurrentProcessExplicitAppUserModelID``` for the PeerFinder to work. This is key, otherwise NOTHING works. It is explained [here](https://social.msdn.microsoft.com/Forums/en-US/ce649545-9ec6-45da-a4ee-71b9f2bed156/using-metro-win8-sdk-to-build-desktop-style-application?forum=winappswithnativecode) and I didn't find it in the documentation.
* You need to edit the csproj to add the <TargetPlatformVersion>8.1</TargetPlatformVersion> to be able to reference WinRT

Once you do all that, the code is trivial, but for some reason the folks at Microsoft writing the examples thought Console apps (and servers) wouldn't benefit from using this API...




