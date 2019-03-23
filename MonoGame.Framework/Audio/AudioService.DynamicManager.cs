// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Audio
{
    /// <summary>
    /// Handles the buffer events of all DynamicSoundEffectInstance instances.
    /// </summary>
    internal sealed partial class AudioService
    {
        private static readonly LinkedList<DynamicSoundEffectInstance> _dynamicPlayingInstances = new LinkedList<DynamicSoundEffectInstance>();
        
        public void AddDynamicPlayingInstance(DynamicSoundEffectInstance instance)
        {
            _dynamicPlayingInstances.AddLast(instance.DynamicPlayingInstancesNode);
        }

        public void RemoveDynamicPlayingInstance(DynamicSoundEffectInstance instance)
        {
            _dynamicPlayingInstances.Remove(instance.DynamicPlayingInstancesNode);
        }

        /// <summary>
        /// Updates buffer queues of the currently playing instances.
        /// </summary>
        /// <remarks>
        /// XNA posts <see cref="DynamicSoundEffectInstance.BufferNeeded"/> events always on the main thread.
        /// </remarks>
        public void _UpdateDynamicPlayingInstances()
        {
            for (var node = _dynamicPlayingInstances.First; node != null;)
            {
                DynamicSoundEffectInstance inst = node.Value;
                node = node.Next;
           
                if (!inst.IsDisposed)
                    inst.UpdateQueue();
            }
        }
    }
}
