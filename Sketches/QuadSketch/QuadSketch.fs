module Sketches.QuadSketch

open System
open System.Diagnostics
open System.Numerics
open Silk.NET.OpenGL
open Silk.NET.Input

open Graphics.VertexArrayObject
open Graphics.ShaderProgram
open Graphics.App
open Graphics.BufferObject
open Graphics.GLExtensions
open Graphics.GLSLAttributeAttribute
open Graphics.Texture
open Graphics.Camera
open Graphics.Lighting
open Graphics.Transform

[<Struct>]
[<GLSLAttribute(0u, VertexAttributeType.Vec3)>]
[<GLSLAttribute(1u, VertexAttributeType.Vec3)>]
type Vertex = { position: Vector3; normal: Vector3; }

[<Struct>]
[<GLSLAttribute(2u, VertexAttributeType.Vec3)>]
type InstanceOffset = { x: float32; y: float32; z: float32 }
    
type QuadState = {
      Vbo: BufferObject<Vertex>
      Ibo: BufferObject<InstanceOffset>
      Ebo: BufferObject<int>
      Vao: VertexArrayObject
      Shader: ShaderProgram
      Texture: Texture
      uColor: float32
      ProjectionMatrix: Matrix4x4
      Camera: Camera
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
    [|
        for v in 0..(vertexRaw.Length / 6)-1 do
            let i = 6 * v
            {
                position = Vector3(vertexRaw[i], vertexRaw[i+1], vertexRaw[i+2])
                normal = Vector3(vertexRaw[i+3], vertexRaw[i+4], vertexRaw[i+5])
            }
    |]

let private indices = [| 0; 1; 2; 1; 2; 3 |]

let private instance_dim = 50 // 10x10 instances
let private num_instances = (instance_dim + 1) * (instance_dim + 1) * (2 * instance_dim + 1)

let private instanceOffsets =
    [|
        for y in (-instance_dim/2)..(instance_dim/2) do
            for x in (-instance_dim/2)..(instance_dim/2) do
                for z in -(2 * instance_dim+1)..0 do
                    { x = float32 x; y = float32 y; z = float32 z }
    |]

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
            let ibo = BufferObject (gl, instanceOffsets, BufferTargetARB.ArrayBuffer)
            let shader = ShaderProgram (gl, "QuadSketch/vert.vert", "QuadSketch/frag.frag") // create shader program
            let texture = Texture (gl, "QuadSketch/FSharpLogo.png")
            let projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(float32 Math.PI / 4f, float32 size.X / float32 size.Y, 0.1f, 100f)
            let camera = Camera ()
            
            vao.enableVertexAttributes<Vertex> vbo // tell openGL how to interpret vertex data
            vao.enableVertexAttributes<InstanceOffset> ibo // tell openGL how to interpret instance data
            gl.glDo <| fun () -> gl.VertexAttribDivisor(2u, 1u)
            
            let state =
                { Vbo = vbo
                  Ibo = ibo
                  Ebo = ebo
                  Vao = vao
                  Shader = shader
                  Texture = texture
                  uColor = 0f
                  ProjectionMatrix = projectionMatrix
                  Camera = camera
                  Transform = {
                      Scale = 0.15f
                      Translation = Vector3.Zero
                      Rotation = Vector3(0f, 0f, 0f)
                  }
                  Lighting = {
                      ambient = Vector3(0.2f, 0.2f, 0.2f)
                      diffuse = {
                          position = Vector3(0f, 0f, 2f)
                          color = Vector3(1f, 1f, 1f)
                      }
                  } }
            
            match shader.ErrorMsg with
            | Some e ->
                delete state // clean up resources we've created
                Error e
            | None -> Ok state
      onKeyDown = fun _keyboard _key _num -> ()
      OnResize = fun size prev ->
          { prev with
                ProjectionMatrix = 
                    Matrix4x4.CreatePerspectiveFieldOfView(
                        float32 Math.PI / 4f,
                        float32 size.X / float32 size.Y,
                        0.1f, 100f) }
      OnUpdate =
        fun _gl _keyboard prev ->
            if _keyboard.IsKeyPressed Key.I then
                prev.Camera.moveForward ()
            if _keyboard.IsKeyPressed Key.K then
                prev.Camera.moveBackward ()
            if _keyboard.IsKeyPressed Key.J then
                prev.Camera.moveLeft ()
            if _keyboard.IsKeyPressed Key.L then
                prev.Camera.moveRight ()
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
            state.Shader.setUniform ("u_view", state.Camera.viewMatrix)
            state.Shader.setUniform ("u_projection", state.ProjectionMatrix)
            match state.Transform.NormalMatrix with
            | Some m -> state.Shader.setUniform("u_normalMatrix", m)
            | None -> printfn "[Playground Warning]: 'u_normalMatrix' was not set b/c does not exist."
            
            state.Shader.setUniform("u_lightAmbient", state.Lighting.ambient)
            state.Shader.setUniform("u_lightDiffusePos", state.Lighting.diffuse.position)
            state.Shader.setUniform("u_lightDiffuseColor", state.Lighting.diffuse.color)
            state.Shader.setUniform("u_cameraPos", state.Camera.position)
            state.Shader.setUniform("u_fogNear", 0f);
            state.Shader.setUniform("u_fogFar", float32 (2 * instance_dim + 1));
            
            state.Texture.bind TextureUnit.Texture0

            // actual draw call
            gl.glDo <| fun () ->
                gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, uint32 vertices.Length, uint32 num_instances)
                
            state.Texture.unbind()
      OnClose = fun _ -> delete }