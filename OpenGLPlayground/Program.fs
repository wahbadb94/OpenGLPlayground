open OpenGLPlayground
open Silk.NET.Maths
open OpenGLPlayground.App

App(
    QuadSketch.quadSketch,
    { title = "Playground App"
      size = Vector2D(800, 600) }
).Run()
