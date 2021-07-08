﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using SharpDX.XAudio2;
using SharpDX.X3DAudio;
using SharpDX.Mathematics.Interop;

namespace Microsoft.Xna.Platform.Audio
{
    public class ConcreteSoundEffectInstance : SoundEffectInstanceStrategy
    {
        internal SourceVoice _voice;
        private SharpDX.XAudio2.Fx.Reverb _reverb;
        private float _reverbMix;

        internal AudioServiceStrategy _audioServiceStrategy;
        private ConcreteSoundEffect _concreteSoundEffect;
        internal ConcreteAudioService ConcreteAudioService { get { return (ConcreteAudioService)_audioServiceStrategy; } }

        private static float[] _defaultChannelAzimuths = new float[] { 0f, 0f };
        private static readonly float[] _outputMatrix = new float[16];

        #region Initialization

        internal ConcreteSoundEffectInstance(AudioServiceStrategy audioServiceStrategy, SoundEffectStrategy sfxStrategy, float pan)
            : base(audioServiceStrategy, sfxStrategy, pan)
        {
            _audioServiceStrategy = audioServiceStrategy;
            _concreteSoundEffect = (ConcreteSoundEffect)sfxStrategy;

            _voice = new SourceVoice(ConcreteAudioService.Device, _concreteSoundEffect._format, VoiceFlags.UseFilter, XAudio2.MaximumFrequencyRatio);
            UpdateOutputMatrix(pan); // Ensure the output matrix is set for this new voice
        }

        internal override void PlatformReuseInstance(ref SoundEffect currentEffect, SoundEffect newEffect, float pan)
        {
            if (!ReferenceEquals(currentEffect, newEffect))
            {
                // check if we can reuse voice
                if (_concreteSoundEffect._format.Encoding != ((ConcreteSoundEffect)newEffect._strategy)._format.Encoding ||
                    _concreteSoundEffect._format.Channels != ((ConcreteSoundEffect)newEffect._strategy)._format.Channels ||
                    _concreteSoundEffect._format.SampleRate != ((ConcreteSoundEffect)newEffect._strategy)._format.SampleRate ||
                    _concreteSoundEffect._format.BitsPerSample != ((ConcreteSoundEffect)newEffect._strategy)._format.BitsPerSample)
                {
                    _voice.DestroyVoice();
                    _voice.Dispose();
                    _voice = null;

                    // recreate voice
                    _voice = new SourceVoice(ConcreteAudioService.Device, ((ConcreteSoundEffect)newEffect._strategy)._format, VoiceFlags.UseFilter, XAudio2.MaximumFrequencyRatio);
                    UpdateOutputMatrix(pan); // Ensure the output matrix is set for this new voice                    
                }

                currentEffect = newEffect;
            }

        #endregion // Initialization

        }


        internal override void PlatformApply3D(AudioListener listener, AudioEmitter emitter)
        {
            // If we have no voice then nothing to do.
            if (_voice == null || ConcreteAudioService.MasterVoice == null)
                return;

            // Convert from XNA Emitter to a SharpDX Emitter
            var e = ToDXEmitter(emitter);
            e.CurveDistanceScaler = SoundEffect.DistanceScale;
            e.DopplerScaler = SoundEffect.DopplerScale;
            e.ChannelCount = _concreteSoundEffect._format.Channels;

            //stereo channel
            if (e.ChannelCount > 1)
            {
                e.ChannelRadius = 0;
                e.ChannelAzimuths = _defaultChannelAzimuths;
            }

            // Convert from XNA Listener to a SharpDX Listener
            var l = ToDXListener(listener);

            // Number of channels in the sound being played.
            // Not actually sure if XNA supported 3D attenuation of sterio sounds, but X3DAudio does.
            var srcChannelCount = _concreteSoundEffect._format.Channels;

            // Number of output channels.
            var dstChannelCount = ConcreteAudioService.MasterVoice.VoiceDetails.InputChannelCount;

            // XNA supports distance attenuation and doppler.            
            var dpsSettings = ConcreteAudioService.Device3D.Calculate(l, e, CalculateFlags.Matrix | CalculateFlags.Doppler, srcChannelCount, dstChannelCount);

            // Apply Volume settings (from distance attenuation) ...
            _voice.SetOutputMatrix(ConcreteAudioService.MasterVoice, srcChannelCount, dstChannelCount, dpsSettings.MatrixCoefficients, 0);

            // Apply Pitch settings (from doppler) ...
            _voice.SetFrequencyRatio(dpsSettings.DopplerFactor);
        }

