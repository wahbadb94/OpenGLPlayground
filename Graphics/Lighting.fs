module Graphics.Lighting

type Diffuse = {
    color: System.Numerics.Vector3
    position: System.Numerics.Vector3
}

type Lighting = {
    ambient: System.Numerics.Vector3
    diffuse: Diffuse
}
