module Graphics.Texture

open System
open Silk.NET.OpenGL
open SkiaSharp

open GLExtensions

type Texture (gl: GL, fileName: string) =
    let handle = gl.glDo <| fun () -> gl.GenTexture()
    
    // members we need in the constructor
    let _bind (slot: TextureUnit) =
        // set texture slot and bind
        gl.glDo <| fun () -> gl.ActiveTexture(slot)
        gl.glDo <| fun () -> gl.BindTexture(TextureTarget.Texture2D, handle)
        
    let _unbind () = gl.glDo <| fun () -> gl.BindTexture(TextureTarget.Texture2D, 0u)
    
    do
        // load image and free mem when we're done here
        use image = SKBitmap.Decode fileName
        
        // bind our texture
        _bind TextureUnit.Texture0
        
        // send image bytes to gpu
        gl.glDo <| fun () ->
            use data = fixed image.Bytes
            
            let format, pixelType =
                match image.ColorType with
                | SKColorType.Rgba8888 -> PixelFormat.Rgba, PixelType.UnsignedByte 
                | SKColorType.Bgra8888 -> PixelFormat.Bgra, PixelType.UnsignedByte 
                | _ ->
                    printfn $"[Playground Warning - Texture.fs]: Unsupported SKColorType {image.ColorType}"
                    PixelFormat.Rgba, PixelType.UnsignedByte 
            
            gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba8, // how OpenGL will store our data
                uint32 image.Width,
                uint32 image.Height,
                0, // 0 pixel border on the texture
                format, // format of data we are giving OpenGL
                pixelType, // each channel format
                ReadOnlySpan(NativeInterop.NativePtr.toVoidPtr data, image.Bytes.Length)) // can use nullptr here if we don't have the data yet
    
        // setting some texture parameters so the texture behaves as expected
        // REQUIRED SETTINGS (first 4)
        gl.glDo <| fun () -> gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, int GLEnum.ClampToEdge)
        gl.glDo <| fun () -> gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, int GLEnum.ClampToEdge);
        gl.glDo <| fun () -> gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, int GLEnum.LinearMipmapLinear);
        gl.glDo <| fun () -> gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, int GLEnum.Linear)
        
        gl.glDo <| fun () -> gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
        gl.glDo <| fun () -> gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8)
        
        // generating mipmaps
        gl.glDo <| fun () -> gl.GenerateMipmap TextureTarget.Texture2D
        
        // unbind now that we're finished
        _unbind ()
    
    member this.bind = _bind
    member this.unbind = _unbind
    member this.delete () = gl.glDo <| fun () -> gl.DeleteTexture handle
    