        private Emitter _dxEmitter;
        private Listener _dxListener;

        private Emitter ToDXEmitter(AudioEmitter emitter)
        {
            // Pulling out Vector properties for efficiency.
            var pos = emitter.Position;
            var vel = emitter.Velocity;
            var forward = emitter.Forward;
            var up = emitter.Up;

            // From MSDN:
            //  X3DAudio uses a left-handed Cartesian coordinate system, 
            //  with values on the x-axis increasing from left to right, on the y-axis from bottom to top, 
            //  and on the z-axis from near to far. 
            //  Azimuths are measured clockwise from a given reference direction. 
            //
            // From MSDN:
            //  The XNA Framework uses a right-handed coordinate system, 
            //  with the positive z-axis pointing toward the observer when the positive x-axis is pointing to the right, 
            //  and the positive y-axis is pointing up. 
            //
            // Programmer Notes:         
            //  According to this description the z-axis (forward vector) is inverted between these two coordinate systems.
            //  Therefore, we need to negate the z component of any position/directions/velocity values.

            forward.Z *= -1.0f;
            up.Z *= -1.0f;
            pos.Z *= -1.0f;
            vel.Z *= -1.0f;

            if (_dxEmitter == null)
                _dxEmitter = new Emitter();

            _dxEmitter.Position = new RawVector3(pos.X, pos.Y, pos.Z);
            _dxEmitter.Velocity = new RawVector3(vel.X, vel.Y, vel.Z);
            _dxEmitter.OrientFront = new RawVector3(forward.X, forward.Y, forward.Z);
            _dxEmitter.OrientTop = new RawVector3(up.X, up.Y, up.Z);
            _dxEmitter.DopplerScaler = emitter.DopplerScale;
            return _dxEmitter;
        }

        private Listener ToDXListener(AudioListener listener)
        {
            // Pulling out Vector properties for efficiency.
            var pos = listener.Position;
            var vel = listener.Velocity;
            var forward = listener.Forward;
            var up = listener.Up;

            // From MSDN:
            //  X3DAudio uses a left-handed Cartesian coordinate system, 
            //  with values on the x-axis increasing from left to right, on the y-axis from bottom to top, 
            //  and on the z-axis from near to far. 
            //  Azimuths are measured clockwise from a given reference direction. 
            //
            // From MSDN:
            //  The XNA Framework uses a right-handed coordinate system, 
            //  with the positive z-axis pointing toward the observer when the positive x-axis is pointing to the right, 
            //  and the positive y-axis is pointing up. 
            //
            // Programmer Notes:         
            //  According to this description the z-axis (forward vector) is inverted between these two coordinate systems.
            //  Therefore, we need to negate the z component of any position/directions/velocity values.

            forward.Z *= -1.0f;
            up.Z *= -1.0f;
            pos.Z *= -1.0f;
            vel.Z *= -1.0f;

            if (_dxListener == null)
                _dxListener = new Listener();

            _dxListener.Position = new RawVector3 { X = pos.X, Y = pos.Y, Z = pos.Z };
            _dxListener.Velocity = new RawVector3 { X = vel.X, Y = vel.Y, Z = vel.Z };
            _dxListener.OrientFront = new RawVector3 { X = forward.X, Y = forward.Y, Z = forward.Z };
            _dxListener.OrientTop = new RawVector3 { X = up.X, Y = up.Y, Z = up.Z };
            return _dxListener;
        }

        internal override void PlatformPause()
        {
            _voice.Stop();
        }

