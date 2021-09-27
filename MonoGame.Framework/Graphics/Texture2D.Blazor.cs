// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
using MonoGame.Framework.Utilities;
using tainicom.Wasm.Canvas.WebGL;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class Texture2D : Texture
    {
        IWebGLRenderingContext GL { get { return GraphicsDevice._glContext; } }

        private void PlatformConstruct(int width, int height, bool mipmap, SurfaceFormat format, SurfaceType type, bool shared)
        {
            this.glTarget = WebGLTextureTarget.TEXTURE_2D;
            GetGLFormat(format, GraphicsDevice, out glInternalFormat, out glFormat, out glType, out _glIsCompressedTexture);

            {
                GenerateGLTextureIfRequired();
                int w = width;
                int h = height;
                int level = 0;
                while (true)
                {
                    if (_glIsCompressedTexture)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        GL.TexImage2D(WebGLTextureTarget.TEXTURE_2D, level, glInternalFormat, w, h, glFormat, glType);
                        GraphicsExtensions.CheckGLError();
                    }

                    if ((w == 1 && h == 1) || !mipmap)
                        break;
                    if (w > 1)
                        w = w / 2;
                    if (h > 1)
                        h = h / 2;
                    ++level;
                }
            }
        }

        private void PlatformSetData<T>(int level, T[] data, int startIndex, int elementCount) where T : struct
        {
            int w, h;
            GetSizeForLevel(Width, Height, level, out w, out h);

            var elementSizeInByte = ReflectionHelpers.SizeOf<T>.Get();

            var startBytes = startIndex * elementSizeInByte;
            if (startBytes != 0)
                throw new NotImplementedException("startIndex");


            GL.BindTexture(WebGLTextureTarget.TEXTURE_2D, glTexture);
            GraphicsExtensions.CheckGLError();

            GenerateGLTextureIfRequired();
            GL.PixelStore(WebGLPixelParameter.UNPACK_ALIGNMENT, Math.Min(_format.GetSize(), 8));
            GraphicsExtensions.CheckGLError();

            if (_glIsCompressedTexture)
            {
                throw new NotImplementedException();
            }
            else
            {
                GL.TexImage2D(WebGLTextureTarget.TEXTURE_2D, level, glInternalFormat, w, h, glFormat, glType, data);
            }
            GraphicsExtensions.CheckGLError();

            //GL.Finish();
            //GraphicsExtensions.CheckGLError();
        }

        private void PlatformSetData<T>(int level, int arraySlice, Rectangle rect, T[] data, int startIndex, int elementCount)
            where T : struct
        {
            var elementSizeInByte = ReflectionHelpers.SizeOf<T>.Get();

            var startBytes = startIndex * elementSizeInByte;
            if (startBytes != 0)
                throw new NotImplementedException("startIndex");

            GL.BindTexture(WebGLTextureTarget.TEXTURE_2D, glTexture);
            GraphicsExtensions.CheckGLError();

            GenerateGLTextureIfRequired();
            GL.PixelStore(WebGLPixelParameter.UNPACK_ALIGNMENT, Math.Min(_format.GetSize(), 8));

            if (_glIsCompressedTexture)
            {
                throw new NotImplementedException();
            }
            else
            {
                GL.TexSubImage2D(
                    WebGLTextureTarget.TEXTURE_2D, level, rect.X, rect.Y, rect.Width, rect.Height,
                    glFormat, glType, data);
            }
            GraphicsExtensions.CheckGLError();
        }

        private void PlatformGetData<T>(int level, int arraySlice, Rectangle rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            throw new NotImplementedException();
        }

        private void PlatformReload(Stream textureStream)
        {
            throw new NotImplementedException();
        }

        private void GenerateGLTextureIfRequired()
        {
            if (glTexture == null)
            {
                glTexture = GL.CreateTexture();
                GraphicsExtensions.CheckGLError();

                // For best compatibility and to keep the default wrap mode of XNA, only set ClampToEdge if either
                // dimension is not a power of two.
                var wrap = WebGLTexParam.REPEAT;
                if (((width & (width - 1)) != 0) || ((height & (height - 1)) != 0))
                    wrap = WebGLTexParam.CLAMP_TO_EDGE;

                GL.BindTexture(WebGLTextureTarget.TEXTURE_2D, this.glTexture);
                GraphicsExtensions.CheckGLError();

                GL.TexParameter(
                    WebGLTextureTarget.TEXTURE_2D, WebGLTexParamName.TEXTURE_MIN_FILTER,
                    (_levelCount > 1) ? WebGLTexParam.LINEAR_MIPMAP_LINEAR : WebGLTexParam.LINEAR);
                GraphicsExtensions.CheckGLError();

                GL.TexParameter(
                    WebGLTextureTarget.TEXTURE_2D, WebGLTexParamName.TEXTURE_MAG_FILTER,
                    WebGLTexParam.LINEAR);
                GraphicsExtensions.CheckGLError();

                GL.TexParameter(WebGLTextureTarget.TEXTURE_2D, WebGLTexParamName.TEXTURE_WRAP_S, wrap);
                GraphicsExtensions.CheckGLError();

                GL.TexParameter(WebGLTextureTarget.TEXTURE_2D, WebGLTexParamName.TEXTURE_WRAP_T, wrap);
                GraphicsExtensions.CheckGLError();

                // Set mipmap levels
                //GL2.TexParameter(WebGLTextureTarget.TEXTURE_2D, WebGL2TexParamName.TEXTURE_BASE_LEVEL, 0);
                //GraphicsExtensions.CheckGLError();
                if (GraphicsDevice.GraphicsCapabilities.SupportsTextureMaxLevel)
                {
                    if (_levelCount > 0)
                    {
                        throw new NotImplementedException();
                        // GL2.TexParameter(WebGLTextureTarget.TEXTURE_2D, WebGL2TexParamName.TEXTURE_MAX_LEVEL, _levelCount - 1);
                    }
                    else
                    {
                        throw new NotImplementedException();
                        // GL2.TexParameter(WebGLTextureTarget.TEXTURE_2D, WebGL2TexParamName.TEXTURE_MAX_LEVEL, 1000);
                    }
                    GraphicsExtensions.CheckGLError();
                }
            }
        }

    }
}

