// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.Utilities;

namespace Microsoft.Xna.Framework.Content.Pipeline.EffectCompiler
{
	internal partial class EffectObject
	{

        private const string Header = "MGFX";
        private const int Version = 9;

        /// <summary>
        /// Writes the effect for loading later.
        /// </summary>
        public void Write(BinaryWriter writer, Options options)
        {
            // Write a very simple header for identification and versioning.
            writer.Write(Header.ToCharArray());
            writer.Write((byte)Version);

            // Write an simple identifier for DX11 vs GLSL
            // so we can easily detect the correct shader type.
            var profile = (byte)options.Profile.FormatId;
            writer.Write(profile);

            // Write the rest to a memory stream.
            using(MemoryStream memStream = new MemoryStream())
            using(BinaryWriter memWriter = new BinaryWriter(memStream))
            {
            // Write all the constant buffers.
            if (Version == 9)
                memWriter.Write((byte)ConstantBuffers.Count);
            else
                memWriter.Write(ConstantBuffers.Count);
            foreach (var cbuffer in ConstantBuffers)
                    cbuffer.Write(Version, memWriter, options);

            // Write all the shaders.
            if (Version == 9)
                memWriter.Write((byte)Shaders.Count);
            else
                memWriter.Write(Shaders.Count);
            foreach (var shader in Shaders)
                    shader.Write(memWriter, options);

            // Write the parameters.
                WriteParameters(memWriter, Parameters, Parameters.Length);

            // Write the techniques.
            var isExtTechniques = false;
            if (Version == 9)
            {
                isExtTechniques = (Techniques.Length > byte.MaxValue);
                if (isExtTechniques)
                {
                    // Value 0 for techniqueCount is reserved.
                    // This is an extension to the format to support more than 255 techniques.
                    memWriter.Write((byte)0);
                    Write7BitEncodedInt(writer, Techniques.Length);
                }
                else
                {
                    memWriter.Write((byte)Techniques.Length);
                }
            }
            else
                memWriter.Write(Techniques.Length);
            foreach (var technique in Techniques)
            {
                    memWriter.Write(technique.name);
                    WriteAnnotations(memWriter, technique.annotation_handles);

                // Write the passes.
                if (Version == 9)
                    memWriter.Write((byte)technique.pass_count);
                else
                    memWriter.Write((int)technique.pass_count);
                for (var p = 0; p < technique.pass_count; p++)
                {
                    var pass = technique.pass_handles[p];

                        memWriter.Write(pass.name);
                        WriteAnnotations(memWriter, pass.annotation_handles);

                    // Write the index for the vertex and pixel shaders.
                        var vertexShaderIndex = GetShaderIndex(STATE_CLASS.VERTEXSHADER, pass.states);
                        var pixelShaderIndex = GetShaderIndex(STATE_CLASS.PIXELSHADER, pass.states);
                        if (isExtTechniques)
                        {
                            WritePackedInt(memWriter, vertexShaderIndex);
                            WritePackedInt(memWriter, pixelShaderIndex);
                        }
                        else
                        {
                            memWriter.Write((byte)vertexShaderIndex);
                            memWriter.Write((byte)pixelShaderIndex);
                        }

                    // Write the state objects too!
					if (pass.blendState != null)
					{
                            memWriter.Write(true);
                            memWriter.Write((byte)pass.blendState.AlphaBlendFunction);
                            memWriter.Write((byte)pass.blendState.AlphaDestinationBlend);
                            memWriter.Write((byte)pass.blendState.AlphaSourceBlend);
                            memWriter.Write(pass.blendState.BlendFactor.R);
                            memWriter.Write(pass.blendState.BlendFactor.G);
                            memWriter.Write(pass.blendState.BlendFactor.B);
                            memWriter.Write(pass.blendState.BlendFactor.A);
                            memWriter.Write((byte)pass.blendState.ColorBlendFunction);
                            memWriter.Write((byte)pass.blendState.ColorDestinationBlend);
                            memWriter.Write((byte)pass.blendState.ColorSourceBlend);
                            memWriter.Write((byte)pass.blendState.ColorWriteChannels);
                            memWriter.Write((byte)pass.blendState.ColorWriteChannels1);
                            memWriter.Write((byte)pass.blendState.ColorWriteChannels2);
                            memWriter.Write((byte)pass.blendState.ColorWriteChannels3);
                            memWriter.Write(pass.blendState.MultiSampleMask);
					}
					else
                            memWriter.Write(false);

					if (pass.depthStencilState != null)
					{
                            memWriter.Write(true);
                            memWriter.Write((byte)pass.depthStencilState.CounterClockwiseStencilDepthBufferFail);
                            memWriter.Write((byte)pass.depthStencilState.CounterClockwiseStencilFail);
                            memWriter.Write((byte)pass.depthStencilState.CounterClockwiseStencilFunction);
                            memWriter.Write((byte)pass.depthStencilState.CounterClockwiseStencilPass);
                            memWriter.Write(pass.depthStencilState.DepthBufferEnable);
                            memWriter.Write((byte)pass.depthStencilState.DepthBufferFunction);
                            memWriter.Write(pass.depthStencilState.DepthBufferWriteEnable);
                            memWriter.Write(pass.depthStencilState.ReferenceStencil);
                            memWriter.Write((byte)pass.depthStencilState.StencilDepthBufferFail);
                            memWriter.Write(pass.depthStencilState.StencilEnable);
                            memWriter.Write((byte)pass.depthStencilState.StencilFail);
                            memWriter.Write((byte)pass.depthStencilState.StencilFunction);
                            memWriter.Write(pass.depthStencilState.StencilMask);
                            memWriter.Write((byte)pass.depthStencilState.StencilPass);
                            memWriter.Write(pass.depthStencilState.StencilWriteMask);
                            memWriter.Write(pass.depthStencilState.TwoSidedStencilMode);
					}
					else
                            memWriter.Write(false);

					if (pass.rasterizerState != null)
					{
                            memWriter.Write(true);
                            memWriter.Write((byte)pass.rasterizerState.CullMode);
                            memWriter.Write(pass.rasterizerState.DepthBias);
                            memWriter.Write((byte)pass.rasterizerState.FillMode);
                            memWriter.Write(pass.rasterizerState.MultiSampleAntiAlias);
                            memWriter.Write(pass.rasterizerState.ScissorTestEnable);
                            memWriter.Write(pass.rasterizerState.SlopeScaleDepthBias);
					}
					else
                            memWriter.Write(false);
                    }
                }

                // Calculate a hash code from memory stream
                // and write it to the header.
                var effectKey = MonoGame.Framework.Utilities.Hash.ComputeHash(memStream);
                writer.Write((Int32)effectKey);

                //write content from memory stream to final stream.
                memStream.WriteTo(writer.BaseStream);
            }

            // Write a tail to be used by the reader for validation.
            if (Version >= 9)
                writer.Write(Header.ToCharArray());
        }

