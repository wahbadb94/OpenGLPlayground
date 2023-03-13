module Graphics.Camera
open System.Numerics

type Camera () =
    // private state
    let mutable _position = Vector3(0f, 0f, 3f)
    let velocity = 0.01f
    
    // private readonly calculated
    member private this._target = _position - Vector3(0f, 0f, 3f) // look 3 units ahead
    member private this._direction = Vector3.Normalize (this._target - _position)
    member private this._right = Vector3.Cross(Vector3.UnitY, this._direction) |> Vector3.Normalize
    member private this._up = Vector3.Cross(this._direction, this._right) |> Vector3.Normalize
    
    // public readonly calculated
    member this.viewMatrix = Matrix4x4.CreateLookAt(_position, this._target, this._up)
    member this.position = _position;
    
    // methods
    member this.moveForward () =
        _position <- _position - velocity * Vector3.UnitZ // forward is actually the -z direction
    member this.moveBackward () =
        _position <- _position + velocity * Vector3.UnitZ // forward is actually the -z direction
    member this.moveLeft () =
        _position <- _position - velocity * Vector3.UnitX
    member this.moveRight () =
        _position <- _position + velocity * Vector3.UnitX
