// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Framework.Utilities;
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace Microsoft.Xna.Platform.Audio
{
    public sealed partial class ConcreteDynamicSoundEffectInstance : ConcreteSoundEffectInstance
        , IDynamicSoundEffectInstanceStrategy
    {
        private int d_sampleRate;
        private AudioChannels d_channels;
        private Queue<AudioBuffer> _queuedBuffers = new Queue<AudioBuffer>();
        private Queue<byte[]> _pooledBuffers = new Queue<byte[]>();
        private static ByteBufferPool _bufferPool = new ByteBufferPool();

        public event EventHandler<EventArgs> OnBufferNeeded;

        internal ConcreteDynamicSoundEffectInstance(AudioServiceStrategy audioServiceStrategy, int sampleRate, AudioChannels channels, float pan)
            : base(audioServiceStrategy, null, pan)
        {
            d_sampleRate = sampleRate;
            d_channels = channels;
            var format = new WaveFormat(sampleRate, (int)channels);

            _voice = new SourceVoice(ConcreteAudioService.Device, format, true);
            _voice.BufferEnd += OnBufferEnd;
        }

        public int DynamicPlatformGetPendingBufferCount()
        {
            return _queuedBuffers.Count;
        }

        internal override void PlatformPlay(bool isLooped, float pitch)
        {
            _voice.Start();
        }

        internal override void PlatformPause()
        {
            _voice.Stop();
        }

        internal override void PlatformResume(bool isLooped)
        {
            _voice.Start();
        }

        internal override void PlatformStop()
        {
            _voice.Stop();

            // Dequeue all the submitted buffers
            _voice.FlushSourceBuffers();

            while (_queuedBuffers.Count > 0)
            {
                var buffer = _queuedBuffers.Dequeue();
                buffer.Stream.Dispose();
                _bufferPool.Return(_pooledBuffers.Dequeue());
            }
        }

        public void DynamicPlatformSubmitBuffer(byte[] buffer, int offset, int count, SoundState state)
        {
            // we need to copy so datastream does not pin the buffer that the user might modify later
            byte[] pooledBuffer;
            pooledBuffer = _bufferPool.Get(count);
            _pooledBuffers.Enqueue(pooledBuffer);
            Buffer.BlockCopy(buffer, offset, pooledBuffer, 0, count);

            var stream = DataStream.Create(pooledBuffer, true, false, 0, true);
            var audioBuffer = new AudioBuffer(stream);
            audioBuffer.AudioBytes = count;

            _voice.SubmitSourceBuffer(audioBuffer, null);
            _queuedBuffers.Enqueue(audioBuffer);
        }

        public void DynamicPlatformUpdateQueue()
        {
            // The XAudio implementation utilizes callbacks, so no work here.
        }


        private void OnBufferEnd(IntPtr obj)
        {
            // Release the buffer
            if (_queuedBuffers.Count > 0)
            {
                var buffer = _queuedBuffers.Dequeue();
                buffer.Stream.Dispose();
                _bufferPool.Return(_pooledBuffers.Dequeue());
            }

            // Raise the event
            var handler = OnBufferNeeded;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                while (_queuedBuffers.Count > 0)
                {
                    var buffer = _queuedBuffers.Dequeue();
                    buffer.Stream.Dispose();
                    _bufferPool.Return(_pooledBuffers.Dequeue());
                }
            }
            
            base.Dispose(disposing);
        }

    }
}
