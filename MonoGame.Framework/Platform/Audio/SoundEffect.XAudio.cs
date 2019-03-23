// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using System.IO;
using SharpDX;
using SharpDX.XAudio2;
using SharpDX.Multimedia;

namespace Microsoft.Xna.Framework.Audio
{
    partial class SoundEffect
    {
        internal DataStream _dataStream;
        private AudioBuffer _buffer;
        private AudioBuffer _loopedBuffer;
        internal WaveFormat _format;

        #region Initialization

        private static DataStream ToDataStream(byte[] buffer, int offset, int length)
        {
            // We make a copy because old versions of 
            // DataStream.Create(...) didn't work correctly for offsets.
            var bufferCopy = new byte[length];
            Buffer.BlockCopy(buffer, offset, bufferCopy, 0, length);

            return DataStream.Create(bufferCopy, true, false);
        }

        private void PlatformInitializePcm(byte[] buffer, int offset, int bufferLength, int sampleBits, int sampleRate, AudioChannels channels, int loopStart, int loopLength)
        {
            CreateBuffers(  new WaveFormat(sampleRate, sampleBits, (int)channels),
                            ToDataStream(buffer, offset, bufferLength),
                            loopStart,
                            loopLength);
        }

        private void PlatformInitializeFormat(byte[] header, byte[] buffer, int bufferLength, int loopStart, int loopLength)
        {
            var format = BitConverter.ToInt16(header, 0);
            var channels = BitConverter.ToInt16(header, 2);
            var sampleRate = BitConverter.ToInt32(header, 4);
            var blockAlignment = BitConverter.ToInt16(header, 12);
            var sampleBits = BitConverter.ToInt16(header, 14);

            WaveFormat waveFormat;
            if (format == 1)
                waveFormat = new WaveFormat(sampleRate, sampleBits, channels);
            else if (format == 2)
                waveFormat = new WaveFormatAdpcm(sampleRate, channels, blockAlignment);
            else if (format == 3)
                waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            else
                throw new NotSupportedException("Unsupported wave format!");

            CreateBuffers(  waveFormat,
                            ToDataStream(buffer, 0, bufferLength),
                            loopStart,
                            loopLength);
        }

        private void PlatformInitializeXact(MiniFormatTag codec, byte[] buffer, int channels, int sampleRate, int blockAlignment, int loopStart, int loopLength, out TimeSpan duration)
        {
            if (codec == MiniFormatTag.Adpcm)
            {
                duration = TimeSpan.FromSeconds((float)loopLength / sampleRate);

                CreateBuffers(  new WaveFormatAdpcm(sampleRate, channels, blockAlignment),
                                ToDataStream(buffer, 0, buffer.Length),
                                loopStart,
                                loopLength);

                return;
            }

            throw new NotSupportedException("Unsupported sound format!");
        }

        private void PlatformLoadAudioStream(Stream stream, out TimeSpan duration)
        {
            SoundStream soundStream = null;
            try
            {
                soundStream = new SoundStream(stream);
            }
            catch (InvalidOperationException ex)
            {
                throw new ArgumentException("Ensure that the specified stream contains valid PCM or IEEE Float wave data.", ex);
            }

            var dataStream = soundStream.ToDataStream();
            int sampleCount = 0;
            switch (soundStream.Format.Encoding)
            {
                case WaveFormatEncoding.Adpcm:
                    {
                        var samplesPerBlock = (soundStream.Format.BlockAlign / soundStream.Format.Channels - 7) * 2 + 2;
                        sampleCount = ((int)dataStream.Length / soundStream.Format.BlockAlign) * samplesPerBlock;
                    }
                    break;
                case WaveFormatEncoding.Pcm:
                case WaveFormatEncoding.IeeeFloat:
                    sampleCount = (int)(dataStream.Length / ((soundStream.Format.Channels * soundStream.Format.BitsPerSample) / 8));
                    break;
                default:
                    throw new ArgumentException("Ensure that the specified stream contains valid PCM, MS-ADPCM or IEEE Float wave data.");
            }

            CreateBuffers(soundStream.Format, dataStream, 0, sampleCount);

            duration = TimeSpan.FromSeconds((float)sampleCount / (float)soundStream.Format.SampleRate);
        }

        private void CreateBuffers(WaveFormat format, DataStream dataStream, int loopStart, int loopLength)
        {
            _format = format;
            _dataStream = dataStream;

            _buffer = new AudioBuffer
            {
                Stream = _dataStream,
                AudioBytes = (int)_dataStream.Length,
                Flags = BufferFlags.EndOfStream,
                PlayBegin = loopStart,
                PlayLength = loopLength,
                Context = new IntPtr(42),
            };

            _loopedBuffer = new AudioBuffer
            {
                Stream = _dataStream,
                AudioBytes = (int)_dataStream.Length,
                Flags = BufferFlags.EndOfStream,
                LoopBegin = loopStart,
                LoopLength = loopLength,
                LoopCount = AudioBuffer.LoopInfinite,
                Context = new IntPtr(42),
            };
        }

        #endregion

        internal AudioBuffer GetDXDataBuffer(bool isLooped)
        {
            return isLooped
                ? _loopedBuffer
                : _buffer;
        }

        private void PlatformDispose(bool disposing)
        {
            if (disposing)
            {
                if (_dataStream != null)
                {
                    _dataStream.Dispose();
                    _dataStream = null;
                }
            }

        }

    }
}

