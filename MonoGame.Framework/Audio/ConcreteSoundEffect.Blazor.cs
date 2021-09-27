// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;

namespace Microsoft.Xna.Platform.Audio
{
    class ConcreteSoundEffect : SoundEffectStrategy
    {

        #region Initialization

        internal override void PlatformLoadAudioStream(Stream stream, out TimeSpan duration)
        {
            duration = TimeSpan.Zero;
        }

        internal override void PlatformInitializePcm(byte[] buffer, int offset, int bufferLength, int sampleBits, int sampleRate, AudioChannels channels, int loopStart, int loopLength)
        {
        }

        internal override void PlatformInitializeFormat(byte[] header, byte[] buffer, int bufferLength, int loopStart, int loopLength)
        {
        }

        internal override void PlatformInitializeXactAdpcm(byte[] buffer, int channels, int sampleRate, int blockAlignment, int loopStart, int loopLength)
        {
            
        }


        #endregion

#region IDisposable Members

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

        }

#endregion

    }
}

