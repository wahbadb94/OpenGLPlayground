module Sketches.QuadSketch

open System
open System.Diagnostics
open System.Numerics
open Graphics.ShaderProgram
open Graphics.VertexArrayObject
open Silk.NET.OpenGL

open Graphics.App
open Graphics.BufferObject
open Graphics.GLExtensions

[<Struct>]
type Vertex =
    { position: Vector3 
      color: Vector3 }
    
type QuadState =
    {
      Vbo: BufferObject<Vertex>
      Ebo: BufferObject<int>
      Vao: VertexArrayObject
      Shader: ShaderProgram
      uColorG: float32 }

let private vertices =
    [|
        { position = Vector3(0.5f, 0.5f, 0.0f); color = Vector3(1.0f, 0.0f, 0.0f) } 
        { position = Vector3(0.5f, -0.5f, 0.0f); color = Vector3(0.0f, 1.0f, 0.0f) } 
        { position = Vector3(-0.5f, -0.5f, 0.0f); color = Vector3(0.0f, 0.0f, 1.0f) } 
        { position = Vector3(-0.5f, 0.5f, 0.0f); color = Vector3(1.0f, 0.0f, 1.0f) } 
    |]
    
let private indices = [| 0; 1; 3; 1; 2; 3 |]

let sw = Stopwatch.StartNew ()

let quadSketch: Sketch<QuadState> =
    { OnInit =
        fun gl ->
            // create vertex array object
            let vao = VertexArrayObject gl

            // init vertex buffer, element array buffer, and shader program
            let vbo = BufferObject (gl, vertices, BufferTargetARB.ArrayBuffer)
            let ebo = BufferObject (gl, indices, BufferTargetARB.ElementArrayBuffer)
            let shader = ShaderProgram (gl, "QuadSketch/vert.vert", "QuadSketch/frag.frag")
            
            // tell opengl the layout of the vertices we just added
            vao.addVertexAttribute<Vertex> VertexAttributeType.Vec3 VertexAttribPointerType.Float // attribute (position) vec3<float32>
            vao.addVertexAttribute<Vertex> VertexAttributeType.Vec3 VertexAttribPointerType.Float // attribute (color) vec3<float32>
                
            { Ebo = ebo
              Vbo = vbo
              Vao = vao
              Shader = shader
              uColorG = 0f }
      OnUpdate =
        fun _ prev ->
            // update green channel
            let g =
                sw.ElapsedMilliseconds |> float
                |> fun v -> 1.0 + Math.Sin (v / 100.0) / 2.0 // osc range [0.0, 1.0]
                |> float32 
                
            { prev with uColorG = g }
      OnRender =
        fun gl state ->
            state.Vao.bind () // not strictly necessary since we only have one vao
            
            state.Shader.useProgram ()
            state.Shader.setUniform4 "u_Color" <| Vector4(1.0f, state.uColorG, 0.0f, 0.0f)

            // actual draw call
            gl.glDo <| fun () ->
                gl.DrawElements(
                    PrimitiveType.Triangles,
                    uint indices.Length,
                    DrawElementsType.UnsignedInt,
                    IntPtr.Zero.ToPointer() // 0, offset pointer to first index
                )
      OnClose =
        fun _ state ->
            state.Vbo.delete ()
            state.Ebo.delete ()
            state.Vao.delete ()
            state.Shader.delete ()}