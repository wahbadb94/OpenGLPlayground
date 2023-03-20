module Graphics.ShaderHelpers

open System
open System.IO
open FSharpPlus
open Silk.NET.OpenGL

open Graphics.CEBuilders
open Graphics.GLExtensions

let private UniformIdentifier = "uniform"
let private LocationStart = "(location="

[<Struct>]
type UniformLocation = {
    location: int
    name: string
}

type BuildProgramReturn = uint32 * Map<string, UniformLocation>

type ShaderHelpers () =
    static member deleteProgram ( gl: GL ) handle =
        gl.glDo <| fun () -> gl.DeleteProgram handle
    
    static member buildProgram gl vertPath fragPath = ResultBuilder () {
        // compile vert and frag
        let! vert, vUniforms = vertPath |> ShaderHelpers.loadShaderFile gl ShaderType.VertexShader
        let! frag, fUniforms = fragPath |> ShaderHelpers.loadShaderFile gl ShaderType.FragmentShader
        
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
                ShaderHelpers.deleteProgram gl program
                Error $"Error linking shader {gl.GetProgramInfoLog program}"
            else
                Ok (program, Map.union vUniforms fUniforms )
    }
    
    static member private loadShaderFile (gl: GL) (shaderType: ShaderType) path =
        let source, uniformMap = ShaderHelpers.parseSource path
            
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
        else Ok (handle, uniformMap)
    
    static member private parseSource path =
         let source, uniforms =
            (("", [||]), File.ReadLines path |> Seq.toArray) ||> Array.fold (fun acc line ->
            let sourceAcc, uniformAcc = acc
            sourceAcc + line + "\n", ShaderHelpers.extractUniforms line |> Array.append uniformAcc )
         
         let uniformMap =
            ( Map.empty , uniforms )
            ||> Array.fold (fun acc uniform -> acc |> Map.add uniform.name uniform)
         
         source, uniformMap
         
    static member private extractUniforms line =
        line.Split(';') |> Array.collect (fun statement ->
            match ShaderHelpers.extractUniformLocation statement with
            | Some uniform -> [| uniform |]
            | None -> [||] )
    
    /// expects a single GLSL statement (without the terminating ';') and
    /// returns an optional GLSL uniform's name and location.
    static member private extractUniformLocation statement =
        let noSpaces = statement.Replace(" ", "")
        (statement.Contains(UniformIdentifier), ()) |> Option.ofPair 
        |> Option.bind(fun () ->
            match noSpaces.IndexOf(LocationStart) with
            | iStart when iStart > -1 -> Some iStart
            | _ -> None )
        |> Option.bind(fun iStart ->
            let locationSlice = noSpaces.Substring(iStart) |> String.takeWhile (fun c -> c <> ')')
            // "(location=0)" -> "0"
            // "(location=12)" -> "12"
            // etc.
            let eqIndex = locationSlice.IndexOf("=")
            if eqIndex < 0 then
                None
            else
                let numStr = locationSlice.Substring(eqIndex + 1)
                try
                    numStr |> int |> Some
                with :? FormatException -> None )
        |> Option.map (fun location -> { location = location; name = statement.Split(" ") |> Array.last } ) 