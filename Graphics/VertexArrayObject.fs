module Graphics.VertexArrayObject

open System
open Microsoft.FSharp.Core
open Silk.NET.OpenGL

open Graphics.GLExtensions

type VertexAttributeType =
    | Scalar = 1
    | Vec2 = 2
    | Vec3 = 3
    | Vec4 = 4

/// VertexArrayObject instance. Instantiating does the following:
/// 1. create a new opengl vertexArrayObject on the gpu
/// 2. bind that newly created object (i.e. immediately makes it the active vertexArrayObject)
type VertexArrayObject (gl: GL) =
    let _bind handle = gl.glDo <| fun () -> gl.BindVertexArray handle
    
    let handle =
        let handle = gl.glDo gl.GenVertexArray
        _bind handle
        handle

    let mutable nextAttributeLocation = 0u
    let mutable offsetCount = 0
    
    member this.bind () = _bind handle
    
    member this.delete () = gl.glDo <| fun () -> gl.DeleteVertexArray handle
    
    // TODO: would be nice if the type 'a provided enough info to calc attributeType and pointerType for all of its attributes
    // maybe as long as not used within the update/render loop, attributes + reflection is okay?
    member this.addVertexAttribute<'a when 'a: struct> (attributeType: VertexAttributeType) (pointerType: VertexAttribPointerType) =
        let sizeOfType = this.vertexAttribPointerTypeSize pointerType
        let attributeCount = int attributeType
        
        // enable the attribute
        gl.glDo <| fun () -> gl.EnableVertexAttribArray(nextAttributeLocation)
        
        // tell gl the shape and location of the attribute
        gl.glDo <| fun () ->
            gl.VertexAttribPointer(
                nextAttributeLocation,
                attributeCount, // 1, 2, 3, or 4
                pointerType, // float, int, byte, etc.
                false,
                uint sizeof<'a>, // stride 
                offsetCount * sizeOfType |> nativeint |> fun i -> i.ToPointer())
        
        // update the location and offset accumulators
        nextAttributeLocation <- nextAttributeLocation + 1u // next location we can assign to is +1
        offsetCount <- offsetCount + attributeCount // next attribute will have offset += attributeCount
    
    member private this.vertexAttribPointerTypeSize (pointerType: VertexAttribPointerType) =
        match pointerType with
        | VertexAttribPointerType.Byte -> sizeof<byte>
        | VertexAttribPointerType.Double -> sizeof<double>
        | VertexAttribPointerType.Float -> sizeof<float32>
        | VertexAttribPointerType.Int -> sizeof<Int32>
        | VertexAttribPointerType.Short -> sizeof<Int16>
        | _ ->
            printfn $"[Warning]: Unsupported VertexAttribPointerType - {pointerType}"
            printfn "VertexShaderObject.vertexAttribPointTypeSize should be extended to handle this enum variant."
            printfn "Falling back to sizeof<float32>, but cannot ensure program correctness."
            sizeof<float32>