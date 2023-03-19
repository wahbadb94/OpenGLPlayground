module Graphics.ShaderHelpers

open System
open System.IO
open Silk.NET.OpenGL

open Graphics.ResultBuilder
open Graphics.GLExtensions

type ShaderHelpers () =
    static member deleteProgram ( gl: GL ) handle =
        gl.glDo <| fun () -> gl.DeleteShader handle
    
    static member buildProgram gl vertPath fragPath = ResultBuilder () {
        // compile vert and frag
        let! vert = vertPath |> ShaderHelpers.loadShaderFile gl ShaderType.VertexShader
        let! frag = fragPath |> ShaderHelpers.loadShaderFile gl ShaderType.FragmentShader
        
        // make and link program
        let program = gl.glDo <| gl.CreateProgram
        gl.glDo <| fun () -> gl.AttachShader (program, vert)
        gl.glDo <| fun () -> gl.AttachShader (program, frag)
        gl.glDo <| fun () -> gl.LinkProgram program
        
        // don't need vert and frag after we've built the program
        gl.glDo <| fun () -> gl.DetachShader(program, vert)
        gl.glDo <| fun () -> gl.DetachShader(program, frag)
        gl.glDo <| fun () -> gl.DeleteShader vert
        gl.glDo <| fun () -> gl.DeleteShader frag
        
        // check for link errors
        let mutable status = 0
        gl.glDo <| fun () -> gl.GetProgram(program, GLEnum.LinkStatus, &status)
        
        return!
            if status = 0 then
                gl.glDo <| fun () -> gl.DeleteProgram program
                Error $"Error linking shader {gl.GetProgramInfoLog program}"
            else
                Ok program
    }
    
    static member private loadShaderFile (gl: GL) (shaderType: ShaderType) path =
        let source = File.ReadAllText path
        let handle = gl.glDo <| fun () -> gl.CreateShader shaderType
        
        gl.glDo <| fun () -> gl.ShaderSource (handle, source)
        gl.glDo <| fun () -> gl.CompileShader handle

        let infoLog = gl.glDo <| fun () -> gl.GetShaderInfoLog handle
        if not (String.IsNullOrWhiteSpace infoLog) then
            let friendlyName =
                match shaderType with
                | ShaderType.ComputeShader -> "Compute"
                | ShaderType.FragmentShader -> "Fragment"
                | ShaderType.GeometryShader -> "Geometry"
                | ShaderType.VertexShader -> "Vertex"
                | ShaderType.TessControlShader -> "Tesselation Control "
                | ShaderType.TessEvaluationShader -> "Tesselation Evaluation"
                | _ -> "Unknown"
            
            printfn $"Error compiling {friendlyName} Shader:"
            printfn $"{infoLog}"
            
            Error infoLog
        else Ok handle
