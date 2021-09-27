// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using tainicom.Wasm.Canvas.WebGL;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class RenderTarget2D
    {

        WebGLTexture IRenderTarget.GLTexture
        {
            get { return glTexture; }
        }

        WebGLTextureTarget IRenderTarget.GLTarget
        {
            get { return glTarget; }
        }

        WebGLTexture IRenderTarget.GLColorBuffer { get; set; }
        WebGLRenderbuffer IRenderTarget.GLDepthBuffer { get; set; }
        WebGLRenderbuffer IRenderTarget.GLStencilBuffer { get; set; }

        WebGLTextureTarget IRenderTarget.GetFramebufferTarget(RenderTargetBinding renderTargetBinding)
        {
            return glTarget;
        }

        private void PlatformConstruct(GraphicsDevice graphicsDevice, int width, int height, bool mipMap,
            DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage, bool shared)
        {
            //Threading.EnsureUIThread();
            {
                graphicsDevice.PlatformCreateRenderTarget(
                    this, width, height, mipMap, this.Format, preferredDepthFormat, preferredMultiSampleCount, usage);
            }
        }

        private void PlatformGraphicsDeviceResetting()
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {

            }

            GraphicsDevice.PlatformDeleteRenderTarget(this);

            base.Dispose(disposing);
        }
    }
}
