module Sketches.CounterSketch

open System.Diagnostics
open Graphics.App

type FpsState =
    { FrameCount: int64
      Stopwatch: Stopwatch
      Fps: int64 }

let counterApp: Sketch<FpsState> =
    { OnInit =
        fun _ __ ->
            { FrameCount = 0L
              Stopwatch = Stopwatch.StartNew()
              Fps = 0 } |> Ok
      OnClose = fun _ __ -> ()
      OnResize = fun _ prev  -> prev
      OnUpdate =
        fun _ prev ->
            if not prev.Stopwatch.IsRunning then
                prev.Stopwatch.Start()

            let count = prev.FrameCount + 1L

            let Fps =
                count * 1000L / prev.Stopwatch.ElapsedMilliseconds

            { prev with
                FrameCount = count
                Fps = Fps }
      OnRender = fun _ state -> printfn $"Average fps: {state.Fps}" }
