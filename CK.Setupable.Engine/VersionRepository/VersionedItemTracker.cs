using System;
using System.Collections.Generic;
using CK.Core;
using System.Linq;

namespace CK.Setup
{

    /// <summary>
    /// Handles tracking of <see cref="IVersionedItem"/> fullname/version.
    /// </summary>
    class VersionedItemTracker
    {
        class TrackerHelper
        {
            readonly Dictionary<string, VersionedNameTracked> _versions;

            public TrackerHelper()
            {
                _versions = new Dictionary<string, VersionedNameTracked>();
            }

            /// <summary>
            /// Initializes the internal dictionary of <see cref="VersionedNameTracked"/>
            /// for each <see cref="VersionedTypedName"/> that have been read.
            /// </summary>
            /// <param name="originals">The original VersionedTypeName set.</param>
            /// <returns>The number of versions.</returns>
            internal int Initialize( IEnumerable<VersionedTypedName> originals )
            {
                foreach( var v in originals )
                {
                    _versions.Add( v.FullName, new VersionedNameTracked( v ) );
                }
                return _versions.Count;
            }

            public VersionedTypedName GetOriginalVersion( string fullName )
            {
                VersionedNameTracked v;
                if( !_versions.TryGetValue( fullName, out v ) ) return null;
                return v.Original;
            }

            /// <summary>
            /// Gets the version for a component, optionnaly marking it as having being <see cref="VersionedNameTracked.Accessed"/>.
            /// </summary>
            /// <param name="fullName">The full name of the component.</param>
            /// <param name="setAccess">True to set access, false to silently retrieve the version.</param>
            /// <returns>The version or null if not found.</returns>
            public Version GetVersion( string fullName, bool setAccess )
            {
                VersionedNameTracked v;
                if( _versions.TryGetValue( fullName, out v ) )
                {
                    if( setAccess ) v.Accessed = true;
                    return v.Original?.Version;
                }
                return null;
            }

            /// <summary>
            /// Sets a version that can be explicitly null for a deletion.
            /// </summary>
            /// <param name="fullName">The component's full name.</param>
            /// <param name="v">The version. Null for deletion.</param>
            /// <param name="type">Component's type.</param>
            public void SetVersion( string fullName, Version v, string type )
            {
                VersionedNameTracked t;
                if( _versions.TryGetValue( fullName, out t ) )
                {
                    t.SetNewVersion( v, type );
                }
                else
                {
                    if( v != null ) _versions.Add( fullName, new VersionedNameTracked( fullName, type, v ) );
                }
            }

            internal IEnumerable<VersionedNameTracked> All => _versions.Values;
        }

        readonly TrackerHelper _tracker;
        readonly IVersionedItemReader _versionReader;

        internal VersionedItemTracker( IVersionedItemReader versionRepository )
        {
            _tracker = new TrackerHelper();
            _versionReader = versionRepository;
        }

        public bool Initialize( IActivityMonitor monitor )
        {
            using( monitor.OpenInfo( "Reading original versions." ) )
            {
                try
                {
                    var originals = _versionReader.GetOriginalVersions( monitor );
                    if( originals == null ) monitor.Fatal( "VersionedItemRepository must return a non null OriginalVersions." );
                    else
                    {
                        int nbRead = _tracker.Initialize( originals );
                        monitor.CloseGroup( $"Got {nbRead} versions." );
                        return true;
                    }
                }
                catch( Exception ex )
                {
                    monitor.Fatal( ex );
                }
                return false;
            }
        }

        /// <summary>
        /// Writes updated versions to a <see cref="IVersionedItemWriter"/>.
        /// </summary>
        /// <param name="monitor">The monitor that will be used.</param>
        /// <param name="writer">The version writer.</param>
        /// <param name="deleteUnaccessedItems">True to delete non accessed names.</param>
        /// <returns>True on success, false on error.</returns>
        internal bool ConcludeWithFatalOnError( IActivityMonitor monitor, IVersionedItemWriter writer, bool deleteUnaccessedItems )
        {
            try
            {
                writer.SetVersions( monitor, _versionReader, _tracker.All, deleteUnaccessedItems );
                return true;
            }
            catch( Exception ex )
            {
                monitor.Fatal( "While saving versions.", ex );
                return false;
            }
        }

        /// <summary>
        /// Gets the current <see cref="VersionedName"/> for a given <see cref="IVersionedItem"/> or null
        /// if not found. 
        /// Item's <see cref="IVersionedItem.PreviousNames"/> are used if provided to handle renaming.
        /// </summary>
        /// <param name="item">The versionned item.</param>
        /// <returns>The versionned name that contains the name and the version of the item stored in this repository or null.</returns>
        public VersionedName GetCurrent( IVersionedItem item )
        {
            VersionedName result = null;
            item.CheckVersionItemArgument( nameof( item ) );
            Version v = _tracker.GetVersion( item.FullName, true );
            if( v != null ) result = new VersionedName( item.FullName, v );
            else
            {
                result = _versionReader.OnVersionNotFound( item, _tracker.GetOriginalVersion );
                if( result == null )
                {
                    IEnumerable<VersionedName> prev = item.PreviousNames;
                    if( prev != null )
                    {
                        foreach( var prevVersion in prev.Reverse() )
                        {
                            v = _tracker.GetVersion( prevVersion.FullName, false );
                            if( v != null ) result = new VersionedName( prevVersion.FullName, v );
                            else result = _versionReader.OnPreviousVersionNotFound( item, prevVersion, _tracker.GetOriginalVersion );
                            if( result != null ) break;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Updates the current version.
        /// If the <see cref="IVersionedItem.Version"/> is null, it is the same as calling <see cref="Delete"/>.
        /// </summary>
        /// <param name="item">The versioned item to update.</param>
        public void SetCurrent( IVersionedItem item )
        {
            item.CheckVersionItemArgument( nameof( item ) );
            _tracker.SetVersion( item.FullName, item.Version, item.ItemType );
        }

        /// <summary>
        /// Deletes the given item from the repository.
        /// Version is not required here: the item with the provided name will 
        /// be deleted regardless of its version.
        /// </summary>
        /// <param name="fullName">The <see cref="IDependentItem.FullName">FullName</see> to remove.</param>
        public void Delete( string fullName )
        {
            if( string.IsNullOrWhiteSpace( fullName ) ) throw new ArgumentNullException( nameof( fullName ) );
            _tracker.SetVersion( fullName, null, null );
        }

    }
}
