// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;

namespace Microsoft.Xna.Framework.Audio
{
    public partial class SoundEffectInstance
    {

        internal void PlatformConstruct()
        {
        }

        internal void PlatformReuseInstance(SoundEffect newEffect)
        {
        }

        private void PlatformApply3D(AudioListener listener, AudioEmitter emitter)
        {
        }


        internal void PlatformInitialize(byte[] buffer, int sampleRate, int channels)
        {
        }

        private void PlatformPause()
        {
        }

        private void PlatformPlay()
        {
        }

        private void PlatformResume()
        {
        }

        private void PlatformStop()
        {
        }

        private void PlatformRelease()
        {
        }

        internal void PlatformUpdateState()
        {
        }

        private void PlatformSetIsLooped(bool isLooped)
        {
        }

        private void PlatformSetPan(float value)
        {
        }

        private void PlatformSetPitch(float value)
        {
        }

        private void PlatformSetVolume(float value)
        {
        }

        internal void PlatformSetReverbMix(float mix)
        {
        }

        internal void PlatformSetFilter(FilterMode mode, float filterQ, float frequency)
        {
        }

        internal void PlatformClearFilter()
        {
        }

        private void PlatformDispose(bool disposing)
        {
        }
    }
}
