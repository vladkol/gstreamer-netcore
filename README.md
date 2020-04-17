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
generated from gobject-introspection data using the [bindinator](https://github.com/GLibSharp/bindinator). GStreamer-sharp currently wraps the API exposed by Gstreamer 1.16 and is compatible with newer gstreamer versions. It was developed
under GSoC 2014 for the mono organization. gstreamer-sharp covers
the core and base gstreamer libraries.

## Prerequisites
* .NET Core 3.1+ runtime for running the apps or .NET Core SDK 3.1+ for development 
* gstreamer core, with "base", "good" and "bad" plugins 1.14 or higher 

**You need to have Gstreamer and its plugins binaries [installed](https://gstreamer.freedesktop.org/documentation/installing/index.html) and added to PATH environment variable!**

## Quick Start
1) Create a .NET Core project. 
2) Add [gstreamer-sharp-netcore](https://www.nuget.org/packages/gstreamer-sharp-netcore/) NuGet package to your .NET Core app. 
3) Look how they do in in [Samples](#samples).

A very minimal app ([BasicTutoria1](https://github.com/GStreamer/gstreamer-sharp/blob/master/samples/BasicTutorial1.cs)):
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
Everywhere GStreamer and .NET Core 3.1 run. Tested on Windows 10, macOS Catalina and Ubuntu 18.04.

## Samples
GStreamer-Sharp has [a plenty of samples](https://github.com/GStreamer/gstreamer-sharp/tree/master/samples), and we also included [a few](samples): 
* [Console app](samples/ConsoleSample) with demostrating different audio and video playback scenarios 
* Rudimentary [video player](samples/AvaloniaPlayer) made with [Avalonia UI](https://github.com/AvaloniaUI/Avalonia)

## License 
gstreamer-sharp is licensed under the [LGPL 2.1](https://www.gnu.org/licenses/lgpl-2.1.html), same as many parts of GStreamer and gstreamer-sharp.