        internal override void PlatformPlay(bool isLooped, float pitch)
        {
            // Choose the correct buffer depending on if we are looped.
            var buffer = _concreteSoundEffect.GetDXDataBuffer(isLooped);

            if (_voice.State.BuffersQueued > 0)
            {
                _voice.Stop();
                _voice.FlushSourceBuffers();
            }

            _voice.SubmitSourceBuffer(buffer, null);
            _voice.Start();
        }

        internal override void PlatformResume(bool isLooped)
        {
            // Restart the sound if (and only if) it stopped playing
            if (!isLooped)
            {
                if (_voice.State.BuffersQueued == 0)
                {
                    _voice.Stop();
                    _voice.FlushSourceBuffers();
                    var buffer = _concreteSoundEffect.GetDXDataBuffer(false);
                    _voice.SubmitSourceBuffer(buffer, null);
                }
            }
            _voice.Start();
        }

        internal override void PlatformStop()
        {
            _voice.Stop();
            _voice.FlushSourceBuffers();
        }

        internal override void PlatformRelease(bool isLooped)
        {
            if (isLooped)
                _voice.ExitLoop();
            else
                _voice.Stop((int)PlayFlags.Tails);
        }

        internal override bool PlatformUpdateState(ref SoundState state)
        {
            // check if the sound has stopped
            if (state == SoundState.Playing)
            {
                // If no voice or no buffers queued the sound is stopped.
                if (ConcreteAudioService.MasterVoice == null || _voice.State.BuffersQueued == 0)
                {
                    // update instance
                    state = SoundState.Stopped;
                    return true;
                }
            }

            return false;
        }

        internal override void PlatformSetIsLooped(SoundState state, bool isLooped)
        {
            if (state == SoundState.Playing)
            {
                if (!isLooped)
                {
                    // release loop while sound is playing
                    _voice.ExitLoop();
                }
                else
                {
                    // enable loop while sound is playing
                    var loopedBuffer = _concreteSoundEffect.GetDXDataBuffer(true);
                    _voice.SubmitSourceBuffer(loopedBuffer, null);
                }
            }
        }

        internal override void PlatformSetPan(float pan)
        {
            if (_voice != null && ConcreteAudioService.MasterVoice != null)
            {
                UpdateOutputMatrix(pan);
            }
        }

        private void UpdateOutputMatrix(float pan)
        {
            var srcChannelCount = _voice.VoiceDetails.InputChannelCount;
            var dstChannelCount = ConcreteAudioService.MasterVoice.VoiceDetails.InputChannelCount;

            // Set the pan on the correct channels based on the reverb mix.
            if (!(_reverbMix > 0.0f))
                _voice.SetOutputMatrix(srcChannelCount, dstChannelCount, CalculateOutputMatrix(pan, 1.0f, srcChannelCount));
            else
            {
                _voice.SetOutputMatrix(ConcreteAudioService.ReverbVoice, srcChannelCount, dstChannelCount, CalculateOutputMatrix(pan, _reverbMix, srcChannelCount));
                _voice.SetOutputMatrix(ConcreteAudioService.MasterVoice, srcChannelCount, dstChannelCount, CalculateOutputMatrix(pan, 1.0f - Math.Min(_reverbMix, 1.0f), srcChannelCount));
            }
        }

