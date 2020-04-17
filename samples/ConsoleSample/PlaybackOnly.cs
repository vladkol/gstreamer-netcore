
// Authors
//   Copyright (C) 2020  Vlad Kolesnikov <vlad@vladkol.com>
//   Copyright (C) 2014 Stephan Sundermann <stephansundermann@gmail.com>

using System;
using Gst;
using Gst.App;

namespace netcore3sample
{
    public static class PlaybackOnly
    {
        static Element Pipeline;
        static GLib.MainLoop MainLoop;
        static bool IsLive = false;

        public static void Run(ref string[] args, string source, string sourceOptions = "")
        {
            Console.WriteLine($"Playing video and audio from {source}");
            Application.Init(ref args);
            Pipeline = Gst.Parse.Launch($"playbin uri=\"{source}\" {sourceOptions}");

            MainLoop = new GLib.MainLoop();

            Pipeline.Bus.AddSignalWatch();
            Pipeline.Bus.EnableSyncMessageEmission();
            Pipeline.Bus.Message += OnMessage;
            Pipeline.Bus.SyncMessage += OnSync;
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