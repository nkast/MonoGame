// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos

using System;

namespace Microsoft.Xna.Framework.Audio
{
    internal partial class AudioService
    {

        // These platforms are only limited by memory.
        internal const int MAX_PLAYING_INSTANCES = int.MaxValue;

        private void PlatformCreate()
        {

        }


        internal void PlatformSetReverbSettings(ReverbSettings reverbSettings)
        {
        }

        private void PlatformDispose(bool disposing)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects).
            }

            // TODO: free unmanaged resources (unmanaged objects)
            // TODO: set large fields to null.

        }
    }
}

