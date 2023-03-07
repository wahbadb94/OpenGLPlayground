module Graphics.App

open System.Runtime.InteropServices
open Microsoft.FSharp.Core
open Microsoft.FSharp.NativeInterop
open Silk.NET.Maths
open Silk.NET.OpenGL
open Silk.NET.Windowing

type WindowSize = Vector2D<int>

type WindowOptions = { title: string; size: Vector2D<int> }

/// interface that drives an App
type Sketch<'a> =
    { /// returns a the initial state of the app, wrapped in a Result.
      OnInit: GL -> WindowSize -> Result<'a, string>
      /// returns new state from previous state
      OnUpdate: GL -> 'a -> 'a
      /// handles rendering, takes state as input
      OnRender: GL -> 'a -> unit
      /// handles updates that occur when the Window is resized.
      OnResize: WindowSize -> 'a -> 'a
      /// invoked when the window is closed. Can be used to clean up any GPU memory, etc.
      OnClose: GL -> 'a -> unit }
        
/// instance of some generic sketch (Sketch<'a>). It functions as both a container for the mutable sketch state and a
/// wrapper around/adapter for Sketch lifecycle methods, allowing them to be hooked into the Silk window lifecycle methods
type private SketchInstance<'a> (gl: GL, size: WindowSize, sketch: Sketch<'a>) =
    let mutable state =
        match sketch.OnInit gl size with
        | Ok s -> s
        | Error errorMsg ->
            printfn $"Failed to Initialize SketchInstance:"
            printfn $"{errorMsg}"
            
            exit 1
            Unchecked.defaultof<'a> // doesn't actually get used
    
    member this.onUpdate (_: float) = state <- sketch.OnUpdate gl state
    
    member this.onResize v = state <- sketch.OnResize v state
        
    member this.onRender (_: float) = sketch.OnRender gl state
    
    member this.onClose () =
        sketch.OnClose gl state
        printfn "Sketch closed gracefully."

/// Top level entity to be instantiated in Program.fs
type App<'a>(sketch: Sketch<'a>, windowOptions: WindowOptions) =
    let window =
        let mutable o = WindowOptions.Default
        o.Title <- windowOptions.title
        o.Size <- windowOptions.size
        Window.Create(o)

    let onLoad () =
        let gl = GL.GetApi(window)
        let sketchInstance = SketchInstance(gl, windowOptions.size, sketch)
        
        // NOTE: the following looks redundant, but fixes F# func to C# Action conversion issue
        let onUpdate = sketchInstance.onUpdate
        let onRender = sketchInstance.onRender
        let onClose = sketchInstance.onClose
        let onResize = sketchInstance.onResize
        
        window.add_Update onUpdate
        window.add_Render onRender
        window.add_Resize onResize
        window.add_Closing onClose

        // print OpenGL version info
        gl.GetString(StringName.Version)
        |> NativePtr.toNativeInt
        |> Marshal.PtrToStringAnsi
        |> fun version -> printfn $"OPENGL VERSION: {version}"

    member this.Run() =
        window.add_Load onLoad
        window.Run()
