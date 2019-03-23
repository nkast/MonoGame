// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using System.Collections.Generic;
using MonoGame.OpenAL;

namespace Microsoft.Xna.Framework.Audio
{
    public sealed partial class DynamicSoundEffectInstance : SoundEffectInstance
    {
        private Queue<OALSoundBuffer> _queuedBuffers;
        private ALFormat _format;

        private void PlatformConstructDynamic()
        {
            _format = _channels == AudioChannels.Mono ? ALFormat.Mono16 : ALFormat.Stereo16;

            _sourceId = _audioService.ReserveSource();

            _queuedBuffers = new Queue<OALSoundBuffer>();
        }

        private int PlatformGetPendingBufferCount()
        {
            return _queuedBuffers.Count;
        }

        private void PlatformPlay()
        {
            AL.GetError();

            // Ensure that the source is not looped (due to source recycling)
            AL.Source(_sourceId, ALSourceb.Looping, false);
            ALHelper.CheckError("Failed to set source loop state.");

            AL.SourcePlay(_sourceId);
            ALHelper.CheckError("Failed to play the source.");
        }

        private void PlatformPause()
        {
            AL.GetError();
            AL.SourcePause(_sourceId);
            ALHelper.CheckError("Failed to pause the source.");
        }

        private void PlatformResume()
        {
            AL.GetError();
            AL.SourcePlay(_sourceId);
            ALHelper.CheckError("Failed to play the source.");
        }

        private void PlatformStop()
        {
            AL.GetError();
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

        private void PlatformSubmitBuffer(byte[] buffer, int offset, int count)
        {
            // Get a buffer
            OALSoundBuffer oalBuffer = new OALSoundBuffer(AudioService.Current);

            // Bind the data
            if (offset == 0)
            {
                oalBuffer.BindDataBuffer(buffer, _format, count, _sampleRate);
            }
            else
            {
                // BindDataBuffer does not support offset
                var offsetBuffer = new byte[count];
                Array.Copy(buffer, offset, offsetBuffer, 0, count);
                oalBuffer.BindDataBuffer(offsetBuffer, _format, count, _sampleRate);
            }

            // Queue the buffer
            _queuedBuffers.Enqueue(oalBuffer);
            AL.SourceQueueBuffer(_sourceId, oalBuffer.OpenALDataBuffer);
            ALHelper.CheckError("Failed to queue the buffer.");

            // If the source has run out of buffers, restart it
            var sourceState = AL.GetSourceState(_sourceId);
            if (_dynamicState == SoundState.Playing && sourceState == ALSourceState.Stopped)
            {
                AL.SourcePlay(_sourceId);
                ALHelper.CheckError("Failed to resume source playback.");
            }
        }

        private void PlatformDispose(bool disposing)
        {
            // SFXI disposal handles buffer detachment and source recycling
            base.Dispose(disposing);

            if (disposing)
            {
                while (_queuedBuffers.Count > 0)
                {
                    var buffer = _queuedBuffers.Dequeue();
                    buffer.Dispose();
                }

                _audioService.RemoveDynamicPlayingInstance(this);
            }
        }

        private void PlatformUpdateQueue()
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

            // Raise the event for each removed buffer, if needed
            for (int i = 0; i < numBuffers; i++)
                CheckBufferCount();
        }
    }
}
