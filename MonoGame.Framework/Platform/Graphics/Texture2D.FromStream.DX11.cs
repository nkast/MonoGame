// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2019 Kastellanos Nikos

using System.IO;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.WIC;


namespace Microsoft.Xna.Framework.Graphics
{
    public partial class Texture2D
    {
        static ImagingFactory imgfactory_DX;
        
        private static BitmapSource LoadBitmap_DX(Stream stream, out SharpDX.WIC.BitmapDecoder decoder)
        {
            if (imgfactory_DX == null)
            {
                imgfactory_DX = new ImagingFactory();
            }

            decoder = new SharpDX.WIC.BitmapDecoder(
                imgfactory_DX,
                stream,
                DecodeOptions.CacheOnDemand
                );

            var fconv = new FormatConverter(imgfactory_DX);

            using (var frame = decoder.GetFrame(0))
            {
                fconv.Initialize(
                    frame,
                    PixelFormat.Format32bppRGBA,
                    BitmapDitherType.None,
                    null,
                    0.0,
                    BitmapPaletteType.Custom);
            }
            return fconv;
        }

        private unsafe static Texture2D PlatformFromStream_DX(GraphicsDevice graphicsDevice, Stream stream)
        {
            // For reference this implementation was ultimately found through this post:
            // http://stackoverflow.com/questions/9602102/loading-textures-with-sharpdx-in-metro 
            
            SharpDX.WIC.BitmapDecoder decoder;
            using (var bmpSource = LoadBitmap_DX(stream, out decoder))
            using (decoder)
            {
                Texture2D texture = new Texture2D(graphicsDevice, bmpSource.Size.Width, bmpSource.Size.Height);
                
                // TODO: use texture.SetData(...)
                Texture2DDescription desc;
                desc.Width = bmpSource.Size.Width;
                desc.Height = bmpSource.Size.Height;
                desc.ArraySize = 1;
                desc.BindFlags = BindFlags.ShaderResource;
                desc.Usage = ResourceUsage.Default;
                desc.CpuAccessFlags = CpuAccessFlags.None;
                desc.Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm;
                desc.MipLevels = 1;
                desc.OptionFlags = ResourceOptionFlags.None;
                desc.SampleDescription.Count = 1;
                desc.SampleDescription.Quality = 0;

                SharpDX.Direct3D11.Texture2D textureResource;
                using (DataStream s = new DataStream(bmpSource.Size.Height * bmpSource.Size.Width * 4, true, true))
                {
                    bmpSource.CopyPixels(bmpSource.Size.Width * 4, s);
                    DataRectangle rect = new DataRectangle(s.DataPointer, bmpSource.Size.Width * 4);
                    textureResource = new SharpDX.Direct3D11.Texture2D(graphicsDevice._d3dDevice, desc, rect);
                }
                texture.SetTextureInternal_DX(textureResource);

                return texture;
            }
        }
    }
}

