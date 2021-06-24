// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using MonoGame.OpenAL;

#if ANDROID
using System.Globalization;
using Android.Content.PM;
using Android.Content;
using Android.Media;
#endif

#if IOS
using AudioToolbox;
using AudioUnit;
using AVFoundation;
#endif


namespace Microsoft.Xna.Framework.Audio
{
    internal partial class AudioService
    {

#if DESKTOPGL || ANGLE
        // MacOS & Linux shares a limit of 256.
        internal const int MAX_PLAYING_INSTANCES = 256;
#elif IOS
        // Reference: http://stackoverflow.com/questions/3894044/maximum-number-of-openal-sound-buffers-on-iphone
        internal const int MAX_PLAYING_INSTANCES = 32;
#elif ANDROID
        // Set to the same as OpenAL on iOS
        internal const int MAX_PLAYING_INSTANCES = 32;
#endif


        private EffectsExtension _efx = null;
        private IntPtr _device;
        private IntPtr _context;
        IntPtr NullContext = IntPtr.Zero;

#if ANDROID
        private const int DEFAULT_FREQUENCY = 48000;
        private const int DEFAULT_UPDATE_SIZE = 512;
        private const int DEFAULT_UPDATE_BUFFER_COUNT = 2;
#elif DESKTOPGL
        private static OggStreamer _oggstreamer;
#endif
        private Stack<int> _alSourcesPool = new Stack<int>(32);
        bool _isDisposed;
        public bool SupportsIma4 { get; private set; }
        public bool SupportsAdpcm { get; private set; }
        public bool SupportsEfx { get; private set; }
        public bool SupportsIeee { get; private set; }
        
        internal int ReverbSlot = 0;
        internal int ReverbEffect = 0;
        
        public int Filter { get; private set; }
        
        public EffectsExtension Efx
        {
            get
            {
                if (_efx == null)
                    _efx = new EffectsExtension();
                return _efx;
            }
        }

        private void PlatformCreate()
        {
            if (AL.NativeLibrary == IntPtr.Zero)
                throw new DllNotFoundException("Couldn't initialize OpenAL because the native binaries couldn't be found.");

            if (!OpenSoundController())
            {
                throw new NoAudioHardwareException("OpenAL device could not be initialized, see console output for details.");
            }

            if (Alc.IsExtensionPresent(_device, "ALC_EXT_CAPTURE"))
                Microphone.PopulateCaptureDevices();

            // We have hardware here and it is ready
            Filter = 0;
            if (Efx.IsInitialized)
            {
                Filter = Efx.GenFilter();
            }
        }
        
