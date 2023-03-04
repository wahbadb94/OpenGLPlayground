module Graphics.GLExtensions

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Silk.NET.OpenGL

type GL with
    member this.glDo
        (
            f,
            [<CallerFilePath; Optional; DefaultParameterValue("")>] path: string,
            [<CallerLineNumber; Optional; DefaultParameterValue(0)>] linNum: int
        ) =
        this.clearErrors ()
        let retVal = f ()
        this.checkError path linNum

        retVal

    member private this.clearErrors() =
        let mutable error = this.GetError()

        while not (error = GLEnum.NoError) do
            error <- this.GetError()

    member private this.checkError path lineNum =
        let error = this.GetError()

        let errorStr =
            match error with
            | GLEnum.NoError -> ""
            | GLEnum.InvalidEnum -> "Invalid Enum"
            | GLEnum.InvalidValue -> "Invalid Value"
            | GLEnum.InvalidOperation -> "Invalid Operation"
            | GLEnum.StackOverflow -> "Stack Overflow"
            | GLEnum.StackUnderflow -> "Stack Underflow"
            | GLEnum.OutOfMemory -> "Out of Memory"
            | GLEnum.InvalidFramebufferOperation -> "Invalid Framebuffer Operation"
            | GLEnum.ContextLost -> "Context Lost"
            | _ -> $"Unknown Error Code {error}"

        if not (errorStr = "") then
            printfn $"[GLError] {errorStr} - {path}{lineNum}"
