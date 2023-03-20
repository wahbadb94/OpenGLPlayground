module Graphics.ShaderProgram

open System.Diagnostics
open System.IO
open System.Numerics
open System.Runtime.InteropServices
open Silk.NET.OpenGL

open GLExtensions
open ShaderHelpers

let private uniformNotFoundReason =
    @"Possible causes include:
        1. Misspelled uniform name.
        2. Uniform unused in shader, and was stripped during shader compilation.
    "

type ShaderProgram (gl: GL, shaderInit, vertPath, fragPath) =
    // unique program id on the GPU, set of uniforms on the shader
    let mutable programHandle, uniformMap = shaderInit
    
    // -----------------------------------------------------------------
    // ------------------ GLSL hot reload watchers ---------------------
    // -----------------------------------------------------------------
    
    let reload gl =
        match ShaderHelpers.buildProgram gl vertPath fragPath with
        | Ok (newHandle, newUniforms) ->
            let prevHandle = programHandle
            programHandle <- newHandle
            uniformMap <- newUniforms
            ShaderHelpers.deleteProgram gl prevHandle
        | Error msg ->
            printfn "[Hot Reload Error]: Error while reloading the shader program."
            printfn $"{msg}"
            
    let sw = Stopwatch.StartNew ()
    let mutable lastUpdate = 0L
    let mutable shouldReload = false 
    let mutable watcher = new FileSystemWatcher (Path = (FileInfo fragPath).Directory.FullName)
    do
        watcher.NotifyFilter <- NotifyFilters.LastWrite
        watcher.EnableRaisingEvents <- true
        watcher.Changed.Add <| fun event ->
            if ( event.FullPath = fragPath || event.FullPath = vertPath ) && (sw.ElapsedMilliseconds - lastUpdate > 1000L) then
                printfn $"[Hot Reload Update]: {event.Name}"
                shouldReload <- true
                lastUpdate <- sw.ElapsedMilliseconds
     
    // -----------------------------------------------------------------
    // ---------------------  public members ---------------------------
    // -----------------------------------------------------------------
    
    member this.useProgram () = gl.UseProgram programHandle
    
    member this.delete () =
        watcher.Dispose ()
        ShaderHelpers.deleteProgram gl programHandle
    
    member this.update gl =
        if shouldReload then
            reload gl
            shouldReload <- false
    
    // -----------------------------------------------------------------
    // --------------------- setting uniforms --------------------------
    // -----------------------------------------------------------------
    
    // scalar
    member this.setUniform (name, value: int) =
        this.getUniformLocation name |> this.locationOptionMap name (fun location ->
            gl.glDo <| fun () -> gl.Uniform1 (location, value) ) |> ignore
    
    member this.setUniform (name, value: float32) =
        this.getUniformLocation name |> this.locationOptionMap name (fun location ->
            gl.glDo <| fun () -> gl.Uniform1 (location, value) ) |> ignore
    
    // vectors
    member this.setUniform (name, data: Vector4) =
        this.getUniformLocation name |> this.locationOptionMap name (fun location ->
            gl.glDo <| fun () -> gl.Uniform4 (location, data) ) |> ignore
    
    member this.setUniform (name, data: Vector3) =
        this.getUniformLocation name |> this.locationOptionMap name (fun location ->
            gl.glDo <| fun () -> gl.Uniform3 (location, data) ) |> ignore
        
    // mat4
    member this.setUniform (name, mat: Matrix4x4) =
        this.getUniformLocation name |> this.locationOptionMap name (fun location ->
            gl.glDo <| fun () ->
                let span = MemoryMarshal.CreateReadOnlySpan(ref mat, 1)
                let asFloat = MemoryMarshal.Cast<Matrix4x4, float32>(span)
                gl.UniformMatrix4(location, 1u, false, asFloat) ) |> ignore
        
    // -----------------------------------------------------------------
    // ---------------------- private members --------------------------
    // -----------------------------------------------------------------
    member private this.getUniformLocation (name: string): int option =
          uniformMap |> Map.tryFind name |> Option.map (fun u -> u.location)
          
    member private this.locationOptionMap name f opt =
        match opt with
        | Some location -> Some <| f location
        | None ->
            printfn $"Could not find uniform location for {name}"
            printfn $"{uniformNotFoundReason}"
            None