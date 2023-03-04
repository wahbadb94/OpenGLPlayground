module OpenGLPlayground.Sketches.Sketches.QuadSketch

open System
open System.Diagnostics
open System.Numerics
open Silk.NET.OpenGL
open FSharp.NativeInterop

open OpenGLPlayground.Infrastructure.Graphics.App
open OpenGLPlayground.Infrastructure.Graphics.BufferObject
open OpenGLPlayground.Infrastructure.Graphics.GLExtensions

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
    
    uniform vec4 u_Color;
    
    void main()
    {
        FragColor = u_Color;
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

let sw = Stopwatch.StartNew ()

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
            gl.glDo <| fun () -> gl.BindVertexArray state.Vao
            gl.glDo <| fun () -> gl.UseProgram state.Shader
            
            // set uColor uniform
            let uLocation = gl.glDo <| fun () -> gl.GetUniformLocation(state.Shader, "u_Color")
            if uLocation = -1 then
                printfn "Could not find uniform location for `u_Color`"
                printfn "Possible causes include: 1. Misspelled uniform name, 2. uniform unused in shader, and was stripped during shader compilation."
            let g =
                sw.ElapsedMilliseconds |> float
                |> fun v -> Math.Sin (v / 100.0) |> float32 // -1 to 1
                |> fun v -> (v + 1f) / 2f  // 0 to 1
                
            gl.glDo <| fun () -> gl.Uniform4(uLocation, Vector4(1.0f, g, 0.0f, 0.0f))

            gl.glDo <| fun () ->
                gl.DrawElements(
                    PrimitiveType.Triangles,
                    uint indices.Length,
                    DrawElementsType.UnsignedInt,
                    IntPtr.Zero.ToPointer() // 0, offset pointer to first index
                )}