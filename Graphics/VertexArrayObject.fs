module Graphics.VertexArrayObject

open System
open Graphics.GLSLAttributeAttribute
open Microsoft.FSharp.Core
open Silk.NET.OpenGL

open Graphics.GLExtensions


/// VertexArrayObject instance. Instantiating does the following:
/// 1. create a new opengl vertexArrayObject on the gpu
/// 2. bind that newly created object (i.e. immediately makes it the active vertexArrayObject)
type VertexArrayObject (gl: GL) =
    let _bind handle = gl.glDo <| fun () -> gl.BindVertexArray handle
    
    let handle =
        let handle = gl.glDo gl.GenVertexArray
        _bind handle
        handle
    
    member this.bind () = _bind handle
    
    member this.delete () = gl.glDo <| fun () -> gl.DeleteVertexArray handle
    
    member private this.getAttributes<'a when 'a: unmanaged> () =
        typeof<'a>.GetCustomAttributes(typeof<GLSLAttributeAttribute>, false)
        |> Array.map (fun obj -> obj :?> GLSLAttributeAttribute)
    
    member this.enableVertexAttributes<'a when 'a: unmanaged> () =
        let sizeOfType = this.vertexAttribPointerTypeSize VertexAttribPointerType.Float
        let glslAttributes = this.getAttributes<'a>()
        
        if glslAttributes.Length = 0 then
            printfn $"[Playground Warning]: Count not find any GLSLAttributes for type {typeof<'a>}"
        else    
            let mutable offsetCount = 0
            
            for a in glslAttributes do
                // enable the attribute
                gl.glDo <| fun () -> gl.EnableVertexAttribArray a.Location
                
                // tell gl the shape and location of the attribute
                gl.glDo <| fun () ->
                    gl.VertexAttribPointer(
                        a.Location,
                        int a.DataType, // 1, 2, 3, or 4 (scalar, vec2, vec3, vec4)
                        VertexAttribPointerType.Float, // float, int, byte, etc.
                        false,
                        uint sizeof<'a>, // stride of each vertex
                        offsetCount * sizeOfType |> nativeint |> fun i -> i.ToPointer())
                
                // increment the offset counter
                offsetCount <- offsetCount + int a.DataType
            
    
    member private this.vertexAttribPointerTypeSize (pointerType: VertexAttribPointerType) =
        match pointerType with
        | VertexAttribPointerType.Byte -> sizeof<byte>
        | VertexAttribPointerType.Double -> sizeof<double>
        | VertexAttribPointerType.Float -> sizeof<float32>
        | VertexAttribPointerType.Int -> sizeof<Int32>
        | VertexAttribPointerType.Short -> sizeof<Int16>
        | _ ->
            printfn $"[Playground Warning]: Unsupported VertexAttribPointerType - {pointerType}"
            printfn "VertexShaderObject.vertexAttribPointTypeSize should be extended to handle this enum variant."
            printfn "Falling back to sizeof<float32>, but cannot ensure program correctness."
            sizeof<float32>