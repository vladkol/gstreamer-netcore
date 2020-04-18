# GStreamer-Sharp for .NET Core
## GStreamer bindings for .NET Core
![GStreamer logo](https://gstreamer.freedesktop.org/data/images/artwork/gstreamer-logo.svg)

This library is .NET Core bindings for [GStreamer](https://gstreamer.freedesktop.org/) made on top of the original [gstreamer-sharp](https://gitlab.freedesktop.org/gstreamer/gstreamer-sharp) bindings. The library uses gstreamer-sharp sources as a submodule made from [gstreamer-sharp GitHub mirror](https://github.com/GStreamer/gstreamer-sharp).

[![NuGet Badge](https://buildstats.info/nuget/gstreamer-sharp-netcore)](https://www.nuget.org/packages/gstreamer-sharp-netcore/)

GStreamer is a library for constructing graphs of media-handling components.
<details>
<summary>What exactly can I do with GStreamer?</summary>
GStreamer supports a range of scenarios from simple audio and video playback and streaming to complex audio (mixing) and video (non-linear editing) processing.

Applications can take advantage of advances in codec and filter technology transparently. Developers can add new codecs and filters by writing a simple plugin with a clean, generic interface. 

[Even more details](https://gstreamer.freedesktop.org/features/index.html)
</details>

**GStreamer-Sharp** is a .NET/mono binding for Gstreamer
generated from gobject-introspection data using the [bindinator](https://github.com/GLibSharp/bindinator). GStreamer-sharp currently wraps the API exposed by Gstreamer 1.12 and is compatible with newer gstreamer versions. It was developed
under GSoC 2014 for the mono organization. gstreamer-sharp covers
the core and base gstreamer libraries.

## Prerequisites
* .NET Core 3.1+ runtime for running the apps or .NET Core SDK 3.1+ for development 
* gstreamer core with "base" and "good" plugins 1.14 or higher (you may need "libav" wrapper or "bad"/"ugly" plugins for some features). 
[What is that?](https://gstreamer.freedesktop.org/documentation/additional/splitup.html)

**You need to have Gstreamer and its plugins binaries [installed](https://gstreamer.freedesktop.org/documentation/installing/index.html) and added to PATH environment variable!**
* [Installing GStreamer on Ubuntu](https://gstreamer.freedesktop.org/documentation/installing/on-linux.html#install-gstreamer-on-ubuntu-or-debian)
* [Installing GStreamer on macOS](https://gstreamer.freedesktop.org/documentation/installing/on-mac-osx.html) (Homebrew works as well)
* [Installing GStreamer on Windows](https://gstreamer.freedesktop.org/documentation/installing/on-windows.html) 
<details>
<summary>Some Windows-speficic tips</summary>
On Windows, gstreamer-sharp works only if you installed a MiniGW build. 

* We tested with [gstreamer-1.0-mingw-x86_64-1.16.2](https://gstreamer.freedesktop.org/data/pkg/windows/1.16.2/gstreamer-1.0-mingw-x86_64-1.16.2.msi). 

When installing GStreamer on Windows, in addition to default components, select *"Gstreamer 1.0 libav wrapper"*. 

You may also need to create **GST_PLUGIN_PATH** environment variable pointing to %GSTREAMER_1_0_ROOT_X86_64%\lib\gstreamer-1.0 (C:\gstreamer\1.0\x86_64\lib\gstreamer-1.0). 
</details>

## Quick Start
1) Create a .NET Core project. You need one for [.NET Core SDK 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) or above. 
```
dotnet new console
```
*If making a class library, make sure it targets .NET Core 3.1+*: 
```
dotnet new classlib -f netcoreapp3.1  
```
2) Add [gstreamer-sharp-netcore](https://www.nuget.org/packages/gstreamer-sharp-netcore/) NuGet package to your .NET Core app. 
```
dotnet add package gstreamer-sharp-netcore
```

3) Here is a very minimal app ([BasicTutoria1](https://github.com/GStreamer/gstreamer-sharp/blob/master/samples/BasicTutorial1.cs)). You may also look at how they do it in more [Samples](#samples).

```cs
using System;
using Gst; 

namespace GstreamerSharp
{
	class Program
	{
		public static void Main (string[] args)
		{
			// Initialize Gstreamer
			Application.Init(ref args);

			// Build the pipeline
			var pipeline = Parse.Launch("playbin uri=http://download.blender.org/durian/trailer/sintel_trailer-1080p.mp4");

			// Start playing
			pipeline.SetState(State.Playing);

			// Wait until error or EOS
			var bus = pipeline.Bus;
			var msg = bus.TimedPopFiltered (Constants.CLOCK_TIME_NONE, MessageType.Eos | MessageType.Error);

			// Free resources
			pipeline.SetState (State.Null);
		}
	}
}
```


## Building 
1) Clone the repository **recursively** (notice --recurse flag)
2) Build the project using dotnet command line or a compatible IDE of your choice (I use [Visual Studio Code](https://code.visualstudio.com/) on Windows, macOS and Linux)
```
git clone https://github.com/vladkol/gstreamer-netcore --recurse
cd gstreamer-netcore
dotnet build
```

## Supported Platforms
Any operating system and environment that GStreamer and .NET Core 3.1 can run on. We tested on Windows 10 1909, macOS Catalina and Ubuntu 18.04. 

## Samples
GStreamer-Sharp has [a plenty of samples](https://github.com/GStreamer/gstreamer-sharp/tree/master/samples), and we also included [a few](samples): 
* [Console app](samples/ConsoleSample) demostrating different audio and video playback scenarios 
* Rudimentary [video player](samples/AvaloniaPlayer) made with [Avalonia UI](https://github.com/AvaloniaUI/Avalonia)

## License 
gstreamer-sharp is licensed under the [LGPL 2.1](https://www.gnu.org/licenses/lgpl-2.1.html), same as many parts of GStreamer and gstreamer-sharp.

