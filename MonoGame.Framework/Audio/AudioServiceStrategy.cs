// Copyright (C)2021 Nick Kastellanos

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;

namespace Microsoft.Xna.Platform.Audio
{
    abstract public class AudioServiceStrategy : IDisposable
    {
        // factory methods
        internal abstract SoundEffectInstanceStrategy CreateSoundEffectInstanceStrategy(SoundEffectStrategy _strategy, float _pan);
        internal abstract IDynamicSoundEffectInstanceStrategy CreateDynamicSoundEffectInstanceStrategy(int sampleRate, AudioChannels channels, float pan);

        internal abstract int PlatformGetMaxPlayingInstances();
        internal abstract void PlatformSetReverbSettings(ReverbSettings reverbSettings);

        internal abstract void PlatformPopulateCaptureDevices(List<Microphone> microphones, ref Microphone defaultMicrophone);
        

        #region IDisposable
        ~AudioServiceStrategy()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
        #endregion
    }

    abstract public class MicrophoneStrategy
    {
        internal abstract void PlatformStart(string deviceName, int sampleRate, int sampleSizeInBytes);
        internal abstract void PlatformStop();
        internal abstract bool PlatformUpdateBuffer();
        internal abstract bool PlatformIsHeadset();
        internal abstract int PlatformGetData(byte[] buffer, int offset, int count);
    }

    abstract public class SoundEffectStrategy : IDisposable
    {
        internal abstract void PlatformLoadAudioStream(Stream stream, out TimeSpan duration);
        internal abstract void PlatformInitializePcm(byte[] buffer, int offset, int count, int sampleBits, int sampleRate, AudioChannels channels, int loopStart, int loopLength);
        internal abstract void PlatformInitializeXact(MiniFormatTag codec, byte[] buffer, int channels, int sampleRate, int blockAlignment, int loopStart, int loopLength, out TimeSpan duration);
        internal abstract void PlatformInitializeFormat(byte[] header, byte[] buffer, int bufferSize, int loopStart, int loopLength);

        #region IDisposable
        ~SoundEffectStrategy()
        {
           Dispose(false);
         }

        public void Dispose()
        {
            Dispose(true);            
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
        #endregion
    }

    abstract public class SoundEffectInstanceStrategy : IDisposable
    {
        internal SoundEffectInstanceStrategy(AudioServiceStrategy audioServiceStrategy, SoundEffectStrategy sfxStrategy, float pan) { }

        internal abstract void PlatformApply3D(AudioListener listener, AudioEmitter emitter);
        internal abstract void PlatformSetIsLooped(SoundState state, bool isLooped);
        internal abstract void PlatformSetPan(float pan);
        internal abstract void PlatformSetPitch(float pitch);
        internal abstract void PlatformSetVolume(float value);
        internal abstract bool PlatformUpdateState(ref SoundState state);
        
        internal abstract void PlatformPause();
        internal abstract void PlatformPlay(bool isLooped, float pitch);
        internal abstract void PlatformResume(bool isLooped);
        internal abstract void PlatformStop();

        internal abstract void PlatformRelease(bool isLooped);

        internal abstract void PlatformReuseInstance(ref SoundEffect currentEffect, SoundEffect newEffect, float pan);

        internal abstract void PlatformSetReverbMix(SoundState state, float mix, float pan);
        internal abstract void PlatformSetFilter(SoundState state, FilterMode mode, float filterQ, float frequency);
        internal abstract void PlatformClearFilter();

        #region IDisposable
        ~SoundEffectInstanceStrategy()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
        #endregion
    }

    public interface IDynamicSoundEffectInstanceStrategy
    {
        event EventHandler<EventArgs> OnBufferNeeded;

        void DynamicPlatformSubmitBuffer(byte[] buffer, int offset, int count, SoundState state);
        int DynamicPlatformGetPendingBufferCount();
        void DynamicPlatformUpdateQueue();
    }
}
