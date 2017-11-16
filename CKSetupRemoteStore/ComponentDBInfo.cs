using CK.Core;
using CKSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;

namespace CKSetupRemoteStore
{
    public class ComponentDBInfo
    {
        /// <summary>
        /// Initializes a new <see cref="ComponentDBInfo"/>.
        /// </summary>
        /// <param name="db">The component database. Can be null.</param>
        public ComponentDBInfo( ComponentDB db )
        {
            if( db == null ) db = ComponentDB.Empty;
            ComponentDB = db;
            var v = new Visitor();
            v.Visit( db );
            NamedComponentCount = v._componentNames.Count();
            TotalComponentCountPerFramework = v._totalComponentCountPerFramework;
            BiggestFiles = v._biggestFiles;
            SmallestFiles = v._smallestFiles;
            ComponentsFilesCount = v._componentsFilesCount;
            ComponentsTotalFilesSize = v._componentsTotalFilesSize;
            StoredFilesCount = v._storedFilesCount;
            StoredTotalFilesSize = v._storedTotalFilesSize;
        }

        class Visitor : ComponentDBVisitor
        {
            class DedupDisplay : IEqualityComparer<ComponentFile>
            {
                public bool Equals( ComponentFile x, ComponentFile y )
                {
                    return x.Name == y.Name
                            && x.FileVersion == y.FileVersion
                            && x.AssemblyVersion == y.FileVersion
                            && x.Length == y.Length;
                }

                public int GetHashCode( ComponentFile o )
                {
                    return Util.Hash.Combine( o.Name.GetHashCode(), o.FileVersion, o.AssemblyVersion, o.Length ).GetHashCode();
                }
            }

            readonly HashSet<ComponentFile> _files;
            readonly HashSet<ComponentFile> _filesDedupDisplay;
            internal readonly HashSet<string> _componentNames;
            internal readonly Dictionary<TargetFramework, int> _totalComponentCountPerFramework;
            internal readonly BestKeeper<ComponentFile> _biggestFiles;
            internal readonly BestKeeper<ComponentFile> _smallestFiles;
            internal int _componentsFilesCount;
            internal long _componentsTotalFilesSize;
            internal int _storedFilesCount;
            internal long _storedTotalFilesSize;

            public Visitor( int bestCount = 10 )
            {
                _files = new HashSet<ComponentFile>();
                _filesDedupDisplay = new HashSet<ComponentFile>( new DedupDisplay() );
                _componentNames = new HashSet<string>();
                _biggestFiles = new BestKeeper<ComponentFile>( bestCount, ( f1, f2 ) => f2.Length - f1.Length );
                _smallestFiles = new BestKeeper<ComponentFile>( bestCount, ( f1, f2 ) => f1.Length - f2.Length );
                _totalComponentCountPerFramework = new Dictionary<TargetFramework, int>();
            }

            protected override Component VisitComponent( ComponentDB db, int idxComponent, Component c )
            {
                _componentNames.Add( c.Name );
                _totalComponentCountPerFramework.TryGetValue( c.TargetFramework, out int frameworkcount );
                _totalComponentCountPerFramework[c.TargetFramework] = ++frameworkcount;
                return base.VisitComponent( db, idxComponent, c );
            }

            protected override IReadOnlyList<ComponentFile> VisitComponentFiles( ComponentDB db, int idxComponent, Component c )
            {
                _componentsFilesCount += c.Files.Count;
                foreach( var f in c.Files )
                {
                    _componentsTotalFilesSize += f.Length;
                    if( _files.Add( f ) )
                    {
                        _storedFilesCount++;
                        _storedTotalFilesSize += f.Length;
                        if( !f.Name.EndsWith( ".json", StringComparison.OrdinalIgnoreCase ) )
                        {
                            if( _filesDedupDisplay.Add( f ) )
                            {
                                _smallestFiles.Add( f );
                                _biggestFiles.Add( f );
                            }
                        }
                    }
                }
                return c.Files;
            }
        }

        /// <summary>
        /// Gets the component database.
        /// </summary>
        public ComponentDB ComponentDB { get; }

        /// <summary>
        /// Gets the total number of components.
        /// </summary>
        public int TotalComponentCount => ComponentDB.Components.Count;

        /// <summary>
        /// Gets the number of components based on their name.
        /// </summary>
        public int NamedComponentCount { get; }

        /// <summary>
        /// Gets the number of components per <see cref="TargetFramework"/>.
        /// </summary>
        public IReadOnlyDictionary<TargetFramework, int> TotalComponentCountPerFramework { get; }

        /// <summary>
        /// Gets the biggest stored files.
        /// </summary>
        public IReadOnlyList<ComponentFile> BiggestFiles { get; }

        /// <summary>
        /// Gets the smallest stored files.
        /// </summary>
        public IReadOnlyList<ComponentFile> SmallestFiles { get; }

        /// <summary>
        /// Gets the sum of all <see cref="Component.Files"/> (as if there were no actual file sharing).
        /// </summary>
        public int ComponentsFilesCount { get; }

        /// <summary>
        /// Gets the sum of all <see cref="Component.Files"/> in bytes (as if there were no actual file sharing).
        /// </summary>
        public long ComponentsTotalFilesSize { get; }

        /// <summary>
        /// Gets the number of actual different files stored.
        /// </summary>
        public int StoredFilesCount { get; }

        /// <summary>
        /// Gets the sum of the size in bytes of all stored files. 
        /// </summary>
        public long StoredTotalFilesSize { get; }
    }
}
