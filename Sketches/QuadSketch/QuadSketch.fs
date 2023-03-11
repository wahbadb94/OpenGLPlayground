module Sketches.QuadSketch

open System
open System.Diagnostics
open System.Numerics

open Graphics.Lighting
open Graphics.Transform
open Silk.NET.Maths
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
type Vertex = { position: Vector3; normal: Vector3; }
    
type QuadState = {
      Vbo: BufferObject<Vertex>
      Ebo: BufferObject<int>
      Vao: VertexArrayObject
      Shader: ShaderProgram
      Texture: Texture
      uColor: float32
      ProjectionMatrix: Matrix4x4
      ViewMatrix: Matrix4x4
      Transform: Transform
      Lighting: Lighting }

let private vertexRaw =
    [|
            -0.5f; -0.5f; -0.5f;  0.0f;  0.0f; -1.0f;
             0.5f; -0.5f; -0.5f;  0.0f;  0.0f; -1.0f; 
             0.5f;  0.5f; -0.5f;  0.0f;  0.0f; -1.0f; 
             0.5f;  0.5f; -0.5f;  0.0f;  0.0f; -1.0f; 
            -0.5f;  0.5f; -0.5f;  0.0f;  0.0f; -1.0f; 
            -0.5f; -0.5f; -0.5f;  0.0f;  0.0f; -1.0f; 
        
            -0.5f; -0.5f;  0.5f;  0.0f;  0.0f; 1.0f;
             0.5f; -0.5f;  0.5f;  0.0f;  0.0f; 1.0f;
             0.5f;  0.5f;  0.5f;  0.0f;  0.0f; 1.0f;
             0.5f;  0.5f;  0.5f;  0.0f;  0.0f; 1.0f;
            -0.5f;  0.5f;  0.5f;  0.0f;  0.0f; 1.0f;
            -0.5f; -0.5f;  0.5f;  0.0f;  0.0f; 1.0f;
        
            -0.5f;  0.5f;  0.5f; -1.0f;  0.0f;  0.0f;
            -0.5f;  0.5f; -0.5f; -1.0f;  0.0f;  0.0f;
            -0.5f; -0.5f; -0.5f; -1.0f;  0.0f;  0.0f;
            -0.5f; -0.5f; -0.5f; -1.0f;  0.0f;  0.0f;
            -0.5f; -0.5f;  0.5f; -1.0f;  0.0f;  0.0f;
            -0.5f;  0.5f;  0.5f; -1.0f;  0.0f;  0.0f;
        
             0.5f;  0.5f;  0.5f;  1.0f;  0.0f;  0.0f;
             0.5f;  0.5f; -0.5f;  1.0f;  0.0f;  0.0f;
             0.5f; -0.5f; -0.5f;  1.0f;  0.0f;  0.0f;
             0.5f; -0.5f; -0.5f;  1.0f;  0.0f;  0.0f;
             0.5f; -0.5f;  0.5f;  1.0f;  0.0f;  0.0f;
             0.5f;  0.5f;  0.5f;  1.0f;  0.0f;  0.0f;
        
            -0.5f; -0.5f; -0.5f;  0.0f; -1.0f;  0.0f;
             0.5f; -0.5f; -0.5f;  0.0f; -1.0f;  0.0f;
             0.5f; -0.5f;  0.5f;  0.0f; -1.0f;  0.0f;
             0.5f; -0.5f;  0.5f;  0.0f; -1.0f;  0.0f;
            -0.5f; -0.5f;  0.5f;  0.0f; -1.0f;  0.0f;
            -0.5f; -0.5f; -0.5f;  0.0f; -1.0f;  0.0f;
        
            -0.5f;  0.5f; -0.5f;  0.0f;  1.0f;  0.0f;
             0.5f;  0.5f; -0.5f;  0.0f;  1.0f;  0.0f;
             0.5f;  0.5f;  0.5f;  0.0f;  1.0f;  0.0f;
             0.5f;  0.5f;  0.5f;  0.0f;  1.0f;  0.0f;
            -0.5f;  0.5f;  0.5f;  0.0f;  1.0f;  0.0f;
            -0.5f;  0.5f; -0.5f;  0.0f;  1.0f;  0.0f
    |]

