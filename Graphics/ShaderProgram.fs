module Graphics.ShaderProgram

open System
open System.IO
open System.Numerics
open Silk.NET.OpenGL
open GLExtensions

/// Shader Program instance. Instantiating does the following:
/// 1. load and compile vert and frag shader source
/// 2. create program from vert and frag
/// 3. clean up any unneeded GPU memory
type ShaderProgram (gl: GL, vertPath: string, fragPath: string) =
    let mutable errorMsg = None
    
    let loadShaderFile ( shaderType: ShaderType ) path =
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
            
            errorMsg <- Some infoLog

        handle
    
    // load shader source code
    let vert = vertPath |> loadShaderFile ShaderType.VertexShader
    let frag = fragPath |> loadShaderFile ShaderType.FragmentShader
    
    // create program
    let program =
        let program = gl.glDo <| gl.CreateProgram
        gl.glDo <| fun () -> gl.AttachShader (program, vert)
        gl.glDo <| fun () -> gl.AttachShader (program, frag)
        gl.glDo <| fun () -> gl.LinkProgram program
        
        // check for errors
        let mutable status = 0
        gl.glDo <| fun () -> gl.GetProgram(program, GLEnum.LinkStatus, &status)
        if status = 0 then
            do printfn $"Error linking shader {gl.GetProgramInfoLog program}"
        
        program
    
    
    // don't need vert and frag after we've built the program
    do
        gl.glDo <| fun () -> gl.DetachShader(program, vert)
        gl.glDo <| fun () -> gl.DetachShader(program, frag)
        gl.glDo <| fun () -> gl.DeleteShader vert
        gl.glDo <| fun () -> gl.DeleteShader frag
    
    let uniformNotFoundReason =
        @"Possible causes include:
            1. Misspelled uniform name.
            2. Uniform unused in shader, and was stripped during shader compilation.
        "
    
    member this.ErrorMsg = errorMsg
    
    /// calls glUseProgram
    member this.useProgram () = gl.UseProgram program
    
    member private this.getUniformLocation (name: string) =
        let location = gl.glDo <| fun () -> gl.GetUniformLocation (program, name)
        if location = -1 then
            printfn $"Could not find uniform location for {name}"
            printfn $"{uniformNotFoundReason}"
            
        location
    
    member this.setUniform1i name (value: int) =
        let location = this.getUniformLocation name
        gl.glDo <| fun () -> gl.Uniform1 (location, value)
    
    member this.setUniform4 (name: string) (data: Vector4) =
        let location = this.getUniformLocation name
        gl.glDo <| fun () -> gl.Uniform4 (location, data)
        
    member this.delete () = gl.glDo <| fun () -> gl.DeleteProgram program