        private static void WriteParameters(BinaryWriter writer, d3dx_parameter[] parameters, int count)
        {
            if (Version <= 8)
                writer.Write((byte)count);
            else
            if (Version == 9)
                Write7BitEncodedInt(writer, count);
            else
                writer.Write(count);
            for (var i = 0; i < count; i++)
                WriteParameter(writer, parameters[i]);
        }

        private static void WriteParameter(BinaryWriter writer, d3dx_parameter param)
        {
            var class_ = ToXNAParameterClass(param.class_);
            var type = ToXNAParameterType(param.type);
            writer.Write((byte)class_);
            writer.Write((byte)type);

            writer.Write(param.name);
            writer.Write(param.semantic);
            WriteAnnotations(writer, param.annotation_handles);

            writer.Write((byte)param.rows);
            writer.Write((byte)param.columns);

            // Write the elements or struct members.
            WriteParameters(writer, param.member_handles, (int)param.element_count);
            WriteParameters(writer, param.member_handles, (int)param.member_count);

            if (param.element_count == 0 && param.member_count == 0)
            {
                switch (type)
                {
                    case EffectParameterType.Bool:
                    case EffectParameterType.Int32:
                    case EffectParameterType.Single:
                        writer.Write((byte[])param.data);
                        break;
                }
            }
        }

        private static void WriteAnnotations(BinaryWriter writer, d3dx_parameter[] annotations)
        {
            var count = annotations == null ? 0 : annotations.Length;
            if (Version == 9)
                writer.Write((byte)count);
            else
                writer.Write(count);
            for (var i = 0; i < count; i++)
                WriteParameter(writer, annotations[i]);
        }
        
        protected static void WritePackedInt(BinaryWriter writer, int value)
        {
            // write zigzag encoded int
            int zzint = ((value << 1) ^ (value >> 31));
            Write7BitEncodedInt(writer, zzint);
        }

        protected static void Write7BitEncodedInt(BinaryWriter writer, int value)
        {
            unchecked
            {
                do
                {
                    var value7bit = (byte)(value & 0x7f);
                    value = (int)((uint)value >> 7);
                    if (value != 0)
                        value7bit |= 0x80;
                    writer.Write(value7bit);
                }
                while (value != 0);
            }
        }
	}
}

