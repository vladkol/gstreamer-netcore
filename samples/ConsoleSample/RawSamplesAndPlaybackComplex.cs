
// Authors
//   Copyright (C) 2020  Vlad Kolesnikov <vlad@vladkol.com>
//   Copyright (C) 2014 Stephan Sundermann <stephansundermann@gmail.com>

using System;
using Gst;
using Gst.App;

namespace netcore3sample
{
    public static class RawSamplesAndPlaybackComplex
    {
        static Element Pipeline;
        static GLib.MainLoop MainLoop;
        static bool IsLive = false;

        public static void Run(ref string[] args, string source, string sourceOptions = "", 
                                bool withVideoPlayback = true, bool withAudioPlayback = true)
        {
            Console.WriteLine($"Getting raw video and audio samples and playing {source}");
            Application.Init(ref args);
            GtkSharp.GstreamerSharp.ObjectManager.Initialize(); // for AppSink, including finding exisitng app sinks

            bool validUri = false;
            if (Gst.Uri.IsValid(source))
            {
                var protocol = Gst.Uri.GetProtocol(source);
                if(Gst.Uri.ProtocolIsValid(protocol) && Gst.Uri.ProtocolIsSupported(URIType.Src, protocol))
                {
                    validUri = true;
                }
            }
            if(!validUri)
            {
                // still trying as a file path 
                source = "file://" + source.Replace('\\', '/'); 
            }

            // if needed to force TCP with rtps source and uridecodebin, 
            // use rtspt:// as source scheme. Also see http://gstreamer-devel.966125.n4.nabble.com/setting-protocols-for-rtsp-when-using-uridecodebin-tp4669327p4669328.html

            // We create a pipeline with 4 target sinks: 
            //                                                                             ==> autoaudiosink (audio playback)
            //                                                                           //
            //                                               ==> audio (tee 'audioTee')=||
            //                                             //                            \\    
            //                                            //                               ==> appsink 'audioSink' (raw audio samples)
            // =>>> source stream =>>> urlcodebin demux =||                 
            //                                            \\                               ==> autovideosink (video playback)
            //                                             \\                            //    
            //                                               ==> video (tee 'videoTee')=||
            //                                                                           \\
            //                                                                             ==> appsink 'videoSink' (raw video samples)
            // 
            // We can initialize a pipeline declaratively using Gst.Parse.Launch as if we were doing that in command line.
            // Then access pipeline elements by their names (audioSink and videoSink)
            
            Pipeline = Gst.Parse.Launch(
                    $"uridecodebin uri=\"{source}\" {sourceOptions} name=dmux " +       // using uridecodebin as a demuxer 
                                                                                        // you can also use replace it with: 
                                                                                        // for HTTP(S) - "souphttpsrc location=\"{source}\" {sourceOptions} ! decodebin name=dmux" 
                                                                                        // for RTSP - "rtspsrc location=\"{source}\" {sourceOptions} ! decodebin name=dmux" 

                    "dmux. ! queue ! audioconvert ! audio/x-raw,format=F32LE " +                // audio flow: raw PCM, 32bit float, little-endian 
                        "! tee name=audioTee " +                                                // create a tee for audio split 
                            (!withAudioPlayback ? " " : 
                                "audioTee. ! queue ! autoaudiosink ") +                         // first [optional] audio branch - an automatic sink for playback 
                            "audioTee. ! queue ! appsink name=audioSink " +                     // second audio branch - an appsink 'audioSink' for raw audio samples 
                    
                    "dmux. ! queue ! videoconvert " +                                           // video flow: raw RGBA, 32bpp 
                                                                                                // color conversion to RGBA on GPU 
                        "! glupload ! glcolorconvert ! video/x-raw(memory:GLMemory),texture-target=2D,format=(string)RGBA ! gldownload " +

                        "! tee name=videoTee " +                                                // create second tee - for video split 
                            (!withVideoPlayback ? " " :  
                                "videoTee. ! queue! autovideosink ") +                          // first [optional] video branch - an automatic sink for playback 
                            "videoTee. ! queue ! appsink name=videoSink");                      // second video branch - an appsink 'videoSink' for raw video samples 

            MainLoop = new GLib.MainLoop();

            Pipeline.Bus.AddSignalWatch();
            Pipeline.Bus.EnableSyncMessageEmission();
            Pipeline.Bus.Message += OnMessage;
            Pipeline.Bus.SyncMessage += OnSync;

            AppSink videoSink = null;
            var videoSinkElement = (Pipeline as Gst.Pipeline).GetChildByName("videoSink");
            if(videoSinkElement != null)
            {
                videoSink = videoSinkElement as AppSink;
                if(videoSink != null)
                {
                    videoSink.EmitSignals = true;
                    videoSink.NewSample += NewVideoSample;

                    videoSink.Drop = true;
                    videoSink.Sync = true;
                    videoSink.Qos = true;
                }
            }

            AppSink audioSink = null;
            var audioSinkElement = (Pipeline as Gst.Pipeline).GetChildByName("audioSink");
            if (audioSinkElement != null)
            {
                audioSink = audioSinkElement as AppSink;
                if (audioSink != null)
                {
                    audioSink.EmitSignals = true;
                    audioSink.NewSample += NewAudioSample;

                    audioSink.Drop = true;
                    audioSink.Sync = true;
                    audioSink.Qos = true;
                }
            }

            var ret = Pipeline.SetState(State.Playing);

            if (ret == StateChangeReturn.Failure)
            {
                Console.WriteLine("Unable to set the pipeline to the playing state.");
                return;
            }
            else if (ret == StateChangeReturn.NoPreroll)
            {
                IsLive = true;
                Console.WriteLine("Playing a live stream.");
            }

            MainLoop.Run();

            Pipeline.SetState(State.Null);
        }