        /// <summary>
        /// Open the sound device, sets up an audio context, and makes the new context
        /// the current context. Note that this method will stop the playback of
        /// music that was running prior to the game start. If any error occurs, then
        /// the state of the controller is reset.
        /// </summary>
        /// <returns>True if the sound controller was setup, and false if not.</returns>
        private bool OpenSoundController()
        {
            try
            {
                _device = Alc.OpenDevice(string.Empty);
                EffectsExtension.device = _device;
            }
            catch (Exception ex)
            {
                throw new NoAudioHardwareException("OpenAL device could not be initialized.", ex);
            }

            AlcHelper.CheckError("Could not open OpenAL device");

            if (_device != IntPtr.Zero)
            {
#if ANDROID
                // Attach activity event handlers so we can pause and resume all playing sounds
                MonoGameAndroidGameView.OnPauseGameThread += Activity_Paused;
                MonoGameAndroidGameView.OnResumeGameThread += Activity_Resumed;

                // Query the device for the ideal frequency and update buffer size so
                // we can get the low latency sound path.

                /*
                The recommended sequence is:

                Check for feature "android.hardware.audio.low_latency" using code such as this:
                import android.content.pm.PackageManager;
                ...
                PackageManager pm = getContext().getPackageManager();
                boolean claimsFeature = pm.hasSystemFeature(PackageManager.FEATURE_AUDIO_LOW_LATENCY);
                Check for API level 17 or higher, to confirm use of android.media.AudioManager.getProperty().
                Get the native or optimal output sample rate and buffer size for this device's primary output stream, using code such as this:
                import android.media.AudioManager;
                ...
                AudioManager am = (AudioManager) getSystemService(Context.AUDIO_SERVICE);
                String sampleRate = am.getProperty(AudioManager.PROPERTY_OUTPUT_SAMPLE_RATE));
                String framesPerBuffer = am.getProperty(AudioManager.PROPERTY_OUTPUT_FRAMES_PER_BUFFER));
                Note that sampleRate and framesPerBuffer are Strings. First check for null and then convert to int using Integer.parseInt().
                Now use OpenSL ES to create an AudioPlayer with PCM buffer queue data locator.

                See http://stackoverflow.com/questions/14842803/low-latency-audio-playback-on-android
                */

                int frequency = DEFAULT_FREQUENCY;
                int updateSize = DEFAULT_UPDATE_SIZE;
                int updateBuffers = DEFAULT_UPDATE_BUFFER_COUNT;
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.JellyBeanMr1)
                {
                    Android.Util.Log.Debug("OAL", Game.Activity.PackageManager.HasSystemFeature(PackageManager.FeatureAudioLowLatency) ? "Supports low latency audio playback." : "Does not support low latency audio playback.");

                    var audioManager = Game.Activity.GetSystemService(Context.AudioService) as AudioManager;
                    if (audioManager != null)
                    {
                        var result = audioManager.GetProperty(AudioManager.PropertyOutputSampleRate);
                        if (!string.IsNullOrEmpty(result))
                            frequency = int.Parse(result, CultureInfo.InvariantCulture);
                        result = audioManager.GetProperty(AudioManager.PropertyOutputFramesPerBuffer);
                        if (!string.IsNullOrEmpty(result))
                            updateSize = int.Parse(result, CultureInfo.InvariantCulture);
                    }

                    // If 4.4 or higher, then we don't need to double buffer on the application side.
                    // See http://stackoverflow.com/a/15006327
                    if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
                    {
                        updateBuffers = 1;
                    }
                }
                else
                {
                    Android.Util.Log.Debug("OAL", "Android 4.2 or higher required for low latency audio playback.");
                }
                Android.Util.Log.Debug("OAL", "Using sample rate " + frequency + "Hz and " + updateBuffers + " buffers of " + updateSize + " frames.");

                // These are missing and non-standard ALC constants
                const int AlcFrequency = 0x1007;
                const int AlcUpdateSize = 0x1014;
                const int AlcUpdateBuffers = 0x1015;

                int[] attribute = new[]
                {
                    AlcFrequency, frequency,
                    AlcUpdateSize, updateSize,
                    AlcUpdateBuffers, updateBuffers,
                    0
                };
#elif IOS
                AVAudioSession.SharedInstance().Init();

                // NOTE: Do not override AVAudioSessionCategory set by the game developer:
                //       see https://github.com/MonoGame/MonoGame/issues/6595

                EventHandler<AVAudioSessionInterruptionEventArgs> handler = delegate(object sender, AVAudioSessionInterruptionEventArgs e) {
                    switch (e.InterruptionType)
                    {
                        case AVAudioSessionInterruptionType.Began:
                            AVAudioSession.SharedInstance().SetActive(false);
                            Alc.MakeContextCurrent(IntPtr.Zero);
                            Alc.SuspendContext(_context);
                            break;
                        case AVAudioSessionInterruptionType.Ended:
                            AVAudioSession.SharedInstance().SetActive(true);
                            Alc.MakeContextCurrent(_context);
                            Alc.ProcessContext(_context);
                            break;
                    }
                };

                AVAudioSession.Notifications.ObserveInterruption(handler);

                // Activate the instance or else the interruption handler will not be called.
                AVAudioSession.SharedInstance().SetActive(true);

                int[] attribute = new int[0];
#else
                int[] attribute = new int[0];
#endif

                _context = Alc.CreateContext(_device, attribute);
#if DESKTOPGL
                _oggstreamer = new OggStreamer();
#endif

                AlcHelper.CheckError("Could not create OpenAL context");

                if (_context != NullContext)
                {
                    Alc.MakeContextCurrent(_context);
                    AlcHelper.CheckError("Could not make OpenAL context current");
                    SupportsIma4 = AL.IsExtensionPresent("AL_EXT_IMA4");
                    SupportsAdpcm = AL.IsExtensionPresent("AL_SOFT_MSADPCM");
                    SupportsEfx = AL.IsExtensionPresent("AL_EXT_EFX");
                    SupportsIeee = AL.IsExtensionPresent("AL_EXT_float32");
                    return true;
                }
            }
            return false;
        }

