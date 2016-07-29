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
            using( monitor.OpenInfo().Send( "Reading original versions." ) )
            {
                try
                {
                    var originals = _versionReader.GetOriginalVersions( monitor );
                    if( originals == null ) monitor.Fatal().Send( "VersionedItemRepository must return a non null OriginalVersions." );
                    else
                    {
                        int nbRead = _tracker.Initialize( originals );
                        monitor.CloseGroup( $"Got {nbRead} versions." );
                        return true;
                    }
                }
                catch( Exception ex )
                {
                    monitor.Fatal().Send( ex );
                }
                return false;
            }
        }

        internal bool ConcludeWithFatalOnError( IActivityMonitor monitor, IVersionedItemWriter writer, bool setupSuccess )
        {
            try
            {
                writer.SetVersions( monitor, _versionReader, _tracker.All, setupSuccess );
                return true;
            }
            catch( Exception ex )
            {
                monitor.Fatal().Send( ex, "While saving versions." );
                return false;
            }
        }
        /// <summary>
        /// Gets the current <see cref="VersionedName"/> for a given <see cref="IVersionedItem"/> or null
        /// if not found. 
        /// Item's <see cref="IVersionedItem.PreviousNames"/> are used if provided to handle renaming.
        /// </summary>
        /// <param name="i">The versionned item.</param>
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
        /// <param name="i">The versioned item to update.</param>
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