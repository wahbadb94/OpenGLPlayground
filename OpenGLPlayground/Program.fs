open Graphics.App
open Silk.NET.Maths
open Sketches

App(
    QuadSketch.quadSketch,
    {
      title = "Playground App"
      size = Vector2D(800, 600)
    }
).Run()
