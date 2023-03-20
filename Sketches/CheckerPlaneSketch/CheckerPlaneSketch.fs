module Sketches.CheckerPlane

open System
open System.Diagnostics
open System.Numerics
open Graphics.CEBuilders
open Silk.NET.OpenGL
open Silk.NET.Input

open Graphics.VertexArrayObject
open Graphics.ShaderProgram
open Graphics.App
open Graphics.BufferObject
open Graphics.GLExtensions
open Graphics.GLSLAttributeAttribute
open Graphics.Camera
open Graphics.Lighting
open Graphics.Transform

[<Struct>]
[<GLSLAttribute(0u, VertexAttributeType.Vec3)>]
[<GLSLAttribute(1u, VertexAttributeType.Vec3)>]
[<GLSLAttribute(2u, VertexAttributeType.Vec2)>]
type Vertex = { position: Vector3; normal: Vector3; uv: Vector2 }

// TODO: remove hardcoded paths (FSharp TypeProviders?)
let private vertPath = "/home/dink/Projects/FSharp/OpenGLPlayground/Sketches/CheckerPlaneSketch/checkered.vert"
let private fragPath = "/home/dink/Projects/FSharp/OpenGLPlayground/Sketches/CheckerPlaneSketch/checkered.frag"

type CheckeredState = {
      Vbo: BufferObject<Vertex>
      Ebo: BufferObject<int>
      Vao: VertexArrayObject
      Shader: ShaderProgram
      ProjectionMatrix: Matrix4x4
      Camera: Camera
      Transform: Transform
      Lighting: Lighting }

let private vertices = [|
    {position = Vector3( 1f,  1f, 0f); normal = Vector3.UnitZ; uv = Vector2(1f, 1f)}
    {position = Vector3( 1f, -1f, 0f); normal = Vector3.UnitZ; uv = Vector2(1f, 0f)}
    {position = Vector3(-1f, -1f, 0f); normal = Vector3.UnitZ; uv = Vector2(0f, 0f)}
    {position = Vector3(-1f,  1f, 0f); normal = Vector3.UnitZ; uv = Vector2(0f, 1f)}
|]

let private indices = [| 0; 1; 3; 1; 2; 3 |]

let sw = Stopwatch.StartNew ()

let checkeredSketch: Sketch<CheckeredState> =
    { OnInit =
        fun gl size ->
            let vao = VertexArrayObject gl // create the vertex array object
            let vbo = BufferObject (gl, vertices, BufferTargetARB.ArrayBuffer) // buffer for vertex data
            let ebo = BufferObject (gl, indices, BufferTargetARB.ElementArrayBuffer) // buffer for index data
            
            let shader = ResultBuilder () {
                 let! handle = Graphics.ShaderHelpers.ShaderHelpers.buildProgram gl vertPath fragPath 
                 return ShaderProgram(gl, handle, vertPath, fragPath)
            }
            let projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(float32 Math.PI / 4f, float32 size.X / float32 size.Y, 0.1f, 100f)
            let camera = Camera ()
            
            vao.enableVertexAttributes<Vertex> vbo // tell openGL how to interpret vertex data
            
            // set up watchers for shader hot reload
            
            shader |> Result.map (fun shader ->
                { Vbo = vbo
                  Ebo = ebo
                  Vao = vao
                  Shader = shader
                  ProjectionMatrix = projectionMatrix
                  Camera = camera
                  Transform = {
                      Scale = 10f
                      Translation = Vector3.Zero
                      Rotation = Vector3(float32 Math.PI / -4f, 0f, 0f)
                  }
                  Lighting = {
                      ambient = Vector3(0.2f, 0.2f, 0.2f)
                      diffuse = {
                          position = Vector3(0f, 0f, 2f)
                          color = Vector3(1f, 1f, 1f)
                      }
                  }} )
      onKeyDown = fun _keyboard _key _num -> ()
      OnResize = fun size prev ->
          { prev with
                ProjectionMatrix = 
                    Matrix4x4.CreatePerspectiveFieldOfView(
                        float32 Math.PI / 4f,
                        float32 size.X / float32 size.Y,
                        0.1f, 100f) }
      OnUpdate =
        fun gl _keyboard prev ->
            if _keyboard.IsKeyPressed Key.I then
                prev.Camera.moveForward ()
            if _keyboard.IsKeyPressed Key.K then
                prev.Camera.moveBackward ()
            if _keyboard.IsKeyPressed Key.J then
                prev.Camera.moveLeft ()
            if _keyboard.IsKeyPressed Key.L then
                prev.Camera.moveRight ()
            
            // hot reload of shader
            prev.Shader.update gl 
            
            prev
      OnRender =
        fun gl state ->
            gl.glDo <| fun () -> gl.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)
            gl.glDo <| fun () -> gl.Enable(EnableCap.DepthTest)
            
            state.Vao.bind ()
            state.Ebo.bind ()
            
            state.Shader.useProgram ()
            state.Shader.setUniform ("u_model", state.Transform.AsModelMatrix)
            state.Shader.setUniform ("u_view", state.Camera.viewMatrix)
            state.Shader.setUniform ("u_projection", state.ProjectionMatrix)

            gl.glDo <| fun () ->
                gl.DrawElements(PrimitiveType.Triangles, uint indices.Length, DrawElementsType.UnsignedInt, IntPtr.Zero.ToPointer())
                
      OnClose = fun _ state ->
          state.Shader.delete ()
          state.Vao.delete ()
          state.Vbo.delete ()
          state.Ebo.delete () }
