#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Version\MultiVersionManager.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Offers an easy way to handle multiple versions definition. Can be used independently or as a base class 
    /// (for a mutable implementation of <see cref="IVersionedItem"/>).
    /// </summary>
    public class MultiVersionManager
    {
        CKSortedArrayList<Version> _versions;
        CKSortedArrayKeyList<VersionedName,Version> _previousNames;

        static Regex _rVersions = new Regex( @"((?<2>(\w|\.|-)+)\s*=)?\s*(?<1>\d+\.\d+\.\d+),?",
                RegexOptions.Singleline
                | RegexOptions.ExplicitCapture
                | RegexOptions.CultureInvariant
                | RegexOptions.Compiled );

        /// <summary>
        /// Initializes a new <see cref="MultiVersionManager"/> with a null <see cref="P:Version"/>.
        /// </summary>
        public MultiVersionManager()
        {
            _versions = new CKSortedArrayList<Version>();
        }

        /// <summary>
        /// Gets or sets the current version: it is the last one of the <see cref="VersionList"/>.
        /// Can be null: no version exists for this package. This property automatically 
        /// synchronises <see cref="VersionList"/>: newer versions are removed from the list.
        /// </summary>
        public Version Version
        {
            get { return _versions.Count > 0 ? _versions[_versions.Count - 1] : null; }
            set
            {
                if( value == null ) _versions.Clear();
                else
                {
                    _versions.Add( value );
                    int idx = _versions.IndexOf( value ) + 1;
                    while( idx < _versions.Count )
                    {
                        if( _previousNames != null )
                        {
                            Version v = _versions[_versions.Count - 1];
                            _previousNames.Remove( v );
                        }
                        _versions.RemoveAt( _versions.Count - 1 );
                    }
                }
            }
        }

        /// <summary>
        /// Gets the sorted list of existing versions. The current <see cref="Version"/> is the last one.
        /// When <see cref="Version"/> is null, this list is empty.
        /// </summary>
        public IReadOnlyList<Version> VersionList
        {
            get { return _versions; }
        }

        /// <summary>
        /// Adds a new existing version in the sorted list of versions.
        /// It automatically synchronises <see cref="Version"/>.
        /// </summary>
        /// <param name="version">The version to add.</param>
        /// <returns>True if the version have been added, false if the version already exists.</returns>
        public bool AddVersion( Version version )
        {
            if( version == null ) throw new ArgumentNullException( "version" );
            return _versions.Add( version );
        }

        /// <summary>
        /// Gets the sorted list of <see cref="VersionedName"/> if it exists (an empty list if no 
        /// previous names have been set).
        /// </summary>
        public ICKReadOnlyList<VersionedName> PreviousNames
        {
            get { return _previousNames ?? CKReadOnlyListEmpty<VersionedName>.Empty; }
        }

        /// <summary>
        /// Adds a previous named version in the sorted list of 
        /// versions (<see cref="VersionList"/>) and in <see cref="PreviousNames"/>.
        /// </summary>
        /// <param name="fullName">The name of the package valid for the <paramref name="version"/>.</param>
        /// <param name="version">The version asociated to the <paramref name="fullName"/>.</param>
        public void AddOrSetPreviousName( string fullName, Version version )
        {
            if( String.IsNullOrWhiteSpace( fullName ) ) throw new ArgumentException( "fullName" );
            if( version == null ) throw new ArgumentNullException( "version" );

            _versions.Add( version );
            EnsurePreviousName().Remove( version );
            _previousNames.Add( new VersionedName( fullName, version ) );
        }

        /// <summary>
        /// Sets the <see cref="VersionList"/> and <see cref="PreviousNames"/> from a string like: "1.2.4, Previous.Name = 1.3.1, A.New.Name=1.4.1, 1.5.0"
        /// The last version must NOT define a previous name since the last version is the current one: an <see cref="ArgumentException"/> is thrown.
        /// If null or empty, <see cref="VersionList"/> and <see cref="PreviousNames"/> are cleared and <see cref="Version"/> is set to null.
        /// </summary>
        /// <param name="versions">A comma separated list of versions (3 or 4 short integers), optionally associated to a previous name.</param>
        public void SetVersionsString( string versions )
        {
            if( _previousNames != null ) _previousNames.Clear();
            _versions.Clear();
            if( String.IsNullOrEmpty( versions ) ) return;

            Version version = null;
            MatchCollection c = _rVersions.Matches( versions );
            for( int i = 0; i < c.Count; ++i )
            {
                Match m = c[i];
                version = Version.Parse( m.Groups[1].Value );
                _versions.Add( version );
                if( m.Groups[2].Length > 0 )
                {
                    if( i == c.Count - 1 ) throw new ArgumentException( String.Format( "Last version can not define a previous name: {0}.", m ), "versions" );
                    EnsurePreviousName().Add( new VersionedName( m.Groups[2].Value, version ) );
                }
            }
        }

        private CKSortedArrayKeyList<VersionedName, Version> EnsurePreviousName()
        {
            if( _previousNames == null ) _previousNames = new CKSortedArrayKeyList<VersionedName, Version>( v => v.Version );
            return _previousNames;
        }

    }

}
