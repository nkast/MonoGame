// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.InteropServices;
using MonoGame.Framework.Utilities;
using MonoGame.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class Texture3D : Texture
    {

        private void PlatformConstruct(GraphicsDevice graphicsDevice, int width, int height, int depth, bool mipMap, SurfaceFormat format, bool renderTarget)
        {
#if GLES
            throw new NotSupportedException("OpenGL ES 2.0 doesn't support 3D textures.");
#else
            this.glTarget = TextureTarget.Texture3D;

            Threading.BlockOnUIThread(() =>
            {
                GL.GenTextures(1, out this.glTexture);
                GraphicsExtensions.CheckGLError();

                GL.BindTexture(glTarget, glTexture);
                GraphicsExtensions.CheckGLError();

                format.GetGLFormat(GraphicsDevice, out glInternalFormat, out glFormat, out glType);

                GL.TexImage3D(glTarget, 0, glInternalFormat, width, height, depth, 0, glFormat, glType, IntPtr.Zero);
                GraphicsExtensions.CheckGLError();
            });

            if (mipMap)
                throw new NotImplementedException("Texture3D does not yet support mipmaps.");
#endif
        }

        private void PlatformSetDataAction<T>(
            int level,
            int left, int top, int right, int bottom, int front, int back,
            T[] data, int startIndex, int elementCount, int width, int height, int depth)
        {
            Threading.BlockOnUIThread(() => PlatformSetData(level, left, top, right, bottom, front, back, data, startIndex, elementCount, width, height, depth));
        }

        private void PlatformSetData<T>(
            int level,
            int left, int top, int right, int bottom, int front, int back,
            T[] data, int startIndex, int elementCount, int width, int height, int depth)
        {
#if GLES
            throw new NotSupportedException("OpenGL ES 2.0 doesn't support 3D textures.");
#else
            if (!Threading.IsOnUIThread())
            {
                PlatformSetDataAction(level, left, top, right, bottom, front, back,data, startIndex, elementCount, width, height, depth);
                return;
            }

            Threading.EnsureUIThread();

            {
                var elementSizeInByte = Marshal.SizeOf<T>();
                var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    var dataPtr = (IntPtr)(dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * elementSizeInByte);

                    GL.BindTexture(glTarget, glTexture);
                    GraphicsExtensions.CheckGLError();

                    GL.TexSubImage3D(glTarget, level, left, top, front, width, height, depth, glFormat, glType, dataPtr);
                    GraphicsExtensions.CheckGLError();
                }
                finally
                {
                    dataHandle.Free();
                }
            }
#endif
        }

        private void PlatformGetData<T>(int level, int left, int top, int right, int bottom, int front, int back, T[] data, int startIndex, int elementCount)
             where T : struct
        {

            throw new NotImplementedException();
        }
    }
}

