// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using MonoGame.OpenAL;

namespace Microsoft.Xna.Framework.Audio
{
    internal class OALSoundBuffer : IDisposable
    {
        internal AudioService _audioService { get; private set; }
        int openALDataBuffer;
        ALFormat openALFormat;
        int dataSize;
        bool _isDisposed;

        public int OpenALDataBuffer { get { return openALDataBuffer; } }

        public double Duration { get; set; }

        public OALSoundBuffer(AudioService audioService)
        {
            if (audioService == null)
                throw new ArgumentNullException("audioService");

            _audioService = audioService;
            _audioService.Disposing += _audioService_Disposing;

            openALDataBuffer = AL.GenBuffer();
            ALHelper.CheckError("Failed to generate OpenAL data buffer.");
        }

        ~OALSoundBuffer()
        {
            Dispose(false);
        }


        public void BindDataBuffer(byte[] dataBuffer, ALFormat format, int size, int sampleRate, int sampleAlignment = 0)
        {
            if ((format == ALFormat.MonoMSAdpcm || format == ALFormat.StereoMSAdpcm) && !AudioService.Current.SupportsAdpcm)
                throw new InvalidOperationException("MS-ADPCM is not supported by this OpenAL driver");
            if ((format == ALFormat.MonoIma4 || format == ALFormat.StereoIma4) && !AudioService.Current.SupportsIma4)
                throw new InvalidOperationException("IMA/ADPCM is not supported by this OpenAL driver");

            openALFormat = format;
            dataSize = size;
            int unpackedSize = 0;

            if (sampleAlignment > 0)
            {
                AL.Bufferi(openALDataBuffer, ALBufferi.UnpackBlockAlignmentSoft, sampleAlignment);
                ALHelper.CheckError("Failed to fill buffer.");
            }

            AL.BufferData(openALDataBuffer, openALFormat, dataBuffer, size, sampleRate);
            ALHelper.CheckError("Failed to fill buffer.");

            int bits, channels;
            Duration = -1;
            AL.GetBuffer(openALDataBuffer, ALGetBufferi.Bits, out bits);
            ALHelper.CheckError("Failed to get buffer bits");
            AL.GetBuffer(openALDataBuffer, ALGetBufferi.Channels, out channels);
            ALHelper.CheckError("Failed to get buffer channels");
            AL.GetBuffer(openALDataBuffer, ALGetBufferi.Size, out unpackedSize);
            ALHelper.CheckError("Failed to get buffer size");
            Duration = (float)(unpackedSize / ((bits / 8) * channels)) / (float)sampleRate;
        }

        private void _audioService_Disposing(object sender, EventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                // Clean up managed objects
            }

            // Release unmanaged resources
            AL.DeleteBuffer(openALDataBuffer);
            ALHelper.CheckError("Failed to delete buffer.");
            openALDataBuffer = 0;

            _audioService.Disposing -= _audioService_Disposing;
            _audioService = null;

            _isDisposed = true;
        }
    }
}