let private vertices =
    [ for v in 0..(vertexRaw.Length / 6)-1 do
          let i = 6 * v
          {
              position = Vector3(vertexRaw[i], vertexRaw[i+1], vertexRaw[i+2])
              normal = Vector3(vertexRaw[i+3], vertexRaw[i+4], vertexRaw[i+5])
          }] |> List.toArray

let private indices = [| 0; 1; 2; 1; 2; 3 |]

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
            let projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(float32 Math.PI / 4f, float32 size.X / float32 size.Y, 0.1f, 100f)
            let viewMatrix =
                let cameraPos = Vector3(0f, 0f, 3f)
                let cameraTarget = Vector3.Zero
                let cameraDirection = Vector3.Normalize <| cameraTarget - cameraPos
                let cameraRight = Vector3.Normalize <| Vector3.Cross(Vector3.UnitY, cameraDirection)
                let cameraUp = Vector3.Normalize <| Vector3.Cross(cameraDirection, cameraRight)
                Matrix4x4.CreateLookAt(cameraPos, cameraTarget, cameraUp)
            
            vao.enableVertexAttributes<Vertex> () // tell openGL how to interpret vertex data
            
            let state =
                { Vbo = vbo
                  Ebo = ebo
                  Vao = vao
                  Shader = shader
                  Texture = texture
                  uColor = 0f
                  ProjectionMatrix = projectionMatrix
                  ViewMatrix = viewMatrix
                  Transform = {
                      Scale = 1f
                      Translation = Vector3.Zero
                      Rotation = Vector3(0f, 0f, 0f)
                  }
                  Lighting = {
                      ambient = Vector3(0.2f, 0.2f, 0.2f)
                      diffuse = {
                          position = Vector3(0f, 0f, 10f)
                          color = Vector3(1f, 1f, 1f)
                      }
                  } }
            
            match shader.ErrorMsg with
            | Some e ->
                delete state // clean up resources we've created
                Error e
            | None -> Ok state
      OnResize = fun size prev ->
          { prev with
                ProjectionMatrix = 
                    Matrix4x4.CreatePerspectiveFieldOfView(
                        float32 Math.PI / 4f,
                        float32 size.X / float32 size.Y,
                        0.1f, 100f) }
      OnUpdate =
        fun _ prev ->
            { prev with
                Transform = {
                    prev.Transform with
                        Rotation = prev.Transform.Rotation + Vector3(0f, 0.003f, 0.009f)
                }}
      OnRender =
        fun gl state ->
            gl.glDo <| fun () -> gl.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)
            gl.glDo <| fun () -> gl.Enable(EnableCap.DepthTest)
            
            state.Vao.bind () // not strictly necessary since we only have one vao
            state.Shader.useProgram ()
            state.Shader.setUniform ("u_model", state.Transform.AsModelMatrix)
            state.Shader.setUniform ("u_view", state.ViewMatrix)
            state.Shader.setUniform ("u_projection", state.ProjectionMatrix)
            match state.Transform.NormalMatrix with
            | Some m -> state.Shader.setUniform("u_normalMatrix", m)
            | None -> printfn "[Playground Warning]: 'u_normalMatrix' was not set b/c does not exist."
            
            state.Shader.setUniform("u_lightAmbient", state.Lighting.ambient)
            state.Shader.setUniform("u_lightDiffusePos", state.Lighting.diffuse.position)
            state.Shader.setUniform("u_lightDiffuseColor", state.Lighting.diffuse.color)
            
            state.Texture.bind TextureUnit.Texture0

            // actual draw call
            gl.glDo <| fun () ->
                gl.DrawArrays(PrimitiveType.Triangles, 0, uint32 vertices.Length)
                
            state.Texture.unbind()
      OnClose = fun _ -> delete }