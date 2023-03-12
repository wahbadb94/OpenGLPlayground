module Graphics.BufferObject

open System
open Microsoft.FSharp.NativeInterop
open Silk.NET.OpenGL
open GLExtensions

type BufferObject<'T when 'T : unmanaged >(gl: GL, data: 'T array, bufferType: BufferTargetARB) =
    // create a handle to the buffer
    let handle = gl.glDo gl.GenBuffer
    let _bind () = gl.glDo <| fun () -> gl.BindBuffer(bufferType, handle)
    let _unbind () = gl.glDo <| fun () -> gl.BindBuffer(bufferType, 0u)
    
    do
        // bind the buffer
        _bind ()
        
        // set the buffer data
        use d = fixed data
        gl.glDo <| fun () -> gl.BufferData(
                bufferType,
                unativeint (data.Length * sizeof<'T>),
                ReadOnlySpan<float32>(NativePtr.toVoidPtr d, data.Length),
                BufferUsageARB.StaticDraw)
        
    do _unbind ()
    
    member this.bind = _bind
    member this.unbind = _unbind
    member this.delete () = gl.glDo <| fun () -> gl.DeleteBuffer handle