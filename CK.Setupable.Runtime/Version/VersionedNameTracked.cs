using System;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Captures original and updated version information.
    /// </summary>
    public class VersionedNameTracked
    {
        /// <summary>
        /// Initializes a new <see cref="VersionedNameTracked"/> bound to an original <see cref="VersionedTypedName"/>.
        /// </summary>
        /// <remarks>This constructor is public only to support unit tests.</remarks>
        /// <param name="original">The original. Can not be null.</param>
        public VersionedNameTracked( VersionedTypedName original )
        {
            Original = original;
            FullName = original.FullName;
        }

        /// <summary>
        /// Initializes a new <see cref="VersionedNameTracked"/> that is a new version (no <see cref="Original"/>).
        /// The <see cref="Accessed"/> marker is set to true.
        /// </summary>
        /// <remarks>This constructor is public only to support unit tests.</remarks>
        /// <param name="n">Full namle of the item.</param>
        /// <param name="t">Type of the item. Can not be null.</param>
        /// <param name="v">Version of the item. Can be null.</param>
        public VersionedNameTracked( string n, string t, Version v )
        {
            Debug.Assert( n != null && t != null );
            FullName = n;
            NewType = t;
            NewVersion = v;
            Accessed = true;
        }

        /// <summary>
        /// Gets the original version information.
        /// Null for new items.
        /// </summary>
        public VersionedTypedName Original { get; }

        /// <summary>
        /// Gets the full name.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Gets or sets whether this has been accessed: the <see cref="FullName"/> is still alive.
        /// </summary>
        public bool Accessed { get; set; }

        /// <summary>
        /// Gets whether this has been explicitly deleted. <see cref="SetNewVersion"/> has
        /// been called with a null version.
        /// </summary>
        public bool Deleted { get; private set; }

        /// <summary>
        /// Gets the new version. Null if no version has been explicitly set or <see cref="SetNewVersion"/> has
        /// been called with a null version..
        /// </summary>
        public Version NewVersion { get; private set; }

        /// <summary>
        /// Gets the new item type. Null if no version has been explicitly set.
        /// </summary>
        public string NewType { get; private set; }

        /// <summary>
        /// Sets the new version and type.
        /// This can not be called twice.
        /// </summary>
        /// <remarks>This method is public only to support unit tests.</remarks>
        /// <param name="v">The new version. Null to mark it as deleted.</param>
        /// <param name="type">The new type. Can not be null if v is not null.</param>
        public void SetNewVersion( Version v, string type )
        {
            if( Deleted || NewVersion != null ) throw new InvalidOperationException( $"New version has already been set on '{FullName}'." );
            if( v == null ) Deleted = true;
            else
            {
                NewVersion = v;
                if( type == null ) throw new ArgumentNullException( nameof( type ) );
                NewType = type;
            }
            Accessed = true;
        }
    }

}
