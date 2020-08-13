using System;

using SharpDX.DXGI;
using SharpDX.Direct3D11;


namespace Microsoft.Xna.Framework.Graphics
{

    /// <summary>
    /// A swap chain used for rendering to a secondary GameWindow.
    /// </summary>
    /// <remarks>
    /// This is an extension and not part of stock XNA.
    /// It is currently implemented for Windows and DirectX only.
    /// </remarks>
    public class SwapChainRenderTarget : RenderTarget2D
    {
        private SwapChain _swapChain;
        private IntPtr _windowHandle;
        private SharpDX.Direct3D11.Texture2D _backBuffer;

        public readonly PresentInterval PresentInterval;

        public SwapChainRenderTarget(   GraphicsDevice graphicsDevice,
                                        IntPtr windowHandle,
                                        int width,
                                        int height)
            : this( 
                graphicsDevice, 
                windowHandle, 
                width, 
                height, 
                false, 
                SurfaceFormat.Color,
                DepthFormat.Depth24,
                0, 
                RenderTargetUsage.DiscardContents,
                PresentInterval.Default)
        {
        }

        public SwapChainRenderTarget(   GraphicsDevice graphicsDevice,
                                        IntPtr windowHandle,                                     
                                        int width,
                                        int height,
                                        bool mipMap,
                                        SurfaceFormat surfaceFormat,
                                        DepthFormat depthFormat,                                        
                                        int preferredMultiSampleCount,
                                        RenderTargetUsage usage,
                                        PresentInterval presentInterval)
            : base(
                graphicsDevice,
                width,
                height,
                mipMap,
                surfaceFormat,
                depthFormat,
                graphicsDevice.GetClampedMultisampleCount(preferredMultiSampleCount),
                usage,
                SurfaceType.SwapChainRenderTarget)
        {
            var dxgiFormat = surfaceFormat == SurfaceFormat.Color
                             ? SharpDX.DXGI.Format.B8G8R8A8_UNorm
                             : SharpDXHelper.ToFormat(surfaceFormat);

            preferredMultiSampleCount = graphicsDevice.GetClampedMultisampleCount(preferredMultiSampleCount);
            var multisampleDesc = graphicsDevice.GetSupportedSampleDescription(dxgiFormat, preferredMultiSampleCount);
            _windowHandle = windowHandle;
            PresentInterval = presentInterval;


            CreateSwaipChainTexture(dxgiFormat, multisampleDesc);

            // Once the desired swap chain description is configured, it must 
            // be created on the same adapter as our D3D Device
            var d3dDevice = graphicsDevice._d3dDevice;

            // Create a view interface on the rendertarget to use on bind.
            _renderTargetViews = new[] { new RenderTargetView(d3dDevice, _backBuffer) };

            // Get the rendertarget dimensions for later.
            var backBufferDesc = _backBuffer.Description;
            var targetSize = new Point(backBufferDesc.Width, backBufferDesc.Height);

            // Create the depth buffer if we need it.
            if (depthFormat != DepthFormat.None)
            {
                dxgiFormat = SharpDXHelper.ToFormat(depthFormat);

                // Allocate a 2-D surface as the depth/stencil buffer.
                using (
                    var depthBuffer = new SharpDX.Direct3D11.Texture2D(d3dDevice,
                                                                       new Texture2DDescription()
                                                                           {
                                                                               Format = dxgiFormat,
                                                                               ArraySize = 1,
                                                                               MipLevels = 1,
                                                                               Width = targetSize.X,
                                                                               Height = targetSize.Y,
                                                                               SampleDescription = multisampleDesc,
                                                                               Usage = ResourceUsage.Default,
                                                                               BindFlags = BindFlags.DepthStencil,
                                                                           }))

                    // Create a DepthStencil view on this surface to use on bind.
                    _depthStencilView = new DepthStencilView(d3dDevice, depthBuffer);
            }
        }

        private SharpDX.Direct3D11.Resource CreateSwaipChainTexture(SharpDX.DXGI.Format dxgiFormat, SharpDX.DXGI.SampleDescription multisampleDesc)
        {
            var d3dDevice = GraphicsDevice._d3dDevice;

            var desc = new SwapChainDescription()
            {
                ModeDescription =
                {
                    Format = dxgiFormat,
                    Scaling = DisplayModeScaling.Stretched,
                    Width = width,
                    Height = height,
                },

                OutputHandle = _windowHandle,
                SampleDescription = multisampleDesc,
                Usage = Usage.RenderTargetOutput,
                BufferCount = 2,
                SwapEffect = SharpDXHelper.ToSwapEffect(PresentInterval),
                IsWindowed = true,
            };
            
            // First, retrieve the underlying DXGI Device from the D3D Device.
            // Creates the swap chain 
            using (var dxgiDevice = d3dDevice.QueryInterface<SharpDX.DXGI.Device1>())
            using (var dxgiAdapter = dxgiDevice.Adapter)
            using (var dxgiFactory = dxgiAdapter.GetParent<Factory1>())
            {
                _swapChain = new SwapChain(dxgiFactory, dxgiDevice, desc);
            }

            // Obtain the backbuffer for this window which will be the final 3D rendertarget.
            _backBuffer = SharpDX.Direct3D11.Resource.FromSwapChain<SharpDX.Direct3D11.Texture2D>(_swapChain, 0);

            return _backBuffer;
        }
        
        internal override SharpDX.Direct3D11.Resource CreateTexture()
        {
            return base.CreateTexture();
        }

        internal override void ResolveSubresource()
        {
            // SwapChainRenderTarget don't need to resolve the MSAA texture.
        }

        // TODO: We need to expose the other Present() overloads
        // for passing source/dest rectangles.

        /// <summary>
        /// Displays the contents of the active back buffer to the screen.
        /// </summary>
        public void Present()
        {
            lock (GraphicsDevice._d3dContext)
            {
                try
                {
                    _swapChain.Present(PresentInterval.GetSyncInterval(), PresentFlags.None);
                }
                catch (SharpDX.SharpDXException)
                {
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SharpDX.Utilities.Dispose(ref _swapChain);
            }

            base.Dispose(disposing);
        }

    }
}

