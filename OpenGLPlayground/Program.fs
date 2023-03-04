open Silk.NET.Maths
open OpenGLPlayground.Infrastructure.Graphics.App
open OpenGLPlayground.Sketches.Sketches

App(
    QuadSketch.quadSketch,
    { title = "Playground App"
      size = Vector2D(800, 600) }
).Run()
