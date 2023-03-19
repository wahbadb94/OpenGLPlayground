module Graphics.ShaderProgram

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

type ShaderProgram (gl: GL, initialHandle, vertPath, fragPath) =
    let mutable programHandle = initialHandle
    
    let mutable updateFunc = None
    
    let directoryToWatch =
        let info = FileInfo vertPath
        info.Directory.FullName
        
    let mutable watcher = new FileSystemWatcher(Path = directoryToWatch)
    do
        watcher.NotifyFilter <- NotifyFilters.LastWrite
        watcher.EnableRaisingEvents <- true
        watcher.Created.Add <| fun event ->
            printfn "update detected"
            if event.Name = fragPath then
                match ShaderHelpers.buildProgram gl vertPath fragPath with
                | Ok newHandle ->
                    updateFunc <- Some <| fun () ->
                        let prevHandle = programHandle
                        programHandle <- newHandle
                        ShaderHelpers.deleteProgram gl prevHandle
                | Error msg ->
                    printfn $"[Shader Hot Reload Error]: Error while updating ${fragPath}"
                    printfn $"{msg}"
    
    // -----------------------------------------------------------------
    // ---------------------  public members ---------------------------
    // -----------------------------------------------------------------
    
    member this.useProgram () = gl.UseProgram programHandle
    
    member this.delete () =
        watcher.Dispose ()
        ShaderHelpers.deleteProgram gl programHandle
    
    member this.update () =
        match updateFunc with
        | Some f ->
            f ()
            updateFunc <- None
        | None  -> ()
    
    // -----------------------------------------------------------------
    // --------------------- setting uniforms --------------------------
    // -----------------------------------------------------------------
    
    // scalar
    member this.setUniform  (name, value: int) =
        let location = this.getUniformLocation name
        gl.glDo <| fun () -> gl.Uniform1 (location, value)
    
    member this.setUniform  (name, value: float32) =
        let location = this.getUniformLocation name
        gl.glDo <| fun () -> gl.Uniform1 (location, value)
    
    // vectors
    member this.setUniform  (name, data: Vector4) =
        let location = this.getUniformLocation name
        gl.glDo <| fun () -> gl.Uniform4 (location, data)
    
    member this.setUniform (name, data: Vector3) =
        let location = this.getUniformLocation name
        gl.glDo <| fun () -> gl.Uniform3 (location, data)
        
    // mat4
    member this.setUniform (name, mat: Matrix4x4) =
        let location = this.getUniformLocation name
        gl.glDo <| fun () ->
            let span = MemoryMarshal.CreateReadOnlySpan(ref mat, 1)
            let asFloat = MemoryMarshal.Cast<Matrix4x4, float32>(span)
            gl.UniformMatrix4(location, 1u, false, asFloat)
    
    
    // -----------------------------------------------------------------
    // ---------------------- private members --------------------------
    // -----------------------------------------------------------------
    
    member private this.getUniformLocation (name: string) =
        let location = gl.glDo <| fun () -> gl.GetUniformLocation (programHandle, name)
        if location = -1 then
            printfn $"Could not find uniform location for {name}"
            printfn $"{uniformNotFoundReason}"
            
        location