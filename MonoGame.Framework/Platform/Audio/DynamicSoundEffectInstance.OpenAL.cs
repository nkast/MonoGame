// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Platform.Audio.OpenAL;

namespace Microsoft.Xna.Platform.Audio
{
    public sealed partial class ConcreteDynamicSoundEffectInstance : ConcreteSoundEffectInstance
        , IDynamicSoundEffectInstanceStrategy
    {  
        private int d_sampleRate;
        private AudioChannels d_channels;
        private ALFormat _format;
        private Queue<OALSoundBuffer> _queuedBuffers = new Queue<OALSoundBuffer>();

        public event EventHandler<EventArgs> OnBufferNeeded;

        internal ConcreteDynamicSoundEffectInstance(AudioServiceStrategy audioServiceStrategy, int sampleRate, AudioChannels channels, float pan)
            : base(audioServiceStrategy, null, pan)
        {
            d_sampleRate = sampleRate;
            d_channels = channels;
            _format = channels == AudioChannels.Mono ? ALFormat.Mono16 : ALFormat.Stereo16;

            _sourceId = ConcreteAudioService.ReserveSource();
        }

        public int DynamicPlatformGetPendingBufferCount()
        {
            return _queuedBuffers.Count;
        }

        internal override  void PlatformPlay(bool isLooped, float pitch)
        {
            // Ensure that the source is not looped (due to source recycling)
            AL.Source(_sourceId, ALSourceb.Looping, isLooped);
            ALHelper.CheckError("Failed to set source loop state.");

            AL.SourcePlay(_sourceId);
            ALHelper.CheckError("Failed to play the source.");
        }

        internal override  void PlatformPause()
        {            
            AL.SourcePause(_sourceId);
            ALHelper.CheckError("Failed to pause the source.");
        }

        internal override  void PlatformResume(bool isLooped)
        {
            AL.SourcePlay(_sourceId);
            ALHelper.CheckError("Failed to play the source.");
        }

        internal override  void PlatformStop()
        {
            AL.SourceStop(_sourceId);
            ALHelper.CheckError("Failed to stop the source.");

            // Remove all queued buffers
            AL.Source(_sourceId, ALSourcei.Buffer, 0);
            while (_queuedBuffers.Count > 0)
            {
                var buffer = _queuedBuffers.Dequeue();
                buffer.Dispose();
            }
        }

        public void DynamicPlatformSubmitBuffer(byte[] buffer, int offset, int count, SoundState state)
        {
            // Get a buffer
            OALSoundBuffer oalBuffer = new OALSoundBuffer(AudioService.Current);

            // Bind the data
            if (offset == 0)
            {
                oalBuffer.BindDataBuffer(buffer, _format, count, d_sampleRate);
            }
            else
            {
                // BindDataBuffer does not support offset
                var offsetBuffer = new byte[count];
                Array.Copy(buffer, offset, offsetBuffer, 0, count);
                oalBuffer.BindDataBuffer(offsetBuffer, _format, count, d_sampleRate);
            }

            // Queue the buffer
            _queuedBuffers.Enqueue(oalBuffer);
            AL.SourceQueueBuffer(_sourceId, oalBuffer.OpenALDataBuffer);
            ALHelper.CheckError("Failed to queue the buffer.");

            // If the source has run out of buffers, restart it
            var sourceState = AL.GetSourceState(_sourceId);
            if (state == SoundState.Playing && sourceState == ALSourceState.Stopped)
            {
                AL.SourcePlay(_sourceId);
                ALHelper.CheckError("Failed to resume source playback.");
            }
        }

        public void DynamicPlatformUpdateQueue()
        {
            // Get the completed buffers
            AL.GetError();
            int numBuffers;
            AL.GetSource(_sourceId, ALGetSourcei.BuffersProcessed, out numBuffers);
            ALHelper.CheckError("Failed to get processed buffer count.");

            // Unqueue them
            if (numBuffers > 0)
            {
                AL.SourceUnqueueBuffers(_sourceId, numBuffers);
                ALHelper.CheckError("Failed to unqueue buffers.");
                for (int i = 0; i < numBuffers; i++)
                {
                    var buffer = _queuedBuffers.Dequeue();
                    buffer.Dispose();
                }
            }

            // Raise the event
            var handler = OnBufferNeeded;
            if (handler != null)
            {
               // Raise the event for each removed buffer, if needed
               for (int i = 0; i < numBuffers; i++)
                   handler(this, EventArgs.Empty);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                while (_queuedBuffers.Count > 0)
                {
                    var buffer = _queuedBuffers.Dequeue();
                    buffer.Dispose();
                }
            }

            base.Dispose(disposing);
        }

    }
}
