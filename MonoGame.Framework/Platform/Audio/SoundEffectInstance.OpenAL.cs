// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using MonoGame.OpenAL;

namespace Microsoft.Xna.Framework.Audio
{
    public partial class SoundEffectInstance
    {
        float _alVolume = 1f;

        internal int _sourceId;
        float reverb;
        bool applyFilter = false;
        EfxFilterType filterType;
        float filterQ;
        float frequency;
        // emmiter's position/velocity relative to the listener
        Vector3 _relativePosition;
        Vector3 _relativeVelocity;
        
        #region Initialization

        internal void PlatformConstruct()
        {
        }

        internal void PlatformReuseInstance(SoundEffect newEffect)
        {
        }

        #endregion // Initialization

        /// <summary>
        /// Converts the XNA [-1, 1] pitch range to OpenAL pitch (0, INF) or Android SoundPool playback rate [0.5, 2].
        /// <param name="xnaPitch">The pitch of the sound in the Microsoft XNA range.</param>
        /// </summary>
        private static float XnaPitchToAlPitch(float xnaPitch)
        {
            return (float)Math.Pow(2, xnaPitch);
        }

        private void PlatformApply3D(AudioListener listener, AudioEmitter emitter)
        {
            // set up matrix to transform world space coordinates to listener space coordinates
            Matrix worldSpaceToListenerSpace = Matrix.Transpose(Matrix.CreateWorld(listener.Position, listener.Forward, listener.Up));
            // set up our final position and velocity according to orientation of listener
            _relativePosition = emitter.Position;
            Vector3.Transform(ref _relativePosition, ref worldSpaceToListenerSpace, out _relativePosition);
            _relativeVelocity = emitter.Velocity - listener.Velocity;
            Vector3.TransformNormal(ref _relativeVelocity, ref worldSpaceToListenerSpace, out _relativeVelocity);

            if (_sourceId != 0)
            {
                // set the position based on relative position
                AL.Source(_sourceId, ALSource3f.Position, ref _relativePosition);
                ALHelper.CheckError("Failed to set source position.");
                AL.Source(_sourceId, ALSource3f.Velocity, ref _relativeVelocity);
                ALHelper.CheckError("Failed to set source velocity.");
                AL.Source(_sourceId, ALSourcef.ReferenceDistance, SoundEffect.DistanceScale);
                ALHelper.CheckError("Failed to set source distance scale.");
                AL.DopplerFactor(SoundEffect.DopplerScale);
                ALHelper.CheckError("Failed to set Doppler scale.");
            }
        }

        private void PlatformPause()
        {
            AL.SourcePause(_sourceId);
            ALHelper.CheckError("Failed to pause source.");
        }

        private void PlatformPlay()
        {
            _sourceId = _audioService.ReserveSource();

            // bind buffer to source
            int bufferId = _effect.GetALSoundBuffer().OpenALDataBuffer;
            AL.Source(_sourceId, ALSourcei.Buffer, bufferId);
            ALHelper.CheckError("Failed to bind buffer to source.");

            // Send the position, gain, looping, pitch, and distance model to the OpenAL driver.

            AL.Source(_sourceId, ALSourcei.SourceRelative, 1);
            ALHelper.CheckError("Failed set source relative.");
            // Distance Model
            AL.DistanceModel (ALDistanceModel.InverseDistanceClamped);
            ALHelper.CheckError("Failed set source distance.");
            // Position/Pan
            AL.Source (_sourceId, ALSource3f.Position, ref _relativePosition);
            ALHelper.CheckError("Failed to set source position/pan.");
            // Velocity
            AL.Source (_sourceId, ALSource3f.Velocity, ref _relativeVelocity);
            ALHelper.CheckError("Failed to set source pan.");
            // Distance Scale
            AL.Source(_sourceId, ALSourcef.ReferenceDistance, SoundEffect.DistanceScale);
            ALHelper.CheckError("Failed to set source distance scale.");
            // Doppler Scale
            AL.DopplerFactor(SoundEffect.DopplerScale);
            ALHelper.CheckError("Failed to set Doppler scale.");
            // Volume
            AL.Source(_sourceId, ALSourcef.Gain, _alVolume);
            ALHelper.CheckError("Failed to set source volume.");
            // Looping
            AL.Source (_sourceId, ALSourceb.Looping, IsLooped);
            ALHelper.CheckError("Failed to set source loop state.");
            // Pitch
            AL.Source (_sourceId, ALSourcef.Pitch, XnaPitchToAlPitch(_pitch));
            ALHelper.CheckError("Failed to set source pitch.");

            ApplyReverb ();
            ApplyFilter ();

            AL.SourcePlay(_sourceId);
            ALHelper.CheckError("Failed to play source.");
        }

        private void PlatformResume()
        {
            AL.SourcePlay(_sourceId);
            ALHelper.CheckError("Failed to play source.");
        }

        private void PlatformStop()
        {
            AL.SourceStop(_sourceId);
            ALHelper.CheckError("Failed to stop source.");
            _state = SoundState.Stopped;

            // Reset the SendFilter to 0 if we are NOT using reverb since
            // sources are recycled
            if (AudioService.Current.SupportsEfx)
            {
                AudioService.Current.Efx.BindSourceToAuxiliarySlot(_sourceId, 0, 0, 0);
                ALHelper.CheckError("Failed to unset reverb.");
                AL.Source(_sourceId, ALSourcei.EfxDirectFilter, 0);
                ALHelper.CheckError("Failed to unset filter.");
            }

            _audioService.RecycleSource(_sourceId);
            _sourceId = 0;
        }

