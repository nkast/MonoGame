// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using System.Collections.Generic;
using Microsoft.Xna.Platform.Audio;

namespace Microsoft.Xna.Framework.Audio
{
    /// <summary>
    /// A <see cref="SoundEffectInstance"/> for which the audio buffer is provided by the game at run time.
    /// </summary>
    public sealed partial class DynamicSoundEffectInstance : SoundEffectInstance
    {
        private const int TargetPendingBufferCount = 3;
        private int _buffersNeeded;
        private int _sampleRate;
        private AudioChannels _channels;
        private SoundState _dynamicState;

        IDynamicSoundEffectInstanceStrategy _dstrategy { get; set; }

        internal LinkedListNode<DynamicSoundEffectInstance> DynamicPlayingInstancesNode { get; private set; }

        #region Public Properties

        /// <summary>
        /// This value has no effect on DynamicSoundEffectInstance.
        /// It may not be set.
        /// </summary>
        public override bool IsLooped
        {
            get
            {
                return false;
            }

            set
            {
                AssertNotDisposed();
                if (value == true)
                    throw new InvalidOperationException("IsLooped cannot be set true. Submit looped audio data to implement looping.");
            }
        }

        public override SoundState State
        {
            get
            {
                AssertNotDisposed();
                return _dynamicState;
            }
        }

        /// <summary>
        /// Returns the number of audio buffers queued for playback.
        /// </summary>
        public int PendingBufferCount
        {
            get
            {
                AssertNotDisposed();
                return _dstrategy.DynamicPlatformGetPendingBufferCount();
            }
        }

        /// <summary>
        /// The event that occurs when the number of queued audio buffers is less than or equal to 2.
        /// </summary>
        /// <remarks>
        /// This event may occur when <see cref="Play()"/> is called or during playback when a buffer is completed.
        /// </remarks>
        public event EventHandler<EventArgs> BufferNeeded;

        #endregion

        #region Public Constructor

        /// <param name="sampleRate">Sample rate, in Hertz (Hz).</param>
        /// <param name="channels">Number of channels (mono or stereo).</param>
        public DynamicSoundEffectInstance(int sampleRate, AudioChannels channels) 
            : base(AudioService.Current)
        {
            if ((sampleRate < 8000) || (sampleRate > 48000))
                throw new ArgumentOutOfRangeException("sampleRate");
            if ((channels != AudioChannels.Mono) && (channels != AudioChannels.Stereo))
                throw new ArgumentOutOfRangeException("channels");
            
            _sampleRate = sampleRate;
            _channels = channels;
            _dynamicState = SoundState.Stopped;

            // This instance is added to the pool so that its volume reflects master volume changes
            // and it contributes to the playing instances limit, but the source/voice is not owned by the pool.
            DynamicPlayingInstancesNode = new LinkedListNode<DynamicSoundEffectInstance>(this);
            base._isDynamic = true;

            _strategy = (SoundEffectInstanceStrategy)_audioService._strategy.CreateDynamicSoundEffectInstanceStrategy(_sampleRate, _channels, Pan);
            _dstrategy = (IDynamicSoundEffectInstanceStrategy)_strategy;
            _dstrategy.OnBufferNeeded += _dstrategy_OnBufferNeeded;
        }

        private void _dstrategy_OnBufferNeeded(object sender, EventArgs e)
        {
            CheckBufferCount();
        }

        #endregion

        #region Public Functions

        /// <summary>
        /// Returns the duration of an audio buffer of the specified size, based on the settings of this instance.
        /// </summary>
        /// <param name="sizeInBytes">Size of the buffer, in bytes.</param>
        /// <returns>The playback length of the buffer.</returns>
        public TimeSpan GetSampleDuration(int sizeInBytes)
        {
            AssertNotDisposed();
            return SoundEffect.GetSampleDuration(sizeInBytes, _sampleRate, _channels);
        }

        /// <summary>
        /// Returns the size, in bytes, of a buffer of the specified duration, based on the settings of this instance.
        /// </summary>
        /// <param name="duration">The playback length of the buffer.</param>
        /// <returns>The data size of the buffer, in bytes.</returns>
        public int GetSampleSizeInBytes(TimeSpan duration)
        {
            AssertNotDisposed();
            return SoundEffect.GetSampleSizeInBytes(duration, _sampleRate, _channels);
        }

        /// <summary>
        /// Pauses playback of the DynamicSoundEffectInstance.
        /// </summary>
        public override void Pause()
        {
            lock (AudioService.SyncHandle)
            {
                AssertNotDisposed();

                var state = _dynamicState;
                switch (state)
                {
                    case SoundState.Paused:
                        return;
                    case SoundState.Stopped:
                        return;
                    case SoundState.Playing:
                        {
                            _strategy.PlatformPause();
                            _dynamicState = SoundState.Paused;

                            _audioService.RemovePlayingInstance(this);
                            _audioService.RemoveDynamicPlayingInstance(this);
                        }
                        return;
                }
            }
        }

        /// <summary>
        /// Plays or resumes the DynamicSoundEffectInstance.
        /// </summary>
        public override void Play()
        {
            lock (AudioService.SyncHandle)
            {
                AssertNotDisposed();

                var state = _dynamicState;
                switch (state)
                {
                    case SoundState.Playing:
                        return;
                    case SoundState.Paused:
                        Resume();
                        return;
                    case SoundState.Stopped:
                        {
                            // Ensure that the volume reflects master volume, which is done by the setter.
                            Volume = Volume;

                            _strategy.PlatformPlay(IsLooped, Pitch);
                            _dynamicState = SoundState.Playing;

                            _audioService.AddPlayingInstance(this);

                            CheckBufferCount();

                            _audioService.AddDynamicPlayingInstance(this);
                        }
                        return;
                }               
            }
        }

