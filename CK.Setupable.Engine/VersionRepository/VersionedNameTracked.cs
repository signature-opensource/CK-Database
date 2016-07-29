using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// Captures original and updated version information.
    /// </summary>
    public class VersionedNameTracked
    {
        internal VersionedNameTracked( VersionedTypedName original )
        {
            Original = original;
            FullName = original.FullName;
        }

        internal VersionedNameTracked( string n, string t, Version v )
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
        /// Gets whether this has been accessed: the <see cref="FullName"/> is still alive.
        /// </summary>
        public bool Accessed { get; internal set; }

        /// <summary>
        /// Gets whether this has been explicitely deleted.
        /// </summary>
        public bool Deleted { get; private set; }

        /// <summary>
        /// Gets the new version. Null if no version has been explicitely set.
        /// </summary>
        public Version NewVersion { get; private set; }

        /// <summary>
        /// Gets the new item type. Null if no version has been explicitely set.
        /// </summary>
        public string NewType { get; private set; }

        internal VersionedNameTracked SetNewVersion( Version v, string type )
        {
            Debug.Assert( (v == null) == (type == null) );
            if( Deleted || NewVersion != null ) throw new InvalidOperationException( $"New version has already been set on '{FullName}'." );
            if( v == null ) Deleted = true;
            else
            {
                NewVersion = v;
                NewType = type;
                Accessed = true;
            }
            return this;
        }
    }

}