        internal void PlatformSetReverbSettings(ReverbSettings reverbSettings)
        {
            if (!Efx.IsInitialized)
                return;

            if (ReverbEffect != 0)
                return;

            var efx = Efx;
            efx.GenAuxiliaryEffectSlots(1, out ReverbSlot);
            efx.GenEffect(out ReverbEffect);
            efx.Effect(ReverbEffect, EfxEffecti.EffectType, (int)EfxEffectType.Reverb);
            efx.Effect(ReverbEffect, EfxEffectf.EaxReverbReflectionsDelay, reverbSettings.ReflectionsDelayMs / 1000.0f);
            efx.Effect(ReverbEffect, EfxEffectf.LateReverbDelay, reverbSettings.ReverbDelayMs / 1000.0f);
            // map these from range 0-15 to 0-1
            efx.Effect(ReverbEffect, EfxEffectf.EaxReverbDiffusion, reverbSettings.EarlyDiffusion / 15f);
            efx.Effect(ReverbEffect, EfxEffectf.EaxReverbDiffusion, reverbSettings.LateDiffusion / 15f);
            efx.Effect(ReverbEffect, EfxEffectf.EaxReverbGainLF, Math.Min(XactHelpers.ParseVolumeFromDecibels(reverbSettings.LowEqGain - 8f), 1.0f));
            efx.Effect(ReverbEffect, EfxEffectf.EaxReverbLFReference, (reverbSettings.LowEqCutoff * 50f) + 50f);
            efx.Effect(ReverbEffect, EfxEffectf.EaxReverbGainHF, XactHelpers.ParseVolumeFromDecibels(reverbSettings.HighEqGain - 8f));
            efx.Effect(ReverbEffect, EfxEffectf.EaxReverbHFReference, (reverbSettings.HighEqCutoff * 500f) + 1000f);
            // According to Xamarin docs EaxReverbReflectionsGain Unit: Linear gain Range [0.0f .. 3.16f] Default: 0.05f
            efx.Effect(ReverbEffect, EfxEffectf.EaxReverbReflectionsGain, Math.Min(XactHelpers.ParseVolumeFromDecibels(reverbSettings.ReflectionsGainDb), 3.16f));
            efx.Effect(ReverbEffect, EfxEffectf.EaxReverbGain, Math.Min(XactHelpers.ParseVolumeFromDecibels(reverbSettings.ReverbGainDb), 1.0f));
            // map these from 0-100 down to 0-1
            efx.Effect(ReverbEffect, EfxEffectf.EaxReverbDensity, reverbSettings.DensityPct / 100f);
            efx.AuxiliaryEffectSlot(ReverbSlot, EfxEffectSlotf.EffectSlotGain, reverbSettings.WetDryMixPct / 200f);

            // Dont know what to do with these EFX has no mapping for them. Just ignore for now
            // we can enable them as we go. 
            //efx.SetEffectParam (ReverbEffect, EfxEffectf.PositionLeft, reverbSettings.PositionLeft);
            //efx.SetEffectParam (ReverbEffect, EfxEffectf.PositionRight, reverbSettings.PositionRight);
            //efx.SetEffectParam (ReverbEffect, EfxEffectf.PositionLeftMatrix, reverbSettings.PositionLeftMatrix);
            //efx.SetEffectParam (ReverbEffect, EfxEffectf.PositionRightMatrix, reverbSettings.PositionRightMatrix);
            //efx.SetEffectParam (ReverbEffect, EfxEffectf.LowFrequencyReference, reverbSettings.RearDelayMs);
            //efx.SetEffectParam (ReverbEffect, EfxEffectf.LowFrequencyReference, reverbSettings.RoomFilterFrequencyHz);
            //efx.SetEffectParam (ReverbEffect, EfxEffectf.LowFrequencyReference, reverbSettings.RoomFilterMainDb);
            //efx.SetEffectParam (ReverbEffect, EfxEffectf.LowFrequencyReference, reverbSettings.RoomFilterHighFrequencyDb);
            //efx.SetEffectParam (ReverbEffect, EfxEffectf.LowFrequencyReference, reverbSettings.DecayTimeSec);
            //efx.SetEffectParam (ReverbEffect, EfxEffectf.LowFrequencyReference, reverbSettings.RoomSizeFeet);

            efx.BindEffectToAuxiliarySlot(ReverbSlot, ReverbEffect);
        }


