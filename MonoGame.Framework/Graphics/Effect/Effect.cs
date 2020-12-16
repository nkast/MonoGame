// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Diagnostics;
using System.IO;
using MonoGame.Framework.Utilities;

namespace Microsoft.Xna.Framework.Graphics
{
	public class Effect : GraphicsResource
    {
        struct MGFXHeader 
        {
            /// <summary>
            /// The MonoGame Effect file format header identifier ("MGFX"). 
            /// </summary>
            public static readonly int MGFXSignature = (BitConverter.IsLittleEndian) ? 0x5846474D: 0x4D474658;

            /// <summary>
            /// The current MonoGame Effect file format versions
            /// used to detect old packaged content.
            /// </summary>
            /// <remarks>
            /// We should avoid supporting old versions for very long if at all 
            /// as users should be rebuilding content when packaging their game.
            /// </remarks>
            public const int MGFXVersion = 10;

            public readonly int Signature;
            public readonly int Version;
            public readonly int Profile;
            public readonly int EffectKey;
            public readonly int HeaderSize;

            public MGFXHeader(byte[] effectCode, int index)
            {
                int offset = 0;
                Signature = BitConverter.ToInt32(effectCode, index + offset); offset += 4;
                Version = (int)effectCode[index + offset]; offset += 1;
                Profile = (int)effectCode[index + offset]; offset += 1;
                EffectKey = BitConverter.ToInt32(effectCode, index + offset); offset += 4;
                HeaderSize = offset;
            }
        }

        public EffectParameterCollection Parameters { get; private set; }

        public EffectTechniqueCollection Techniques { get; private set; }

        public EffectTechnique CurrentTechnique { get; set; }
  
        internal ConstantBuffer[] ConstantBuffers { get; private set; }

        private Shader[] _shaders;

	    private readonly bool _isClone;

        internal Effect(GraphicsDevice graphicsDevice)
		{
            if (graphicsDevice == null)
            {
                throw new ArgumentNullException("graphicsDevice", FrameworkResources.ResourceCreationWhenDeviceIsNull);
            }
            this.GraphicsDevice = graphicsDevice;
		}
			
		protected Effect(Effect cloneSource)
            : this(cloneSource.GraphicsDevice)
		{
            _isClone = true;
            Clone(cloneSource);
		}

        public Effect(GraphicsDevice graphicsDevice, byte[] effectCode)
            : this(graphicsDevice, effectCode, 0, effectCode.Length)
        {
        }


        public Effect (GraphicsDevice graphicsDevice, byte[] effectCode, int index, int count)
            : this(graphicsDevice)
		{
			// By default we currently cache all unique byte streams
			// and use cloning to populate the effect with parameters,
			// techniques, and passes.
			//
			// This means all the immutable types in an effect:
			//
			//  - Shaders
			//  - Annotations
			//  - Names
			//  - State Objects
			//
			// Are shared for every instance of an effect while the 
			// parameter values and constant buffers are copied.
			//
			// This might need to change slightly if/when we support
			// shared constant buffers as 'new' should return unique
			// effects without any shared instance state.
 
            //Read the header
            MGFXHeader header = new MGFXHeader(effectCode, index);
            if (header.Signature != MGFXHeader.MGFXSignature)
                throw new Exception("This does not appear to be a MonoGame MGFX file!");
            if (header.Profile != Shader.Profile)
                throw new Exception("This MGFX effect was built for a different platform!");
            if (header.Version > MGFXHeader.MGFXVersion)
                throw new Exception("This MGFX effect seems to be for a newer release of MonoGame.");
            if (header.Version == 8) //TNC: fallback to version 8
                System.Diagnostics.Debug.WriteLine("This MGFX effect is for an older release of MonoGame and needs to be rebuilt.");
            else if (header.Version == 9) //TNC: fallback to version 9
            { }
            else
            if (header.Version < MGFXHeader.MGFXVersion)
                throw new Exception("This MGFX effect is for an older release of MonoGame and needs to be rebuilt.");

            // First look for it in the cache.
            //
            Effect cloneSource;
            lock (graphicsDevice.EffectCache)
            {
                if (!graphicsDevice.EffectCache.TryGetValue(header.EffectKey, out cloneSource))
                {
                    using (var stream = new MemoryStream(effectCode, index + header.HeaderSize, count - header.HeaderSize, false))
                    if (header.Version == 8 || header.Version == 9)
               	   using (var reader = new EffectReader9(stream, graphicsDevice, header))
                    {
                        // Create Effect.
                        cloneSource = reader.ReadEffect();

                        // Cache the effect for later in its original unmodified state.
                        graphicsDevice.EffectCache.Add(header.EffectKey, cloneSource);
                    }
                   else
                   using (var reader = new BinaryReader(stream))
                   {
                        // Create one.
                        cloneSource = new Effect(graphicsDevice);
                        cloneSource.ReadEffect(reader);

                        // Check file tail to ensure we parsed the content correctly.
                        var tail = reader.ReadInt32();
                        if (tail != MGFXHeader.MGFXSignature) throw new ArgumentException("The MGFX effect code was not parsed correctly.", "effectCode");

                        // Cache the effect for later in its original unmodified state.
                        graphicsDevice.EffectCache.Add(header.EffectKey, cloneSource);
                   }
                }
            }

            // Clone it.
            _isClone = true;
            Clone(cloneSource);
        }

