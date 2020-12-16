using System.IO;

namespace Microsoft.Xna.Framework.Content.Pipeline.EffectCompiler
{
    internal partial class ConstantBufferData
    {
        public void Write(int version, BinaryWriter writer, Options options)
        {
            writer.Write(Name);

            writer.Write((ushort)Size);

            if (version == 9)
                writer.Write((byte)ParameterIndex.Count);
            else
                writer.Write(ParameterIndex.Count);
            for (var i=0; i < ParameterIndex.Count; i++)
            {
                if (version == 9)
                    writer.Write((byte)ParameterIndex[i]);
                else
                    writer.Write(ParameterIndex[i]);
                writer.Write((ushort)ParameterOffset[i]);
            }
        }
    }
}
