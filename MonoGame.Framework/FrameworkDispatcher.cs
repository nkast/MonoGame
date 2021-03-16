// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using Microsoft.Xna.Framework.Audio;

namespace Microsoft.Xna.Framework
{
    /// <summary>
    /// Helper class for processing internal framework events.
    /// </summary>
    /// <remarks>
    /// If you use <see cref="Game"/> class, <see cref="Update()"/> is called automatically.
    /// Otherwise you must call it as part of your game loop.
    /// </remarks>
    public static class FrameworkDispatcher
    {
        internal static Action OnUpdate;

        /// <summary>
        /// Processes framework events.
        /// </summary>
        public static void Update()
        {
            var updateHandler = OnUpdate;
            if (updateHandler != null)
                updateHandler();

            DynamicSoundEffectInstanceManager.UpdatePlayingInstances();
            SoundEffectInstancePool.Update();
            Microphone.UpdateMicrophones();
        }
    }
}

