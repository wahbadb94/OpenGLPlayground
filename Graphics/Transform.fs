module Graphics.Transform

open System.Numerics

type Rotation =
    | Matrix of Matrix4x4
    | Quaternion of Quaternion

[<Struct>]
type Transform =
    {
        Position: Vector3
        Scale: float32
        Rotation: Rotation
    } with
    member this.ViewMatrix =
        let rotation =
            match this.Rotation with
            | Matrix matrix -> matrix
            | Quaternion quaternion -> Matrix4x4.CreateFromQuaternion quaternion
            
        Matrix4x4.Identity
        * rotation
        * Matrix4x4.CreateScale this.Scale
        * Matrix4x4.CreateTranslation this.Position