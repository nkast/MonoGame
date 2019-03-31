using System;
using System.Collections.Generic;
using System.Diagnostics;

using MonoGame.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class GraphicsDevice
    {
        internal void PlatformInvalidateDeviceContext()
        {            
            // invalidate clearColor 
            this._lastClearColor.X = float.MaxValue;
            this._lastClearStencil = int.MaxValue;
            this._lastClearDepth = float.MaxValue;
            
            // clear states
            this._depthStencilStateDirty = true;
            this._rasterizerStateDirty = true;

            // clear states
            //this._game.GraphicsDevice._lastRasterizerState = null;
            //this._game.GraphicsDevice._lastDepthStencilState = null;
            //this._game.GraphicsDevice._lastBlendState = null;

            //invalidate scissor
            this._scissorRectangleDirty = true;

            //invalidate index buffer
            _attribsDirty = true;
            this._indexBufferDirty = true;

            //invalidate shaders
            this._vertexShaderDirty = true;
            this._pixelShaderDirty = true;
            this._shaderProgram = null;

            //invalidate VertexAttributes
            _enabledVertexAttributes.Clear();

            //invalidate textures
            Textures.Clear();
        }

        private RenderTarget2D _rtBackBuffer;
        private SpriteBatch _spriteBatch;
         
        private void PlatformCardboardInitialize()
        {
            int width = PresentationParameters.BackBufferWidth;
            int height = PresentationParameters.BackBufferHeight;
            bool mipMap = false;
            SurfaceFormat preferredFormat = SurfaceFormat.Color;
            DepthFormat preferredDepthFormat = DepthFormat.Depth24Stencil8;

            _rtBackBuffer = new RenderTarget2D(this, width, height, mipMap, preferredFormat, preferredDepthFormat);
            _spriteBatch = new SpriteBatch(this);

        }
        
        bool _skipDefaultRenderTarget = false;
        private bool PlatformCardboardApplyDefaultRenderTarget()
        {
            return false;

            if (_skipDefaultRenderTarget)
                return false;

            this.SetRenderTarget(_rtBackBuffer);
            return true;
        }

        internal void PlatformBeforeDraw()
        {
            var device = this;

            //if (_rtBackBuffer == null)
            //{
            //    _rtBackBuffer = new RenderTarget2D(device, 500, 400, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
            //}
            //device.SetRenderTarget(device._rtBackBuffer);
        }
        
   
        internal void PlatformApplyStagingRenderTarget()
        {
            return;

            //if (_rtBackBuffer == null)
            //{                
            //    int width = 800;
            //    int height = 400;
            //    bool mipMap = false;
            //     SurfaceFormat preferredFormat = SurfaceFormat.Color;
            //    DepthFormat preferredDepthFormat = DepthFormat.Depth24Stencil8;
            //
            //    _rtBackBuffer = new RenderTarget2D(this, width, height, mipMap, preferredFormat, preferredDepthFormat, 0, RenderTargetUsage.PlatformContents);
            //}

            //this.SetRenderTarget(_rtBackBuffer);
        }

        // present 
        internal void PlatformRenderStagingRenderTarget(int glFramebuffer, int width, int height)
        {
            return;

            if (_rtBackBuffer == null)
                return;

            try
            {
                //PlatformInvalidateDeviceContext();
                //this._currentRenderTargetCount = 0;
                this._skipDefaultRenderTarget = true;                
                this.glFramebuffer = glFramebuffer;
                this.SetRenderTarget(null);
                this._skipDefaultRenderTarget = false;
                
                /*
                this.framebufferHelper.BindFramebuffer(glFramebuffer);
                // Reset the raster state because we flip vertices
                // when rendering offscreen and hence the cull direction.
                _rasterizerStateDirty = true;
                // Textures will need to be rebound to render correctly in the new render target.
                Textures.Dirty();
                */

                this.Viewport = new Viewport(0,0, width, height);                                
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
                _spriteBatch.Draw(_rtBackBuffer, Vector2.Zero, Color.PaleVioletRed); //, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
                _spriteBatch.End();
            }
            finally
            {
                _skipDefaultRenderTarget = false;
            }
        }
    }
}