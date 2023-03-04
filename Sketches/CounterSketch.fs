module OpenGLPlayground.Sketches.Sketches.CounterSketch

open System.Diagnostics
open OpenGLPlayground.Infrastructure.Graphics.App

type FpsState =
    { FrameCount: int64
      Stopwatch: Stopwatch
      Fps: int64 }

let counterApp: Sketch<FpsState> =
    { OnInit =
        fun _ ->
            { FrameCount = 0L
              Stopwatch = Stopwatch.StartNew()
              Fps = 0 }
      OnClose = fun _ __ -> ()
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