        /// <summary>
        /// Clone the source into this existing object.
        /// </summary>
        /// <remarks>
        /// Note this is not overloaded in derived classes on purpose.  This is
        /// only a reason this exists is for caching effects.
        /// </remarks>
        /// <param name="cloneSource">The source effect to clone from.</param>
        private void Clone(Effect cloneSource)
        {
            Debug.Assert(_isClone, "Cannot clone into non-cloned effect!");

            // Copy the mutable members of the effect.
            Parameters = cloneSource.Parameters.Clone();
            Techniques = cloneSource.Techniques.Clone(this);

            // Make a copy of the immutable constant buffers.
            ConstantBuffers = new ConstantBuffer[cloneSource.ConstantBuffers.Length];
            for (var i = 0; i < cloneSource.ConstantBuffers.Length; i++)
                ConstantBuffers[i] = new ConstantBuffer(cloneSource.ConstantBuffers[i]);

            // Find and set the current technique.
            for (var i = 0; i < cloneSource.Techniques.Count; i++)
            {
                if (cloneSource.Techniques[i] == cloneSource.CurrentTechnique)
                {
                    CurrentTechnique = Techniques[i];
                    break;
                }
            }

            // Take a reference to the original shader list.
            _shaders = cloneSource._shaders;
        }

        /// <summary>
        /// Returns a deep copy of the effect where immutable types 
        /// are shared and mutable data is duplicated.
        /// </summary>
        /// <remarks>
        /// See "Cloning an Effect" in MSDN:
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/ff476138(v=vs.85).aspx
        /// </remarks>
        /// <returns>The cloned effect.</returns>
		public virtual Effect Clone()
		{
            return new Effect(this);
		}

        protected internal virtual void OnApply()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (!_isClone)
                    {
                        // Only the clone source can dispose the shaders.
                        if (_shaders != null)
                        {
                            foreach (var shader in _shaders)
                                shader.Dispose();
                        }
                    }

