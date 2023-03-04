module Sketches.QuadSketch

open System
open System.Diagnostics
open System.Numerics
open Graphics.ShaderProgram
open Silk.NET.OpenGL

open Graphics.App
open Graphics.BufferObject
open Graphics.GLExtensions

type QuadState =
    {
      Vbo: BufferObject<float32>
      Ebo: BufferObject<int>
      Vao: uint
      Shader: ShaderProgram
      uColorG: float32 }

let private vertices =
    [| 0.5f // A
       0.5f
       0.0f
       0.5f // B
       -0.5f
       0.0f
       -0.5f // C
       -0.5f
       0.0f
       -0.5f // D
       0.5f
       0.0f |]

let private indices = [| 0; 1; 3; 1; 2; 3 |]

let sw = Stopwatch.StartNew ()

let quadSketch: Sketch<QuadState> =
    { OnInit =
        fun gl ->
            // create vertex array object
            let vao =
                let vao = gl.glDo gl.GenVertexArray
                gl.glDo <| fun () -> gl.BindVertexArray(vao)
                vao

            // init vertex buffer, and element array buffer, and shader program
            let vbo = BufferObject (gl, vertices, BufferTargetARB.ArrayBuffer)
            let ebo = BufferObject (gl, indices, BufferTargetARB.ElementArrayBuffer)
            let shader = ShaderProgram (gl, "QuadSketch/vert.vert", "QuadSketch/frag.frag")
            
            gl.glDo <| fun () ->
                gl.VertexAttribPointer(
                    0u, // location
                    3, // num vertex components (must be 1,2,3,4)
                    VertexAttribPointerType.Float, // type of each component
                    false, // don't normalize (we are already using floats)
                    3u * uint sizeof<float32>, // stride (size of each vertex in bytes)
                    IntPtr.Zero.ToPointer() // 0, offset pointer to first vertex in the buffer
                )
            gl.glDo <| fun () -> gl.EnableVertexAttribArray(0u)
                
            { Ebo = ebo
              Vbo = vbo
              Vao = vao
              Shader = shader
              uColorG = 0f }
      OnClose =
        fun gl state ->
            state.Vbo.delete()
            state.Ebo.delete()
            gl.glDo <| fun () -> gl.DeleteBuffer state.Vao
            gl.glDo <| fun () -> gl.DeleteVertexArray state.Vao
            state.Shader.delete ()
      OnUpdate =
        fun _ prev ->
            // update green channel
            let g =
                sw.ElapsedMilliseconds |> float
                |> fun v -> Math.Sin (v / 100.0) |> float32 // -1 to 1
                |> fun v -> (v + 1f) / 2f  // 0 to 1
                
            {prev with uColorG = g}
      OnRender =
        fun gl state ->
            gl.glDo <| fun () -> gl.BindVertexArray state.Vao
            state.Shader.useProgram ()
            
            // set uColor uniform
            state.Shader.setUniform4 "u_Color" <| Vector4(1.0f, state.uColorG, 0.0f, 0.0f)

            gl.glDo <| fun () ->
                gl.DrawElements(
                    PrimitiveType.Triangles,
                    uint indices.Length,
                    DrawElementsType.UnsignedInt,
                    IntPtr.Zero.ToPointer() // 0, offset pointer to first index
                )}