        static void OnSync(object o, SyncMessageArgs args)
        {
            if (Gst.Video.Global.IsVideoOverlayPrepareWindowHandleMessage(args.Message))
            {
                Element src = (Gst.Element)args.Message.Src;
                src["force-aspect-ratio"] = true;
            }
        }

        static void NewVideoSample(object sender, NewSampleArgs args)
        {
            var sink = (Gst.App.AppSink)sender;

            // Retrieve the buffer
            var sample = sink.PullSample();
            if (sample != null)
            {
                Caps caps = sample.Caps;
                var cap = caps[0];

                string format;
                int width = 0;
                int height = 0;
                int fpsNumerator = 0;
                int fpsDenominator = 1;
                
                format = cap.GetString("format");
                cap.GetInt("width", out width);
                cap.GetInt("height", out height);
                cap.GetFraction("framerate", out fpsNumerator, out fpsDenominator);

                MapInfo map;
                if (sample.Buffer.Map(out map, MapFlags.Read))
                {
                    // TODO: work with your RGBA frame in map.Data or map DataPtr or use map.CopyTo(IntPtr, long) to copy raw memory 
                    sample.Buffer.Unmap(map);
                }
                sample.Dispose();
            }
        }

        static void NewAudioSample(object sender, NewSampleArgs args)
        {
            var sink = (Gst.App.AppSink)sender;

            // Retrieve the buffer
            var sample = sink.PullSample();
            if (sample != null)
            {
                var caps = sample.Caps;
                var cap = caps[0];

                int rate = 0; // audio rate 
                int channels = 0; // number of audio channels 

                cap.GetInt("rate", out rate);
                cap.GetInt("channels", out channels);

                MapInfo map;
                if (sample.Buffer.Map(out map, MapFlags.Read))
                {
                    float[] f32leSamples = new float[map.Size / sizeof(float)];
                    // convert sample's byte array to floats 
                    System.Buffer.BlockCopy(map.Data, 0, f32leSamples, 0, f32leSamples.Length * sizeof(float));
                    sample.Buffer.Unmap(map);

                    // TODO: work with your F32LE sample in f32leSamples
                }

                sample.Dispose();
            }
        }

        static void OnMessage(object e, MessageArgs args)
        {
            switch (args.Message.Type)
            {
                case MessageType.StateChanged:
                    State oldstate, newstate, pendingstate;
                    args.Message.ParseStateChanged(out oldstate, out newstate, out pendingstate);
                    System.Console.WriteLine($"[StateChange] From {oldstate} to {newstate} pending at {pendingstate}");
                    break;
                case MessageType.StreamStatus:
                    Element owner;
                    StreamStatusType type;
                    args.Message.ParseStreamStatus(out type, out owner);
                    System.Console.WriteLine($"[StreamStatus] Type {type} from {owner}");
                    break;
                case MessageType.DurationChanged:
                    long duration;
                    Pipeline.QueryDuration(Format.Time, out duration);
                    System.Console.WriteLine($"[DurationChanged] New duration is {(duration / Gst.Constants.SECOND)} seconds");
                    break;
                case MessageType.ResetTime:
                    ulong runningtime = args.Message.ParseResetTime();
                    System.Console.WriteLine($"[ResetTime] Running time is {runningtime}");
                    break;
                case MessageType.AsyncDone:
                    ulong desiredrunningtime = args.Message.ParseAsyncDone();
                    System.Console.WriteLine($"[AsyncDone] Running time is {desiredrunningtime}");
                    break;
                case MessageType.NewClock:
                    Clock clock = args.Message.ParseNewClock();
                    System.Console.WriteLine($"[NewClock] {clock}");
                    break;
                case MessageType.Buffering:
                    int percent = args.Message.ParseBuffering();
                    System.Console.WriteLine($"[Buffering] {percent}% done");

                    if (!IsLive)
                    {
                        if (percent < 100)
                        {
                            Pipeline.SetState(State.Paused);
                        }
                        else
                        {
                            Pipeline.SetState(State.Playing);
                        }
                        break;
                    }
                    break;
                case MessageType.Tag:
                    TagList list = args.Message.ParseTag();
                    System.Console.WriteLine($"[Tag] Information in scope {list.Scope} is {list.ToString()}");
                    break;
                case MessageType.Error:
                    GLib.GException gerror;
                    string debug;
                    args.Message.ParseError(out gerror, out debug);
                    System.Console.WriteLine($"[Error] {gerror.Message}, debug information {debug}.");
                    //MainLoop.Quit();
                    break;
                case MessageType.Eos:
                    System.Console.WriteLine("[Eos] Playback has ended. Exiting!");
                    MainLoop.Quit();
                    break;
                default:
                    System.Console.WriteLine($"[Recv] {args.Message.Type}");
                    break;
            }
        }
    }
}