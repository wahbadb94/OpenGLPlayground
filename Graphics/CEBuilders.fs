module Graphics.CEBuilders

type ResultBuilder () =
    member this.Return x = Ok x
    member this.Bind (m, f) = Result.bind f m
    member this.ReturnFrom x = x

type OptionBuilder () =
    member this.Return x = Some x
    member this.Bind (m, f) = Option.bind f m
    member this.ReturnFrom x = x
    member this.Zero () = None
    member this.Combine (a, b) =
        match a with
        | Some _ -> a
        | None -> b
    member this.Delay f = f ()