        private void PlatformRelease()
        {
            if (_isLooped)
            {
                AL.Source(_sourceId, ALSourceb.Looping, false);
                ALHelper.CheckError("Failed to set source loop state.");
            }
        }

        internal void PlatformUpdateState()
        {
            // check if the sound has stopped
            if (_state == SoundState.Playing)
            {
                var alState = AL.GetSourceState(_sourceId);
                ALHelper.CheckError("Failed to get source state.");

                if (alState == ALSourceState.Stopped)
                {
                    // update instance
                    PlatformStop();
                    _state = SoundState.Stopped;

                    _audioService.RemovePlayingInstance(this);
                    _audioService.AddPooledInstance(this);
                }
            }
        }

        private void PlatformSetIsLooped(bool isLooped)
        {
            if (_sourceId != 0)
            {
                AL.Source(_sourceId, ALSourceb.Looping, isLooped);
                ALHelper.CheckError("Failed to set source loop state.");
            }
        }

        private void PlatformSetPan(float value)
        {
            // OpenAL doesn't support Panning. We emulate it using 3D audio.
            // If the user set both Pan and Apply3D(), only the last call takes effect.
            _relativePosition.X = (float)Math.Sin(value * MathHelper.PiOver2) * SoundEffect.DistanceScale;
            _relativePosition.Y = (float)Math.Cos(value * MathHelper.PiOver2) * SoundEffect.DistanceScale;
            _relativePosition.Z = 0f;

            if (_sourceId != 0)
            {
                AL.Source(_sourceId, ALSource3f.Position, ref _relativePosition);
                ALHelper.CheckError("Failed to set source pan.");
            }
        }

        private void PlatformSetPitch(float value)
        {
            if (_sourceId != 0)
            {
                AL.Source(_sourceId, ALSourcef.Pitch, XnaPitchToAlPitch(value));
                ALHelper.CheckError("Failed to set source pitch.");
            }
        }

        private void PlatformSetVolume(float value)
        {
            _alVolume = value;

            if (_sourceId != 0)
            {
                AL.Source(_sourceId, ALSourcef.Gain, _alVolume);
                ALHelper.CheckError("Failed to set source volume.");
            }
        }

        internal void PlatformSetReverbMix(float mix)
        {
            if (!AudioService.Current.Efx.IsInitialized)
                return;
            reverb = mix;
            if (State == SoundState.Playing)
            {
                ApplyReverb();
                reverb = 0f;
            }
        }

        void ApplyReverb()
        {
            if (reverb > 0f && AudioService.Current.ReverbSlot != 0)
            {
                AudioService.Current.Efx.BindSourceToAuxiliarySlot(_sourceId, AudioService.Current.ReverbSlot, 0, 0);
                ALHelper.CheckError("Failed to set reverb.");
            }
        }

        void ApplyFilter()
        {
            if (applyFilter && _audioService.Filter > 0)
            {
                var freq = frequency / 20000f;
                var lf = 1.0f - freq;
                var efx = AudioService.Current.Efx;
                efx.Filter(_audioService.Filter, EfxFilteri.FilterType, (int)filterType);
                ALHelper.CheckError("Failed to set filter.");
                switch (filterType)
                {
                case EfxFilterType.Lowpass:
                    efx.Filter(_audioService.Filter, EfxFilterf.LowpassGainHF, freq);
                    ALHelper.CheckError("Failed to set LowpassGainHF.");
                    break;
                case EfxFilterType.Highpass:
                    efx.Filter(_audioService.Filter, EfxFilterf.HighpassGainLF, freq);
                    ALHelper.CheckError("Failed to set HighpassGainLF.");
                    break;
                case EfxFilterType.Bandpass:
                    efx.Filter(_audioService.Filter, EfxFilterf.BandpassGainHF, freq);
                    ALHelper.CheckError("Failed to set BandpassGainHF.");
                    efx.Filter(_audioService.Filter, EfxFilterf.BandpassGainLF, lf);
                    ALHelper.CheckError("Failed to set BandpassGainLF.");
                    break;
                }
                AL.Source(_sourceId, ALSourcei.EfxDirectFilter, _audioService.Filter);
                ALHelper.CheckError("Failed to set DirectFilter.");
            }
        }

        internal void PlatformSetFilter(FilterMode mode, float filterQ, float frequency)
        {
            if (!AudioService.Current.Efx.IsInitialized)
                return;

            applyFilter = true;
            switch (mode)
            {
            case FilterMode.BandPass:
                filterType = EfxFilterType.Bandpass;
                break;
                case FilterMode.LowPass:
                filterType = EfxFilterType.Lowpass;
                break;
                case FilterMode.HighPass:
                filterType = EfxFilterType.Highpass;
                break;
            }
            this.filterQ = filterQ;
            this.frequency = frequency;
            if (State == SoundState.Playing)
            {
                ApplyFilter();
                applyFilter = false;
            }
        }

        internal void PlatformClearFilter()
        {
            if (!AudioService.Current.Efx.IsInitialized)
                return;

            applyFilter = false;
        }

        private void PlatformDispose(bool disposing)
        {
            
        }
    }
}
