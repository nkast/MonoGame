// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Audio
{   
    internal sealed partial class AudioService : IDisposable
    {
        private volatile static AudioService _current;
        private LinkedList<SoundEffectInstance> _playingInstances = new LinkedList<SoundEffectInstance>();
        internal readonly static object SyncHandle = new object();


        internal static AudioService Current
        {
            get
            {
                var current = _current;
                if (current != null)
                    return current;

                // Create instance
                lock(SyncHandle)
                {
                    if (_current == null)
                    {   
                        try
                        {
                            _current = new AudioService();
                        }
                        catch (Exception ex)
                        {
                            throw new NoAudioHardwareException("Audio has failed to initialize.", ex);
                        }
                    }
                    return _current;
                }
            }
        }

        private AudioService()
        {
            PlatformCreate();
            FrameworkDispatcher.OnUpdate += AudioService.Update;
        }

        internal static void UpdateMasterVolume()
        {
            if (_current == null) return;

            lock (SyncHandle)
            {
                if (_current != null)
                    _current._UpdateMasterVolume();
            }
        }

        internal static void OnEffectDisposed(SoundEffect effect, bool disposing)
        {
            if (_current == null) return;

            lock (SyncHandle)
            {
                if (_current != null)
                    _current._OnEffectDisposed(effect, disposing);
            }
        }

        internal static void Update()
        {
            if (_current == null) return;

            lock (SyncHandle)
            {
                if (_current != null)
                {
                    _current._UpdateDynamicPlayingInstances();
                    _current._UpdatePlayingInstances();
                }
            }

            Microphone.UpdateMicrophones();
        }
        
        internal static void Shutdown()
        {
            if (_current == null) return;

            // Shutdown
            lock (SyncHandle)
            {
                if (_current != null)
                {
                    _current.Dispose();
                    _current = null;
                }
            }
        }

        private void _UpdateMasterVolume()
        {
            for (var node = _playingInstances.First; node != null; node = node.Next)
            {
                SoundEffectInstance inst = node.Value;

                // XAct sounds are not controlled by the SoundEffect
                // master volume, so we can skip them completely.
                if (inst._isXAct)
                    continue;

                // Re-applying the volume to itself will update
                // the sound with the current master volume.
                inst.Volume = inst.Volume;
            }
        }

        /// <summary>
        /// Iterates the list of playing instances, stop them and return them to the pool if they are instances of the given SoundEffect.
        /// </summary>
        /// <param name="effect">The SoundEffect</param>
        /// <param name="disposing">true if the Effect was disposed. false if it was collected.</param>
        private void _OnEffectDisposed(SoundEffect effect, bool disposing)
        {
            // stop playing instances of the disposed effect
            for (var node = _playingInstances.First; node != null; )
            {
                SoundEffectInstance inst = node.Value;
                node = node.Next;

                if (inst._effect == effect)
                {
                    inst.Stop();
                }
            }

            // remove instances of the disposed effect from _pooledInstances
            for (var node = _pooledInstances.First; node != null;)
            {
                SoundEffectInstance inst = node.Value;
                node = node.Next;

                if (inst._effect == effect)
                {
                    _pooledInstances.Remove(inst.PooledInstancesNode);

                    if (disposing)
                    {
                        inst.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Iterates the list of playing instances, returning them to the pool if they
        /// have stopped playing.
        /// </summary>
        private void _UpdatePlayingInstances()
        {
            // Cleanup instances which have finished playing.
            for (var node = _playingInstances.First; node != null; )
            {
                SoundEffectInstance inst = node.Value;
                node = node.Next;

                // Don't consume XACT instances... XACT will
                // clear this flag when it is done with the wave.
                if (inst._isXAct)
                    continue;

                System.Diagnostics.Debug.Assert(!inst.IsDisposed);
               
                inst.PlatformUpdateState();
            }
        }

        internal bool Play(SoundEffect effect)
        {
            lock (SyncHandle)
            {
                // is Sounds Available?
                if (!(_playingInstances.Count < AudioService.MAX_PLAYING_INSTANCES))
                    return false;

                var inst = GetInstance(effect);

                inst.Play();
            }

            return true;
        }

        internal bool Play(SoundEffect effect, float volume, float pitch, float pan)
        {
            lock (SyncHandle)
            {
                // is Sounds Available?
                if (!(_playingInstances.Count < AudioService.MAX_PLAYING_INSTANCES))
                    return false;

                var inst = AudioService.Current.GetInstance(effect);

                inst.Volume = volume;
                inst.Pitch = pitch;
                inst.Pan = pan;

                inst.Play();
            }

            return true;
        }

        internal SoundEffectInstance GetInstance(SoundEffect effect, bool isXAct = false)
        {
            var inst = GetPooledInstance(effect, isXAct);
            if (inst == null)
                inst = new SoundEffectInstance(this, effect, true, isXAct);

            return inst;
        }

        internal void AddPlayingInstance(SoundEffectInstance inst)
        {
            if (_playingInstances.Count >= AudioService.MAX_PLAYING_INSTANCES) // is Sounds Available?
                throw new InstancePlayLimitException();

            _playingInstances.AddLast(inst.PlayingInstancesNode);
        }

        internal void RemovePlayingInstance(SoundEffectInstance inst)
        {
            _playingInstances.Remove(inst.PlayingInstancesNode);
        }

        #region IDisposable

        private bool isDisposed = false;
        public event EventHandler Disposing;

        ~AudioService()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (isDisposed)
                    return;

                // stop playing instances of the disposed AudioSystem
                for (var node = _playingInstances.First; node != null;)
                {
                    SoundEffectInstance inst = node.Value;
                    node = node.Next;

                    inst.Stop();
                }

                var handler = Disposing;
                if (handler != null)
                    handler(this, EventArgs.Empty);

                // dispose pooled instances
                for (var node = _pooledInstances.First; node != null;)
                {
                    SoundEffectInstance inst = node.Value;
                    node = node.Next;

                    inst.Dispose();
                }

                FrameworkDispatcher.OnUpdate -= AudioService.Update;

                PlatformDispose(disposing);

                // free unmanaged resources (unmanaged objects)
                _playingInstances.Clear();
                _pooledInstances.Clear();

                // set large fields to null.
                _playingInstances = null;
                _pooledInstances = null;

                isDisposed = true;
            }
            else
            {
                if (isDisposed)
                    return;

                FrameworkDispatcher.OnUpdate -= AudioService.Update;

                PlatformDispose(disposing);

                // free unmanaged resources (unmanaged objects)
                _playingInstances.Clear();
                _pooledInstances.Clear();

                // set large fields to null.
                _playingInstances = null;
                _pooledInstances = null;

                isDisposed = true;
            }
        }


        #endregion // IDisposable
    }
}

