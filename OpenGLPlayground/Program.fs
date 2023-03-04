open Graphics.App
open Sketches

App(
    QuadSketch.quadSketch,
    {
      title = "Playground App"
      size = { width = 800; height = 600 }
    }
).Run()