        /// <summary>
        /// Resumes playback of the DynamicSoundEffectInstance.
        /// </summary>
        public override void Resume()
        {
            lock (AudioService.SyncHandle)
            {
                AssertNotDisposed();

                var state = _dynamicState;
                switch (state)
                {
                    case SoundState.Playing:
                        return;
                    case SoundState.Stopped:
                        Play();
                        return;
                    case SoundState.Paused:
                        {
                            Volume = Volume;

                            _strategy.PlatformResume(IsLooped);
                            _dynamicState = SoundState.Playing;

                            _audioService.AddPlayingInstance(this);
                            _audioService.AddDynamicPlayingInstance(this);
                        }
                        return;
                }
            }
        }

        /// <summary>
        /// Immediately stops playing the DynamicSoundEffectInstance.
        /// </summary>
        /// <remarks>
        /// Calling this also releases all queued buffers.
        /// </remarks>
        public override void Stop()
        {
            lock (AudioService.SyncHandle)
            {
                AssertNotDisposed();

                var state = _dynamicState;
                switch (state)
                {
                    case SoundState.Stopped:
                        _strategy.PlatformStop(); // Dequeue all the submitted buffers
                        return;
                    case SoundState.Paused:
                    case SoundState.Playing:
                        {
                            _strategy.PlatformStop();
                            _dynamicState = SoundState.Stopped;

                            _audioService.RemovePlayingInstance(this);
                            _audioService.RemoveDynamicPlayingInstance(this);
                        }
                        return;
                }
            }
        }

        /// <summary>
        /// Stops playing the DynamicSoundEffectInstance.
        /// If the <paramref name="immediate"/> parameter is false, this call has no effect.
        /// </summary>
        /// <remarks>
        /// Calling this also releases all queued buffers.
        /// </remarks>
        /// <param name="immediate">When set to false, this call has no effect.</param>
        public override void Stop(bool immediate)
        {
            if (immediate)
            {
                Stop();
                return;
            }


            
            lock (AudioService.SyncHandle)
            {
                AssertNotDisposed();
                // TODO: exit loop
            }
        }

        /// <summary>
        /// Queues an audio buffer for playback.
        /// </summary>
        /// <remarks>
        /// The buffer length must conform to alignment requirements for the audio format.
        /// </remarks>
        /// <param name="buffer">The buffer containing PCM audio data.</param>
        public void SubmitBuffer(byte[] buffer)
        {
            AssertNotDisposed();
            
            if (buffer.Length == 0)
                throw new ArgumentException("Buffer may not be empty.");

            // Ensure that the buffer length matches alignment.
            // The data must be 16-bit, so the length is a multiple of 2 (mono) or 4 (stereo).
            var sampleSize = 2 * (int)_channels;
            if (buffer.Length % sampleSize != 0)
                throw new ArgumentException("Buffer length does not match format alignment.");

            SubmitBuffer(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Queues an audio buffer for playback.
        /// </summary>
        /// <remarks>
        /// The buffer length must conform to alignment requirements for the audio format.
        /// </remarks>
        /// <param name="buffer">The buffer containing PCM audio data.</param>
        /// <param name="offset">The starting position of audio data.</param>
        /// <param name="count">The amount of bytes to use.</param>
        public void SubmitBuffer(byte[] buffer, int offset, int count)
        {
            AssertNotDisposed();
            
            if ((buffer == null) || (buffer.Length == 0))
                throw new ArgumentException("Buffer may not be null or empty.");
            if (count <= 0)
                throw new ArgumentException("Number of bytes must be greater than zero.");
            if ((offset + count) > buffer.Length)
                throw new ArgumentException("Buffer is shorter than the specified number of bytes from the offset.");

            // Ensure that the buffer length and start position match alignment.
            var sampleSize = 2 * (int)_channels;
            if (count % sampleSize != 0)
                throw new ArgumentException("Number of bytes does not match format alignment.");
            if (offset % sampleSize != 0)
                throw new ArgumentException("Offset into the buffer does not match format alignment.");

            _dstrategy.DynamicPlatformSubmitBuffer(buffer, offset, count, _dynamicState);
        }

        #endregion

        #region Nonpublic Functions

        private void AssertNotDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("DynamicSoundEffectInstance");
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                _dstrategy.OnBufferNeeded -= _dstrategy_OnBufferNeeded;
                _strategy.Dispose();
                _strategy = null;
                _dstrategy = null;
                base.Dispose(disposing);

                DynamicPlayingInstancesNode = null;
            }
            else
            {
                _dstrategy.OnBufferNeeded -= _dstrategy_OnBufferNeeded;
                _strategy = null;
                _dstrategy = null;
                base.Dispose(disposing);

                DynamicPlayingInstancesNode = null;
            }

        }

        private void CheckBufferCount()
        {
            if ((PendingBufferCount < TargetPendingBufferCount) &&
                (_dynamicState == SoundState.Playing))
                _buffersNeeded++;
        }

        internal void UpdateQueue()
        {
            // Update the buffers
            _dstrategy.DynamicPlatformUpdateQueue();

            // Raise the event
            var bufferNeededHandler = BufferNeeded;
            if (bufferNeededHandler != null)
            {
                var eventCount = Math.Max(_buffersNeeded,3);
                for (var i = 0; i < eventCount; i++)
                    bufferNeededHandler(this, EventArgs.Empty);
            }

            _buffersNeeded = 0;
        }

        #endregion
    }
}
