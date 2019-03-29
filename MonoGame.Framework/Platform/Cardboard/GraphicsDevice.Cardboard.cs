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
    }
}