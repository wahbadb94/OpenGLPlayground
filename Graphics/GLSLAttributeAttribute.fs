module Graphics.GLSLAttributeAttribute

/// represents what GLSL type the vertex attribute should be
type VertexAttributeType =
    | Scalar = 1
    | Vec2 = 2
    | Vec3 = 3
    | Vec4 = 4

/// adds metadata about how GLSL vertex attributes should be laid out
[<System.AttributeUsage(System.AttributeTargets.Struct, AllowMultiple = true)>]
type GLSLAttributeAttribute(location: uint32, dataType: VertexAttributeType) =
    inherit System.Attribute()
    member this.Location = location
    member this.DataType = dataType
