// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using System.IO;

namespace Microsoft.Xna.Framework.Audio
{
    partial class SoundEffect
    {

        #region Initialization


        private void PlatformInitializePcm(byte[] buffer, int offset, int count, int sampleBits, int sampleRate, AudioChannels channels, int loopStart, int loopLength)
        {
        }

        private void PlatformInitializeFormat(byte[] header, byte[] buffer, int bufferSize, int loopStart, int loopLength)
        {
        }

        private void PlatformInitializeXact(MiniFormatTag codec, byte[] buffer, int channels, int sampleRate, int blockAlignment, int loopStart, int loopLength, out TimeSpan duration)
        {
            throw new NotSupportedException("Unsupported sound format!");
        }

        private void PlatformLoadAudioStream(Stream stream, out TimeSpan duration)
        {
            duration = TimeSpan.Zero;
        }

        #endregion

        private void PlatformDispose(bool disposing)
        {
        }

    }
}

