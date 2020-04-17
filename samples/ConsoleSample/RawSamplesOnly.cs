
// Authors
//   Copyright (C) 2020  Vlad Kolesnikov <vlad@vladkol.com>
//   Copyright (C) 2014 Stephan Sundermann <stephansundermann@gmail.com>

using System;
using Gst;
using Gst.App;
using Gst.Video;

namespace netcore3sample
{
    public static class RawSamplesOnly
    {
        static Pipeline Pipeline;
        static Element Playbin;
        static AppSink VideoSink;
        static GLib.MainLoop MainLoop;
        static bool IsLive = false;

        public static void Run(ref string[] args, string source, string sourceOptions = "")
        {
            Console.WriteLine($"Playing video and audio from {source}");
            Application.Init(ref args);

            Pipeline = new Gst.Pipeline("simplepipeline");
            VideoSink = new AppSink("videoSink");
            Playbin = ElementFactory.Make("playbin", "playbin");

            Playbin["uri"] = source;
            Playbin["video-sink"] = VideoSink;

            VideoSink["caps"] = Caps.FromString("video/x-raw,format=RGBA");
            VideoSink.EmitSignals = true;
            VideoSink.NewSample += NewVideoSample;
            VideoSink.Drop = true;
            VideoSink.Sync = true;
            VideoSink.Qos = true;
            
            Pipeline.Add(Playbin);

            MainLoop = new GLib.MainLoop();

            Pipeline.Bus.AddSignalWatch();
            Pipeline.Bus.Message += OnMessage;

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