        /// <summary>
        /// Reserves a sound buffer and return its identifier. If there are no available sources
        /// or the controller was not able to setup the hardware then an
        /// <see cref="InstancePlayLimitException"/> is thrown.
        /// </summary>
        /// <returns>The source number of the reserved sound buffer.</returns>
        public int ReserveSource()
        {
            if (_alSourcesPool.Count > 0)
                return _alSourcesPool.Pop();

            int src = AL.GenSource();
            ALHelper.CheckError("Failed to generate source.");
            return src;
        }

        public void RecycleSource(int sourceId)
        {
            AL.Source(sourceId, ALSourcei.Buffer, 0);
            ALHelper.CheckError("Failed to free source from buffers.");

            _alSourcesPool.Push(sourceId);
        }

        public double SourceCurrentPosition(int sourceId)
        {
            int pos;
            AL.GetSource(sourceId, ALGetSourcei.SampleOffset, out pos);
            ALHelper.CheckError("Failed to set source offset.");
            return pos;
        }

#if ANDROID
        void Activity_Paused(object sender, EventArgs e)
        {
            // Pause all currently playing sounds by pausing the mixer
            Alc.DevicePause(_device);
        }

        void Activity_Resumed(object sender, EventArgs e)
        {
            // Resume all sounds that were playing when the activity was paused
            Alc.DeviceResume(_device);
        }
#endif

        private void PlatformDispose(bool disposing)
        {
            if (disposing)
            {
            }

            if (ReverbEffect != 0)
            {
                Efx.DeleteAuxiliaryEffectSlot(ReverbSlot);
                Efx.DeleteEffect((int)ReverbEffect);
            }

#if DESKTOPGL
                if(_oggstreamer != null)
                    _oggstreamer.Dispose();
#endif

            while (_alSourcesPool.Count > 0)
            {
                AL.DeleteSource(_alSourcesPool.Pop());
                ALHelper.CheckError("Failed to delete source.");
            }

            if (Filter != 0 && Efx.IsInitialized)
            {
                Efx.DeleteFilter(Filter);
            }

            Microphone.StopMicrophones();

            // CleanUpOpenAL
            Alc.MakeContextCurrent(NullContext);

            if (_context != NullContext)
            {
                Alc.DestroyContext(_context);
            }

            if (_device != IntPtr.Zero)
            {
                Alc.CloseDevice(_device);
            }

            _context = NullContext;
            _device = IntPtr.Zero;
        }
    }
}