        internal static float[] CalculateOutputMatrix(float pan, float scale, int inputChannels)
        {
            // XNA only ever outputs to the front left/right speakers (channels 0 and 1)
            // Assumes there are at least 2 speaker channels to output to

            // Clear all the channels.
            var outputMatrix = _outputMatrix;
            Array.Clear(outputMatrix, 0, outputMatrix.Length);

            if (inputChannels == 1) // Mono source
            {
                // Left/Right output levels:
                //   Pan -1.0: L = 1.0, R = 0.0
                //   Pan  0.0: L = 1.0, R = 1.0
                //   Pan +1.0: L = 0.0, R = 1.0
                outputMatrix[0] = (pan > 0f) ? ((1f - pan) * scale) : scale; // Front-left output
                outputMatrix[1] = (pan < 0f) ? ((1f + pan) * scale) : scale; // Front-right output
            }
            else if (inputChannels == 2) // Stereo source
            {
                // Left/Right input (Li/Ri) mix for Left/Right outputs (Lo/Ro):
                //   Pan -1.0: Lo = 0.5Li + 0.5Ri, Ro = 0.0Li + 0.0Ri
                //   Pan  0.0: Lo = 1.0Li + 0.0Ri, Ro = 0.0Li + 1.0Ri
                //   Pan +1.0: Lo = 0.0Li + 0.0Ri, Ro = 0.5Li + 0.5Ri
                if (pan <= 0f)
                {
                    outputMatrix[0] = (1f + pan * 0.5f) * scale; // Front-left output, Left input
                    outputMatrix[1] = (-pan * 0.5f) * scale; // Front-left output, Right input
                    outputMatrix[2] = 0f; // Front-right output, Left input
                    outputMatrix[3] = (1f + pan) * scale; // Front-right output, Right input
                }
                else
                {
                    outputMatrix[0] = (1f - pan) * scale; // Front-left output, Left input
                    outputMatrix[1] = 0f; // Front-left output, Right input
                    outputMatrix[2] = (pan * 0.5f) * scale; // Front-right output, Left input
                    outputMatrix[3] = (1f - pan * 0.5f) * scale; // Front-right output, Right input
                }
            }

            return outputMatrix;
        }

        internal override void PlatformSetPitch(float pitch)
        {
            if (_voice == null || ConcreteAudioService.MasterVoice == null)
                return;

            // NOTE: This is copy of what XAudio2.SemitonesToFrequencyRatio() does
            // which avoids the native call and is actually more accurate.
            var xapitch = (float)Math.Pow(2.0, pitch);
            _voice.SetFrequencyRatio(xapitch);
        }

        internal override void PlatformSetVolume(float value)
        {
            if (_voice != null && ConcreteAudioService.MasterVoice != null)
                _voice.SetVolume(value, XAudio2.CommitNow);
        }

        internal override void PlatformSetReverbMix(SoundState state, float mix, float pan)
        {
            // At least for XACT we can't go over 2x the volume on the mix.
            _reverbMix = MathHelper.Clamp(mix, 0, 2);

            // If we have no voice then nothing more to do.
            if (_voice == null || ConcreteAudioService.MasterVoice == null)
                return;

            if (!(_reverbMix > 0.0f))
                _voice.SetOutputVoices(new VoiceSendDescriptor(ConcreteAudioService.MasterVoice));
            else
            {
                _voice.SetOutputVoices(new VoiceSendDescriptor(ConcreteAudioService.ReverbVoice),
                                        new VoiceSendDescriptor(ConcreteAudioService.MasterVoice));
            }

            UpdateOutputMatrix(pan);
        }

        internal override void PlatformSetFilter(SoundState state, FilterMode mode, float filterQ, float frequency)
        {
            if (_voice == null || ConcreteAudioService.MasterVoice == null)
                return;

            var filter = new FilterParameters
            {
                Frequency = XAudio2.CutoffFrequencyToRadians(frequency, _voice.VoiceDetails.InputSampleRate),
                OneOverQ = 1.0f / filterQ,
                Type = (FilterType)mode
            };
            _voice.SetFilterParameters(filter);
        }

        internal override void PlatformClearFilter()
        {
            if (_voice == null || ConcreteAudioService.MasterVoice == null)
                return;

            var filter = new FilterParameters { Frequency = 1.0f, OneOverQ = 1.0f, Type = FilterType.LowPassFilter };
            _voice.SetFilterParameters(filter);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_reverb != null)
                {
                    _reverb.Dispose();
                    _reverb = null;
                }

                if (ConcreteAudioService.MasterVoice != null)
                {
                    _voice.DestroyVoice(); // TODO: _voice.Dispose() should also destroy voice
                    _voice.Dispose();
                    _voice = null;
                }
            }

        }
    }
}
