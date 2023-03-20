module Graphics.App

open System
open System.Runtime.InteropServices
open FSharpPlus
open Microsoft.FSharp.Core
open Microsoft.FSharp.NativeInterop
open Silk.NET.Maths
open Silk.NET.OpenGL
open Silk.NET.Windowing
open Silk.NET.Input

open GLExtensions

type WindowSize = Vector2D<int>

type WindowOptions = { title: string; size: Vector2D<int> }

/// interface that drives an App
type Sketch<'a> =
    { /// returns a the initial state of the app, wrapped in a Result.
      OnInit: GL -> WindowSize -> Result<'a, string>
      /// invoked when window keydown event fires.
      onKeyDown: IKeyboard -> Key -> int -> unit
      /// returns new state from previous state
      OnUpdate: GL -> IKeyboard -> 'a -> 'a
      /// handles rendering, takes state as input
      OnRender: GL -> 'a -> unit
      /// handles updates that occur when the Window is resized.
      OnResize: WindowSize -> 'a -> 'a
      /// invoked when the window is closed. Can be used to clean up any GPU memory, etc.
      OnClose: GL -> 'a -> unit }
        
/// instance of some generic sketch (Sketch<'a>). It functions as both a container for the mutable sketch state and a
/// wrapper around/adapter for Sketch lifecycle methods, allowing them to be hooked into the Silk window lifecycle methods
type private SketchInstance<'a> (gl: GL, size: WindowSize, sketch: Sketch<'a>, keyboard: IKeyboard) =
    let mutable state =
        match sketch.OnInit gl size with
        | Ok s -> s
        | Error errorMsg ->
            printfn "Failed to Initialize SketchInstance:"
            printfn $"{errorMsg}"
            
            exit 1
            Unchecked.defaultof<'a> // doesn't actually get used
    
    member this.onUpdate (_timeDelta: float) = state <- sketch.OnUpdate gl keyboard state
    member this.onKeyDown (keyboard: IKeyboard) ( key: Key ) ( num: int) = sketch.onKeyDown keyboard key num  
    member this.onResize size = state <- sketch.OnResize size state
    member this.onRender (_timeDelta: float) = sketch.OnRender gl state
    member this.onClose () =
        sketch.OnClose gl state
        printfn "Sketch closed gracefully."

/// Top level entity to be instantiated in Program.fs
type App<'a>(sketch: Sketch<'a>, windowOptions: WindowOptions) =
    let window =
        let mutable o = WindowOptions.Default
        o.Title <- windowOptions.title
        o.Size <- windowOptions.size
        o.Samples <- 4
        o.VSync <- true
        o.API <- GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, APIVersion(4, 6))
        
        Window.Create o

    let onLoad () =
        let gl = GL.GetApi(window)
        
        let input = window.CreateInput()
        let keyboardMaybe = input.Keyboards |> IReadOnlyList.tryItem 0
        
        match keyboardMaybe with
        | Some keyboard ->
            // create the sketch
            let sketchInstance = SketchInstance(gl, windowOptions.size, sketch, keyboard)
            
            // register handlers
            keyboard.add_KeyDown <| Action<IKeyboard, Key, int> sketchInstance.onKeyDown 
            window.add_Update <| Action<float> sketchInstance.onUpdate
            window.add_Render <| Action<float> sketchInstance.onRender
            window.add_Closing <| Action sketchInstance.onClose
            window.add_Resize <| Action<Vector2D<int>>(
                fun s ->
                    gl.glDo <| fun () -> gl.Viewport s
                    sketchInstance.onResize s )
            
            // print OpenGL version info
            gl.GetString(StringName.Version)
            |> NativePtr.toNativeInt
            |> Marshal.PtrToStringAnsi
            |> fun version -> printfn $"OPENGL VERSION: {version}"
        | None -> printfn "Could not find a connected keyboard." 
        
    member this.Run() =
        window.add_Load onLoad
        window.Run()