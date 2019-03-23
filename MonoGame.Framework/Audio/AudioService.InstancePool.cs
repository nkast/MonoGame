// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Audio
{
    internal sealed partial class AudioService
    {
        private LinkedList<SoundEffectInstance> _pooledInstances = new LinkedList<SoundEffectInstance>();

        /// <summary>
        /// Add the specified instance to the pool if it is a pooled instance and removes it from the
        /// list of playing instances.
        /// </summary>
        /// <param name="inst">The SoundEffectInstance</param>
        internal void AddPooledInstance(SoundEffectInstance inst)
        {
            if (inst.PooledInstancesNode == null)
                return;

            var maxPooledInstances = Math.Min(512, AudioService.MAX_PLAYING_INSTANCES) * 2;
            if (_pooledInstances.Count >= maxPooledInstances)
            {
                var firstNode = _pooledInstances.First;
                firstNode.Value.Dispose();
                _pooledInstances.Remove(firstNode);
            }

            _pooledInstances.AddLast(inst.PooledInstancesNode);
        }

        /// <summary>
        /// Returns a pooled SoundEffectInstance if one is available, or allocates a new
        /// SoundEffectInstance if the pool is empty.
        /// </summary>
        /// <returns>The SoundEffectInstance.</returns>
        private SoundEffectInstance GetPooledInstance(SoundEffect effect, bool forXAct = false)
        {
            SoundEffectInstance inst = null;

            // search for an instance of effect
            for (var node = _pooledInstances.First; node != null; node = node.Next)
            {
                if (ReferenceEquals(node.Value._effect, effect))
                {
                    inst = node.Value;
                    _pooledInstances.Remove(inst.PooledInstancesNode);
                    break;
                }
            }
            
            // get any instance and reuse it
            var count = _pooledInstances.Count;
            var maxPooledInstances = Math.Min(512, AudioService.MAX_PLAYING_INSTANCES) * 2;
            if (inst == null && count == maxPooledInstances)
            {
                inst = _pooledInstances.First.Value;
                _pooledInstances.Remove(inst.PooledInstancesNode);
            }

            if (inst != null)
            {
                // Reset used instance to the "default" state.
                inst._isXAct = forXAct;
                inst.Volume = 1.0f;
                inst.Pan = 0.0f;
                inst.Pitch = 0.0f;
                inst.IsLooped = false;
                inst.PlatformSetReverbMix(0);
                inst.PlatformClearFilter();

                inst.PlatformReuseInstance(effect);
                return inst;
            }

            return null;
        }
    }
}