                    if (ConstantBuffers != null)
                    {
                        foreach (var buffer in ConstantBuffers)
                            buffer.Dispose();
                        ConstantBuffers = null;
                    }
                }
            }

            base.Dispose(disposing);
        }

        internal protected override void GraphicsDeviceResetting()
        {
            for (var i = 0; i < ConstantBuffers.Length; i++)
                ConstantBuffers[i].Clear();
        }

        #region Effect File Reader

		private void ReadEffect (BinaryReader reader)
		{
			// TODO: Maybe we should be reading in a string 
			// table here to save some bytes in the file.

            ConstantBuffers = new ConstantBuffer[reader.ReadInt32()];

            for (var c = 0; c < ConstantBuffers.Length; c++)
            {				
				var name = reader.ReadString();

				// Create the backing system memory buffer.
				var sizeInBytes = (int)reader.ReadInt16();

				// Read the parameter index values.
                var parameters = new int[reader.ReadInt32()];
				var offsets = new int[parameters.Length];
				for (var i = 0; i < parameters.Length; i++) 
                {
                    parameters[i] = reader.ReadInt32();
                    offsets[i] = (int)reader.ReadUInt16();
				}

                ConstantBuffers[c] = new ConstantBuffer(GraphicsDevice,
				                                sizeInBytes,
				                                parameters,
				                                offsets,
				                                name);
            }

            _shaders = new Shader[reader.ReadInt32()];

            for (var s = 0; s < _shaders.Length; s++)
                _shaders[s] = new Shader(GraphicsDevice, reader);

            Parameters = ReadParameters(reader);

            var techniques = new EffectTechnique[reader.ReadInt32()];

            for (var t = 0; t < techniques.Length; t++)
            {
                var name = reader.ReadString();
                var annotations = ReadAnnotations(reader);
                var passes = ReadPasses(reader, this, _shaders);

                techniques[t] = new EffectTechnique(this, name, passes, annotations);
            }

            Techniques = new EffectTechniqueCollection(techniques);

            CurrentTechnique = Techniques[0];
        }

        private static EffectAnnotationCollection ReadAnnotations(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            if (count == 0)
                return EffectAnnotationCollection.Empty;

            var annotations = new EffectAnnotation[count];

            // TODO: Annotations are not implemented!

            return new EffectAnnotationCollection(annotations);
        }

        private static EffectPassCollection ReadPasses(BinaryReader reader, Effect effect, Shader[] shaders)
        {
            var passes = new EffectPass[reader.ReadInt32()];

            for (var i = 0; i < passes.Length; i++)
            {
                var name = reader.ReadString();
                var annotations = ReadAnnotations(reader);

                // Get the vertex shader.
                var shaderIndex = reader.ReadInt32();
                Shader vertexShader = shaderIndex < 0 ? null : shaders[shaderIndex];

                // Get the pixel shader.
                shaderIndex = reader.ReadInt32();
                Shader pixelShader = shaderIndex < 0 ? null : shaders[shaderIndex];

				BlendState blend = null;
				DepthStencilState depth = null;
				RasterizerState raster = null;
				if (reader.ReadBoolean())
				{
					blend = new BlendState
					{
						AlphaBlendFunction = (BlendFunction)reader.ReadByte(),
						AlphaDestinationBlend = (Blend)reader.ReadByte(),
						AlphaSourceBlend = (Blend)reader.ReadByte(),
						BlendFactor = new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte()),
						ColorBlendFunction = (BlendFunction)reader.ReadByte(),
						ColorDestinationBlend = (Blend)reader.ReadByte(),
						ColorSourceBlend = (Blend)reader.ReadByte(),
						ColorWriteChannels = (ColorWriteChannels)reader.ReadByte(),
						ColorWriteChannels1 = (ColorWriteChannels)reader.ReadByte(),
						ColorWriteChannels2 = (ColorWriteChannels)reader.ReadByte(),
						ColorWriteChannels3 = (ColorWriteChannels)reader.ReadByte(),
						MultiSampleMask = reader.ReadInt32(),
					};
				}
				if (reader.ReadBoolean())
				{
					depth = new DepthStencilState
					{
						CounterClockwiseStencilDepthBufferFail = (StencilOperation)reader.ReadByte(),
						CounterClockwiseStencilFail = (StencilOperation)reader.ReadByte(),
						CounterClockwiseStencilFunction = (CompareFunction)reader.ReadByte(),
						CounterClockwiseStencilPass = (StencilOperation)reader.ReadByte(),
						DepthBufferEnable = reader.ReadBoolean(),
						DepthBufferFunction = (CompareFunction)reader.ReadByte(),
						DepthBufferWriteEnable = reader.ReadBoolean(),
						ReferenceStencil = reader.ReadInt32(),
						StencilDepthBufferFail = (StencilOperation)reader.ReadByte(),
						StencilEnable = reader.ReadBoolean(),
						StencilFail = (StencilOperation)reader.ReadByte(),
						StencilFunction = (CompareFunction)reader.ReadByte(),
						StencilMask = reader.ReadInt32(),
						StencilPass = (StencilOperation)reader.ReadByte(),
						StencilWriteMask = reader.ReadInt32(),
						TwoSidedStencilMode = reader.ReadBoolean(),
					};
				}
				if (reader.ReadBoolean())
				{
					raster = new RasterizerState
					{
						CullMode = (CullMode)reader.ReadByte(),
						DepthBias = reader.ReadSingle(),
						FillMode = (FillMode)reader.ReadByte(),
						MultiSampleAntiAlias = reader.ReadBoolean(),
						ScissorTestEnable = reader.ReadBoolean(),
						SlopeScaleDepthBias = reader.ReadSingle(),
					};
				}

                passes[i] = new EffectPass(effect, name, vertexShader, pixelShader, blend, depth, raster, annotations);
			}

            return new EffectPassCollection(passes);
		}

		private static EffectParameterCollection ReadParameters(BinaryReader reader)
		{
            var count = reader.ReadInt32();
            if (count == 0)
                return EffectParameterCollection.Empty;

            var parameters = new EffectParameter[count];
			for (var i = 0; i < count; i++)
			{
				var class_ = (EffectParameterClass)reader.ReadByte();				
                var type = (EffectParameterType)reader.ReadByte();
				var name = reader.ReadString();
				var semantic = reader.ReadString();
				var annotations = ReadAnnotations(reader);
				var rowCount = (int)reader.ReadByte();
				var columnCount = (int)reader.ReadByte();

				var elements = ReadParameters(reader);
				var structMembers = ReadParameters(reader);

				object data = null;
				if (elements.Count == 0 && structMembers.Count == 0)
				{
					switch (type)
					{						
                        case EffectParameterType.Bool:
                        case EffectParameterType.Int32:
#if !OPENGL
                            // Under most platforms we properly store integers and 
                            // booleans in an integer type.
                            //
                            // MojoShader on the otherhand stores everything in float
                            // types which is why this code is disabled under OpenGL.
					        {
					            var buffer = new int[rowCount * columnCount];								
                                for (var j = 0; j < buffer.Length; j++)
                                    buffer[j] = reader.ReadInt32();
                                data = buffer;
                                break;
					        }
#endif

						case EffectParameterType.Single:
							{
								var buffer = new float[rowCount * columnCount];
								for (var j = 0; j < buffer.Length; j++)
                                    buffer[j] = reader.ReadSingle();
                                data = buffer;
                                break;							
                            }

                        case EffectParameterType.String:
                            // TODO: We have not investigated what a string
                            // type should do in the parameter list.  Till then
                            // throw to let the user know.
							throw new NotSupportedException();

                        default:
                            // NOTE: We skip over all other types as they 
                            // don't get added to the constant buffer.
					        break;
					}
                }

				parameters[i] = new EffectParameter(
					class_, type, name, rowCount, columnCount, semantic, 
					annotations, elements, structMembers, data);
			}

			return new EffectParameterCollection(parameters);
		}

        private class EffectReader9 : BinaryReader
        {
            private readonly GraphicsDevice graphicsDevice;
            private readonly MGFXHeader header;

            public EffectReader9(MemoryStream stream, GraphicsDevice graphicsDevice, MGFXHeader header) : base(stream)
            {
                this.header = header;
                this.graphicsDevice = graphicsDevice;
            }

            internal Effect ReadEffect()
            {
                var effect = new Effect(graphicsDevice);

                effect.ConstantBuffers = ReadConstantBuffers();
                effect._shaders = ReadShaders();
                effect.Parameters = ReadParameters();
                effect.Techniques = ReadTechniques(effect);

                effect.CurrentTechnique = effect.Techniques[0];

                return effect;
            }

            private EffectTechniqueCollection ReadTechniques(Effect effect)
            {
                var techniqueCount = (int)ReadByte();
                // Value 0 for techniqueCount is reserved.
                // This is an extension to the format to support more than 255 techniques.
                var isExtTechniques = (techniqueCount == 0);
                if (isExtTechniques)
                    techniqueCount = Read7BitEncodedInt(); // an extension to the format to support more than 255 techniques

                var techniques = new EffectTechnique[techniqueCount];
                for (var t = 0; t < techniqueCount; t++)
                {
                    var name = ReadString();
                    var annotations = ReadAnnotations();
                    var passes = ReadPasses(effect, isExtTechniques);
                    techniques[t] = new EffectTechnique(effect, name, passes, annotations);
                }

                return new EffectTechniqueCollection(techniques);
            }

            private ConstantBuffer[] ReadConstantBuffers()
            {
                var buffersCount = (int)ReadByte();
                var constantBuffers = new ConstantBuffer[buffersCount];
                for (var c = 0; c < buffersCount; c++)
                    constantBuffers[c] = ReadConstantBuffer();
                return constantBuffers;
            }

            ConstantBuffer ReadConstantBuffer()
            {
                var name = ReadString();

                // Create the backing system memory buffer.
                var sizeInBytes = (int)ReadInt16();

                // Read the parameter index values.
                var parameters = new int[ReadByte()];
                var offsets = new int[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    parameters[i] = (int)ReadByte();
                    offsets[i] = (int)ReadUInt16();
                }

                var buffer = new ConstantBuffer(graphicsDevice,
                                                sizeInBytes,
                                                parameters,
                                                offsets,
                                                name);
                return buffer;
            }

            private Shader[] ReadShaders()
            {
                var shadersCount = (int)ReadByte();
                var shaders = new Shader[shadersCount];
                for (var s = 0; s < shadersCount; s++)
                    shaders[s] = ReadShader();
                return shaders;
            }

            private Shader ReadShader()
            {
                return new Shader(graphicsDevice, this);
            }

            private EffectPassCollection ReadPasses(Effect effect, bool isExtTechniques)
            {
                var passesCount = (int)ReadByte();
                var passes = new EffectPass[passesCount];
                for (var i = 0; i < passesCount; i++)
                    ReadEffectPass(effect, isExtTechniques, passes, i);
                return new EffectPassCollection(passes);
		    }

            private void ReadEffectPass(Effect effect, bool isExtTechniques, EffectPass[] passes, int i)
            {
                var name = ReadString();
                var annotations = ReadAnnotations();

                // Get the vertex and pixel shader.
                Shader vertexShader = null;
                Shader pixelShader = null;
                if (isExtTechniques)
                {
                    int vertexShaderIndex = ReadPackedInt();
                    int pixelShaderIndex = ReadPackedInt();
                    vertexShader = effect._shaders[vertexShaderIndex];
                    pixelShader = effect._shaders[pixelShaderIndex];
                }
                else
                {
                    var vertexShaderIndex = (int)ReadByte();
                    var pixelShaderIndex = (int)ReadByte();
                    if (vertexShaderIndex != 255)
                        vertexShader = effect._shaders[vertexShaderIndex];
                    if (pixelShaderIndex != 255)
                        pixelShader = effect._shaders[pixelShaderIndex];
                }

                BlendState blend = ReadBoolean() ? ReadBlendState() : null;
                DepthStencilState depth = ReadBoolean() ? ReadDepthStencilState() : null;
                RasterizerState rasterizer = ReadBoolean() ? ReadRasterizerState() : null;

                passes[i] = new EffectPass(effect, name, vertexShader, pixelShader, blend, depth, rasterizer, annotations);
            }

            private BlendState ReadBlendState()
            {
                return new BlendState
                {
                    AlphaBlendFunction = (BlendFunction)ReadByte(),
                    AlphaDestinationBlend = (Blend)ReadByte(),
                    AlphaSourceBlend = (Blend)ReadByte(),
                    BlendFactor = new Color(ReadByte(), ReadByte(), ReadByte(), ReadByte()),
                    ColorBlendFunction = (BlendFunction)ReadByte(),
                    ColorDestinationBlend = (Blend)ReadByte(),
                    ColorSourceBlend = (Blend)ReadByte(),
                    ColorWriteChannels = (ColorWriteChannels)ReadByte(),
                    ColorWriteChannels1 = (ColorWriteChannels)ReadByte(),
                    ColorWriteChannels2 = (ColorWriteChannels)ReadByte(),
                    ColorWriteChannels3 = (ColorWriteChannels)ReadByte(),
                    MultiSampleMask = ReadInt32(),
                };
            }

            private DepthStencilState ReadDepthStencilState()
            {
                return new DepthStencilState
                {
                    CounterClockwiseStencilDepthBufferFail = (StencilOperation)ReadByte(),
                    CounterClockwiseStencilFail = (StencilOperation)ReadByte(),
                    CounterClockwiseStencilFunction = (CompareFunction)ReadByte(),
                    CounterClockwiseStencilPass = (StencilOperation)ReadByte(),
                    DepthBufferEnable = ReadBoolean(),
                    DepthBufferFunction = (CompareFunction)ReadByte(),
                    DepthBufferWriteEnable = ReadBoolean(),
                    ReferenceStencil = ReadInt32(),
                    StencilDepthBufferFail = (StencilOperation)ReadByte(),
                    StencilEnable = ReadBoolean(),
                    StencilFail = (StencilOperation)ReadByte(),
                    StencilFunction = (CompareFunction)ReadByte(),
                    StencilMask = ReadInt32(),
                    StencilPass = (StencilOperation)ReadByte(),
                    StencilWriteMask = ReadInt32(),
                    TwoSidedStencilMode = ReadBoolean(),
                };
            }

            private RasterizerState ReadRasterizerState()
            {
                return new RasterizerState
                {
                    CullMode = (CullMode)ReadByte(),
                    DepthBias = ReadSingle(),
                    FillMode = (FillMode)ReadByte(),
                    MultiSampleAntiAlias = ReadBoolean(),
                    ScissorTestEnable = ReadBoolean(),
                    SlopeScaleDepthBias = ReadSingle(),
                };
            }

            private EffectAnnotationCollection ReadAnnotations()
            {
                var count = (int)ReadByte();
                if (count == 0)
                    return EffectAnnotationCollection.Empty;

                var annotations = new EffectAnnotation[count];

                // TODO: Annotations are not implemented!

                return new EffectAnnotationCollection(annotations);
            }

            private EffectParameterCollection ReadParameters()
		    {
                //TNC: fallback to version 8
                int count = (header.Version == 8)
                    ? (int)ReadByte()
                    : Read7BitEncodedInt();

                if (count == 0)
                    return EffectParameterCollection.Empty;

                var parameters = new EffectParameter[count];
			    for (var i = 0; i < count; i++)
			    {
				    var class_ = (EffectParameterClass)ReadByte();				
                    var type = (EffectParameterType)ReadByte();
				    var name = ReadString();
				    var semantic = ReadString();
				    var annotations = ReadAnnotations();
				    var rowCount = (int)ReadByte();
				    var columnCount = (int)ReadByte();

                    var elements = ReadParameters();
                    var structMembers = ReadParameters();

				    object data = null;
				    if (elements.Count == 0 && structMembers.Count == 0)
				    {
					    switch (type)
					    {						
                            case EffectParameterType.Bool:
                            case EffectParameterType.Int32:
#if !OPENGL
                                // Under most platforms we properly store integers and 
                                // booleans in an integer type.
                                //
                                // MojoShader on the otherhand stores everything in float
                                // types which is why this code is disabled under OpenGL.
					            {
					                var buffer = new int[rowCount * columnCount];								
                                    for (var j = 0; j < buffer.Length; j++)
                                        buffer[j] = ReadInt32();
                                    data = buffer;
                                    break;
					            }
#endif

						    case EffectParameterType.Single:
							    {
								    var buffer = new float[rowCount * columnCount];
								    for (var j = 0; j < buffer.Length; j++)
                                        buffer[j] = ReadSingle();
                                    data = buffer;
                                    break;
                                }

                            case EffectParameterType.String:
                                // TODO: We have not investigated what a string
                                // type should do in the parameter list.  Till then
                                // throw to let the user know.
							    throw new NotSupportedException();

                            default:
                                // NOTE: We skip over all other types as they 
                                // don't get added to the constant buffer.
					            break;
					    }
                    }

				    parameters[i] = new EffectParameter(
					    class_, type, name, rowCount, columnCount, semantic, 
					    annotations, elements, structMembers, data);
			    }

			    return new EffectParameterCollection(parameters);
		    }

            private int ReadPackedInt()
            {
                unchecked
                {
                    // read zigzag encoded int
                    int zzint = Read7BitEncodedInt();
                    return ((int)((uint)zzint >> 1) ^ (-(zzint & 1)));
                }
            }
        }

        #endregion // Effect File Reader
    }
}
