// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using Microsoft.Xna.Framework.Audio;

namespace Microsoft.Xna.Platform.Audio
{
    public sealed partial class ConcreteDynamicSoundEffectInstance : ConcreteSoundEffectInstance
        , IDynamicSoundEffectInstanceStrategy
    {
        private int d_sampleRate;
        private AudioChannels d_channels;

        public event EventHandler<EventArgs> OnBufferNeeded;

        internal ConcreteDynamicSoundEffectInstance(AudioServiceStrategy audioServiceStrategy, int sampleRate, AudioChannels channels, float pan)
            : base(audioServiceStrategy, null, pan)
        {

        }

        public void DynamicPlatformConstruct(AudioServiceStrategy audioServiceStrategy, int sampleRate, AudioChannels channels)
        {
            ConcreteAudioService = (ConcreteAudioService)audioServiceStrategy;
            d_sampleRate = sampleRate;
            d_channels = channels;
        }

        public int DynamicPlatformGetPendingBufferCount()
        {
            return 0;
        }

        internal override void PlatformPlay(bool isLooped, float pitch)
        {
        }

        internal override void PlatformPause()
        {
        }

        internal override void PlatformResume(bool isLooped)
        {
        }

        internal override void PlatformStop()
        {
        }

        public void DynamicPlatformSubmitBuffer(byte[] buffer, int offset, int count, SoundState state)
        {
        }

        public void DynamicPlatformUpdateQueue()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            base.Dispose(disposing);
        }

    }
}
