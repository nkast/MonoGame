﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Platform.Audio;

namespace Microsoft.Xna.Platform.Audio
{
    /// <summary>
    /// Provides microphones capture features.
    /// </summary>	
    public sealed class ConcreteMicrophone : MicrophoneStrategy
    {
        internal override void PlatformStart(string deviceName, int sampleRate, int sampleSizeInBytes)
        {
			throw new NotImplementedException();
        }

        internal override void PlatformStop()
        {
			throw new NotImplementedException();
        }

        internal override bool PlatformIsHeadset()
        {
            throw new NotImplementedException();
        }

        internal override bool PlatformUpdateBuffer()
		{
			throw new NotImplementedException();
		}
		
		internal override int PlatformGetData(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
