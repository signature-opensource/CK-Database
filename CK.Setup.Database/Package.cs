using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CK.Core;

namespace CK.Setup.Database
{
    public class Package : ISetupableItemContainer, IDependentItemDiscoverer
    {
        string _fullName;
        IDependentItemContainerRef _container;
        List<string> _requires;
        List<string> _requiredBy;
        SortedArrayList<Version> _versions;
        SortedArrayKeyList<VersionedName,Version> _previousNames;
        List<IDependentItemRef> _children;
        PackageModel _model;

        static Regex _rVersions = new Regex( @"((?<2>(\w|\.|-)+)\s*=)?\s*(?<1>\d+\.\d+\.\d+),?",
                        RegexOptions.Singleline
                        | RegexOptions.ExplicitCapture
                        | RegexOptions.CultureInvariant
                        | RegexOptions.Compiled );


        public Package()
        {
            _versions = new SortedArrayList<Version>();
        }

        /// <summary>
        /// Gets the optional <see cref="PackageModel"/> for this <see cref="Package"/>.
        /// It is null if this package has no Model: use <see cref="EnsureModel"/> to
        /// create the Model if needed.
        /// </summary>
        public PackageModel Model
        {
            get { return _model; }
        }

        public PackageModel EnsureModel()
        {
            return _model ?? (_model = new PackageModel( this ));
        }

        public void SupressModel()
        {
            _model = null;
        }

        public string FullName
        {
            get { return _fullName; }
            set { _fullName = value ?? String.Empty; }
        }

        public IDependentItemContainerRef Container
        {
            get { return _container; }
            set { _container = value; }
        }

        public IList<string> Requires
        {
            get { return _requires ?? (_requires = new List<string>()); }
        }

        public IList<string> RequiredBy
        {
            get { return _requiredBy ?? (_requiredBy = new List<string>()); }
        }

        /// <summary>
        /// Splits the parameter on the comma and appends the stings in <see cref="Requires"/>.
        /// </summary>
        /// <param name="commaSeparatedRequires">Comma separated requires. When null or empty, nothing is added.</param>
        public void AddRequiresString( string commaSeparatedRequires )
        {
            if( !String.IsNullOrWhiteSpace( commaSeparatedRequires ) )
            {
                Requires.AddRangeArray( commaSeparatedRequires.Split( new[] { ',', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries ) );
            }
        }

        /// <summary>
        /// Splits the parameter on the comma and appends the stings in <see cref="RequiredBy"/>.
        /// </summary>
        /// <param name="commaSeparatedRequiredBy">Comma separated requires. When null or empty, nothing is added.</param>
        public void AddRequiredByString( string commaSeparatedRequiredBy )
        {
            if( !String.IsNullOrWhiteSpace( commaSeparatedRequiredBy ) )
            {
                RequiredBy.AddRangeArray( commaSeparatedRequiredBy.Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries ) );
            }
        }

        /// <summary>
        /// Gets or sets the current version: it is the last one of the <see cref="VersionList"/>.
        /// Can be null: no version exists for this package. This property automatically 
        /// synchronises <see cref="VersionList"/>: older versions are removed from the list.
        /// </summary>
        public Version Version
        {
            get { return _versions.Count > 0 ? _versions[_versions.Count-1] : null; }
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
        /// When <see cref="Version"/> is null, ths list is empty.
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
        /// Adds a previous named version in the sorted list of 
        /// versions (<see cref="VersionList"/>) and in <see cref="PreviousNames"/>.
        /// </summary>
        /// <param name="fullName">The name of the package valid for the <paramref name="version"/>.</param>
        /// <param name="version">The version asociated to the <see cref="fullName"/>.</param>
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
        /// The last version must NOT define a previous name since the last version is the current one: the <see cref="FullName"/>: an <see cref="ArgumentException"/> is thrown.
        /// If null or empty, <see cref="VersionList"/> and <see cref="PreviousNames"/> are cleared and <see cref="Version"/> is set to null.
        /// </summary>
        /// <param name="versions">A comma separated list of versions (3 integers), optionally associated to a previous name.</param>
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

        private SortedArrayKeyList<VersionedName, Version> EnsurePreviousName()
        {
            if( _previousNames == null ) _previousNames = new SortedArrayKeyList<VersionedName, Version>( v => v.Version );
            return _previousNames;
        }

        public IReadOnlyList<VersionedName> PreviousNames
        {
            get { return _previousNames ?? ReadOnlyListEmpty<VersionedName>.Empty; }
        }

        public IList<IDependentItemRef> Children
        {
            get { return _children ?? (_children = new List<IDependentItemRef>()); }
        }

        string ISetupableItem.SetupDriverTypeName
        {
            get { return typeof(SetupDriverContainer).AssemblyQualifiedName; }
        }

        IEnumerable<string> IDependentItem.Requires
        {
            get { return _requires; }
        }

        IEnumerable<string> IDependentItem.RequiredBy
        {
            get { return _requiredBy; }
        }

        IEnumerable<VersionedName> IVersionedItem.PreviousNames
        {
            get { return _previousNames; }
        }

        string IVersionedItem.ItemType
        {
            get { return "Package"; }
        }

        IEnumerable<IDependentItemRef> IDependentItemContainer.Children
        {
            get { return _children; }
        }

        IEnumerable<IDependentItem> IDependentItemDiscoverer.GetDependentItems()
        {
            return _model != null ? new ReadOnlyListMono<IDependentItem>( _model ) : null;
        }


    }


}
