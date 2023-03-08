module Sketches.QuadSketch

open System
open System.Diagnostics
open System.Numerics

open Graphics.Transform
open Silk.NET.OpenGL

open Graphics.VertexArrayObject
open Graphics.ShaderProgram
open Graphics.App
open Graphics.BufferObject
open Graphics.GLExtensions
open Graphics.GLSLAttributeAttribute
open Graphics.Texture

[<Struct>]
[<GLSLAttribute(0u, VertexAttributeType.Vec3)>]
[<GLSLAttribute(1u, VertexAttributeType.Vec3)>]
[<GLSLAttribute(2u, VertexAttributeType.Vec2)>]
type Vertex = { position: Vector3; color: Vector3; texCoords: Vector2 }
    
type QuadState = {
      Vbo: BufferObject<Vertex>
      Ebo: BufferObject<int>
      Vao: VertexArrayObject
      Shader: ShaderProgram
      Texture: Texture
      uColor: float32
      ProjectionMatrix: Matrix4x4
      RotationZ: float32
      Scale: float32
      Translation: Vector3 }

let private vertices =
    [|
        { position = Vector3(0.5f, 0.5f, 0.0f); color = Vector3(1.0f, 0.0f, 0.0f); texCoords = Vector2(1f, 1f)}
        { position = Vector3(0.5f, -0.5f, 0.0f); color = Vector3(0.0f, 1.0f, 0.0f); texCoords = Vector2(1f, 0f)} 
        { position = Vector3(-0.5f, -0.5f, 0.0f); color = Vector3(0.0f, 0.0f, 1.0f); texCoords = Vector2(0f, 0f) } 
        { position = Vector3(-0.5f, 0.5f, 0.0f); color = Vector3(1.0f, 0.0f, 1.0f); texCoords = Vector2(0f, 1f) } 
    |]
    
let private indices = [| 0; 1; 3; 1; 2; 3 |]

let sw = Stopwatch.StartNew ()

let private delete state =
    state.Vbo.delete ()
    state.Ebo.delete ()
    state.Vao.delete ()
    state.Shader.delete ()
    state.Texture.delete ()

let quadSketch: Sketch<QuadState> =
    { OnInit =
        fun gl size ->
            let vao = VertexArrayObject gl // create the vertex array object

            let vbo = BufferObject (gl, vertices, BufferTargetARB.ArrayBuffer) // buffer for vertex data
            let ebo = BufferObject (gl, indices, BufferTargetARB.ElementArrayBuffer) // buffer for index data
            let shader = ShaderProgram (gl, "QuadSketch/vert.vert", "QuadSketch/frag.frag") // create shader program
            let texture = Texture (gl, "QuadSketch/FSharpLogo.png")
            let projectionMatrix = Matrix4x4.CreateOrthographic(2.0f, 2f * float32 size.Y / float32 size.X, -4f, 1f)
            
            vao.enableVertexAttributes<Vertex> () // tell openGL how to interpret vertex data
            
            let state =
                { Vbo = vbo
                  Ebo = ebo
                  Vao = vao
                  Shader = shader
                  Texture = texture
                  uColor = 0f
                  ProjectionMatrix = projectionMatrix
                  RotationZ = 0f
                  Scale = 1f
                  Translation = Vector3.Zero }
            
            match shader.ErrorMsg with
            | Some e ->
                delete state // clean up resources we've created
                Error e
            | None -> Ok state
      OnResize = fun size prev ->
          { prev with
                ProjectionMatrix = Matrix4x4.CreateOrthographic(float32 size.X, float32 size.Y, 1f, -1f) }
      OnUpdate =
        fun _ prev ->
            // update green channel
            let g =
                sw.ElapsedMilliseconds |> float
                |> fun v -> 1.0 + Math.Sin (v / 100.0) / 2.0 // osc range [0.0, 1.0]
                |> float32
                
            { prev with
                uColor = g
                RotationZ = prev.RotationZ + 0.005f
                Scale = 1.1f * g }
      OnRender =
        fun gl state ->
            let viewMatrix: Transform =
                {
                    Scale = state.Scale
                    Position = state.Translation
                    Rotation = Matrix <| Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, state.RotationZ)
                } 
            gl.glDo <| fun () -> gl.Clear(ClearBufferMask.ColorBufferBit)
            
            state.Vao.bind () // not strictly necessary since we only have one vao
            state.Shader.useProgram ()
            state.Shader.setUniform ("u_Color",  Vector4(1.0f, state.uColor, 0.0f, 0.0f))
            state.Shader.setUniform ("u_MVP", viewMatrix.ViewMatrix * state.ProjectionMatrix)
            
            state.Texture.bind TextureUnit.Texture0
            state.Shader.setUniform ("u_Texture0", 0)

            // actual draw call
            gl.glDo <| fun () ->
                gl.DrawElements(
                    PrimitiveType.Triangles,
                    uint indices.Length,
                    DrawElementsType.UnsignedInt,
                    IntPtr.Zero.ToPointer() // 0, offset pointer to first index
                )
                
            state.Texture.unbind()
      OnClose = fun _ -> delete }