using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CKSetup
{
    /// <summary>
    /// Collects multiple <see cref="Component.Files"/> into one <see cref="Result"/> file list.
    /// </summary>
    class ComponentFileCollector
    {
        readonly Dictionary<string, List<KeyValuePair<Component,ComponentFile>>> _files;
        readonly bool _keepFirst;

        /// <summary>
        /// Initializes a new <see cref="ComponentFileCollector"/> that keeps the first file
        /// it found when versions are equals.
        /// </summary>
        /// <param name="keepFirst">True to keep the first version, false to keep the last one.</param>
        public ComponentFileCollector( bool keepFirst = true )
        {
            _files = new Dictionary<string, List<KeyValuePair<Component, ComponentFile>>>( StringComparer.OrdinalIgnoreCase );
            _keepFirst = keepFirst;
        }

        /// <summary>
        /// Gets the final set of files.
        /// </summary>
        public IEnumerable<ComponentFile> Result => _files.Values.Select( l => l[0].Value );

        /// <summary>
        /// Gets the number of final files.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the final set of file versions: the first item of each list
        /// is the one to use, the remainder of the lists have been evicted.
        /// </summary>
        public IEnumerable<IReadOnlyList<KeyValuePair<Component, ComponentFile>>> DetailedResult => _files.Values;


        /// <summary>
        /// Dumps detailed results in monitor.
        /// </summary>
        /// <param name="m">Monitor to use.</param>
        public void DumpResult( IActivityMonitor m )
        {
            using( m.OpenInfo( $"Selected {_files.Count} setup files." ) )
            {
                foreach( var nKV in _files.OrderBy( x => x.Key ) )
                {
                    var list = nKV.Value;
                    var best = list[0];
                    using( m.OpenInfo( best.Value.ToDisplayString() ) )
                    {
                        m.Info( $"From: {list.Where( kv => kv.Value.SHA1 == best.Value.SHA1 ).Select( kv => kv.Key.Name).Concatenate()}" );
                        var others = list.Skip( 1 ).Where( kv => kv.Value.SHA1 != best.Value.SHA1 ).ToList();
                        foreach( var o in others )
                        {
                            m.Info( $"Skipped {o.Value} from {o.Key}" );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a whole set of <see cref="Component.Files"/> to this collector.
        /// </summary>
        /// <param name="components">Components to add.</param>
        public void Add( IEnumerable<Component> components )
        {
            foreach( var c in components ) Add( c );
        }

        /// <summary>
        /// Adds <see cref="Component.Files"/> to this collector.
        /// </summary>
        /// <param name="comp">Component to add.</param>
        public void Add( Component comp )
        {
            foreach( var f in comp.Files ) Add( comp, f );
        }

        static int Compare( KeyValuePair<Component, ComponentFile> x, KeyValuePair<Component, ComponentFile> y )
        {
            return x.Value.AssemblyVersion?.CompareTo( y.Value.AssemblyVersion )
                    ?? x.Value.FileVersion?.CompareTo( y.Value.FileVersion )
                    ?? 0;
        }

        void Add( Component comp, ComponentFile f )
        {
            var candidate = new KeyValuePair<Component, ComponentFile>( comp, f );
            List<KeyValuePair<Component, ComponentFile>> exists;
            if( _files.TryGetValue( f.Name, out exists ) )
            {
                int idx = _keepFirst
                            ? (Compare( candidate, exists[0] ) > 0 ? 0 : 1)
                            : (Compare( candidate, exists[0] ) >= 0 ? 0 : 1);
                exists.Insert( idx, candidate );
            }
            else
            {
                _files.Add( f.Name, new List<KeyValuePair<Component, ComponentFile>>() { candidate } );
            }
        }

    }
}
