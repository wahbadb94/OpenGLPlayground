module OpenGLPlayground.QuadSketch

open System
open OpenGLPlayground.App
open OpenGLPlayground.BufferObject
open OpenGLPlayground.GLExtensions
open Silk.NET.OpenGL
open FSharp.NativeInterop

type QuadState =
    {
      Vbo: BufferObject<float32>
      Ebo: BufferObject<int>
      Vao: uint
      Shader: uint }

let private vertSource =
    @"
    #version 330 core //Using version GLSL version 3.3
    layout (location = 0) in vec4 vPos;

    void main()
    {
        gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
    }
    "

let private fragSource =
    @"
    #version 330 core
    out vec4 FragColor;
    void main()
    {
        FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
    }
    "

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

let quadSketch: Sketch<QuadState> =
    { OnInit =
        fun gl ->
            // create vertex array object
            let vao =
                let vao = gl.glDo gl.GenVertexArray
                gl.glDo <| fun () -> gl.BindVertexArray(vao)
                vao

            // init vertex buffer, and element array buffer
            let vbo = BufferObject (gl, vertices, BufferTargetARB.ArrayBuffer)
            let ebo = BufferObject (gl, indices, BufferTargetARB.ElementArrayBuffer)

            // create and compiler vert shader
            let vert =
                let vert = gl.glDo <| fun () -> gl.CreateShader(ShaderType.VertexShader)
                
                gl.glDo <| fun () -> gl.ShaderSource(vert, vertSource)
                gl.glDo <| fun () -> gl.CompileShader(vert)

                let infoLog = gl.glDo <| fun () -> gl.GetShaderInfoLog(vert)

                if not (String.IsNullOrWhiteSpace infoLog) then
                    printfn $"Error compiling vertex shader {infoLog}"

                vert

            // create and compile frag shader
            let frag =
                let frag = gl.glDo <| fun () -> gl.CreateShader ShaderType.FragmentShader

                gl.glDo <| fun () -> gl.ShaderSource(frag, fragSource)
                gl.glDo <| fun () -> gl.CompileShader frag

                let infoLog = gl.glDo <| fun () -> gl.GetShaderInfoLog frag

                if not (String.IsNullOrWhiteSpace infoLog) then
                    printfn $"Error compiling fragment shader {infoLog}"

                frag

            let shader =
                let shader = gl.glDo gl.CreateProgram
                gl.glDo <| fun () -> gl.AttachShader(shader, vert)
                gl.glDo <| fun () -> gl.AttachShader(shader, frag)
                gl.glDo <| fun () -> gl.LinkProgram shader

                let mutable status = 0

                gl.glDo <| fun () -> gl.GetProgram(shader, GLEnum.LinkStatus, &status)

                if status = 0 then
                    do printfn $"Error linking shader {gl.GetProgramInfoLog shader}"

                shader
            
            
            // don't need vert and frag after we've built the program
            gl.glDo <| fun () -> gl.DetachShader(shader, vert)
            gl.glDo <| fun () -> gl.DetachShader(shader, frag)
            gl.glDo <| fun () -> gl.DeleteShader vert
            gl.glDo <| fun () -> gl.DeleteShader frag
            
            gl.glDo <| fun () ->
                gl.VertexAttribPointer(
                    0u,
                    3,
                    VertexAttribPointerType.Float,
                    false,
                    3u * uint sizeof<float32>,
                    NativePtr.nullPtr<unativeint>
                    |> NativePtr.toVoidPtr
                )
            gl.glDo <| fun () -> gl.EnableVertexAttribArray(0u)
                
            { Ebo = ebo
              Vbo = vbo
              Vao = vao
              Shader = shader }
      OnClose =
        fun gl state ->
            state.Vbo.delete()
            state.Ebo.delete()
            gl.glDo <| fun () -> gl.DeleteBuffer state.Vao
            gl.glDo <| fun () -> gl.DeleteVertexArray state.Vao
            gl.glDo <| fun () -> gl.DeleteProgram state.Shader
      OnUpdate =
        fun _ prev -> prev
      OnRender =
        fun gl state ->
            let glDo = gl.glDo
            
            glDo <| fun () -> gl.BindVertexArray state.Vao
            glDo <| fun () -> gl.UseProgram state.Shader

            glDo <| fun () ->
                gl.DrawElements(
                    PrimitiveType.Triangles,
                    uint indices.Length,
                    DrawElementsType.UnsignedInt,
                    NativePtr.nullPtr<unativeint>
                    |> NativePtr.toVoidPtr
                )}