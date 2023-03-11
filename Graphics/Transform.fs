module Graphics.Transform

open System.Numerics

[<Struct>]
type Transform =
    {
        Translation: Vector3
        Scale: float32
        Rotation: Vector3
    } with
    member this.AsModelMatrix =
        Matrix4x4.Identity
        * Matrix4x4.CreateFromYawPitchRoll(this.Rotation.Y, this.Rotation.X, this.Rotation.Z)
        * Matrix4x4.CreateScale this.Scale
        * Matrix4x4.CreateTranslation this.Translation
    member this.NormalMatrix =
        let mutable inverse = Matrix4x4.Identity;
        let inverseExists = Matrix4x4.Invert(this.AsModelMatrix, &inverse)
        if inverseExists then
            Some <| Matrix4x4.Transpose inverse
        else
            None