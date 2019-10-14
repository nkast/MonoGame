﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

#if WP8
extern alias MicrosoftXnaFramework;
using MsAlbumCollection = MicrosoftXnaFramework::Microsoft.Xna.Framework.Media.AlbumCollection;
#endif
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Media
{
    public sealed class AlbumCollection : IDisposable
    {
#if WP8
        private MsAlbumCollection albumCollection;
#else
        private List<Album> albumCollection;
#endif

        /// <summary>
        /// Gets the number of Album objects in the AlbumCollection.
        /// </summary>
        public int Count
        {
            get
            {
                return this.albumCollection.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get
            {
#if WP8
                return this.albumCollection.IsDisposed;
#else
                return false;
#endif
            }
        }

#if WP8
        public static implicit operator AlbumCollection(MsAlbumCollection albumCollection)
        {
            return new AlbumCollection(albumCollection);
        }

        private AlbumCollection(MsAlbumCollection albumCollection)
        {
            this.albumCollection = albumCollection;
        }
#else
        public AlbumCollection(List<Album> albums)
        {
            this.albumCollection = albums;
        }
#endif

        /// <summary>
        /// Gets the Album at the specified index in the AlbumCollection.
        /// </summary>
        /// <param name="index">Index of the Album to get.</param>
        public Album this[int index]
        {
            get
            {
#if WP8
                return (Album)this.albumCollection[index];
#else
                return this.albumCollection[index];
#endif
            }
        }

        /// <summary>
        /// Immediately releases the unmanaged resources used by this object.
        /// </summary>
        public void Dispose()
        {
#if WP8
            this.albumCollection.Dispose();
#else
            foreach (var album in this.albumCollection)
                album.Dispose();
#endif
        }
    }
}
