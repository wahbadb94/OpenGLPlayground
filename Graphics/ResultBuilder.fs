module Graphics.ResultBuilder

type ResultBuilder () =
    member this.Return x = Ok x
    member this.Bind (m, f) = Result.bind f m
    member this.ReturnFrom x = x
