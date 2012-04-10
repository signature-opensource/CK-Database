using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public sealed class DependentItemIssue
    {
        string[] _missingDep;
        string[] _extraneousContainers;
        string[] _missingChildren;
        IDependentItem[] _homonyms;
        int _nbRequiredMissingDep;
        
        /// <summary>
        /// Constructor for a missing named container or other structure errors.
        /// This may be called with a <see cref="DependentItemStructureError.None"/>
        /// status to register the very first optional missing dependency.
        /// </summary>
        internal DependentItemIssue( IDependentItem item, DependentItemStructureError m )
        {
            Item = item;
            StructureError = m;
        }

        internal void AddHomonym( IDependentItem homonym )
        {
            Debug.Assert( homonym != null );
            Debug.Assert( Item != homonym && Item.FullName == homonym.FullName );
            Append( ref _homonyms, homonym );
        }

        internal void AddMissing( string missing, bool isStrong )
        {
            Debug.Assert( !String.IsNullOrWhiteSpace( missing ) );
            Debug.Assert( isStrong == (missing[0] != '?') );
            if( _missingDep == null )
            {
                _missingDep = new[] { missing };
            }
            else
            {
                Debug.Assert( Array.IndexOf( _missingDep, missing ) < 0, "Duplicates are handled by ComputeRank." );
                int len = _missingDep.Length;
                // This is to maintain the fact that a strong missing 
                // dependency hides an optional one.
                if( isStrong )
                {
                    string weak = '?' + missing;
                    int idx = Array.IndexOf( _missingDep, weak );
                    if( idx >= 0 )
                    {
                        StructureError |= DependentItemStructureError.MissingDependency;
                        ++_nbRequiredMissingDep;
                        _missingDep[idx] = missing;
                        return;
                    }
                }
                Array.Resize( ref _missingDep, len + 1 );
                _missingDep[len] = missing;
            }
            if( isStrong )
            {
                StructureError |= DependentItemStructureError.MissingDependency;
                ++_nbRequiredMissingDep;
            }
        }

        internal void AddExtraneousContainers( string name )
        {
            Append( ref _extraneousContainers, name );
        }

        internal void AddMissingChild( string name )
        {
            Append( ref _missingChildren, name );
        }

        private void Append<T>( ref T[] a, T e )
        {
            if( a == null ) a = new T[] { e };
            else
            {
                int len = a.Length;
                Array.Resize( ref a, len + 1 );
                a[len] = e;
            }
        }

        public int RequiredMissingCount
        {
            get { return _nbRequiredMissingDep; }
        }

        public readonly IDependentItem Item;

        /// <summary>
        /// Gets a bit flag that summarizes the different errors related to structure 
        /// </summary>
        public DependentItemStructureError StructureError { get; internal set; }

        /// <summary>
        /// Gets the list of conflicting containers if any. Never null.
        /// </summary>
        public IEnumerable<string> ExtraneousContainers
        {
            get { return _extraneousContainers ?? Util.EmptyStringArray; }
        }

        /// <summary>
        /// Gets the list of <see cref="IDependentItem"/> that share the same name. Never null.
        /// </summary>
        public IEnumerable<IDependentItem> Homonyms
        {
            get { return _homonyms ?? new IDependentItem[0]; }
        }

        /// <summary>
        /// Gets the list of missing children if any (when named references are used). Never null.
        /// </summary>
        public IEnumerable<string> MissingChildren
        {
            get { return _missingChildren ?? Util.EmptyStringArray; }
        }

        /// <summary>
        /// Gets a list of missing dependencies either optional (starting with '?') or required. 
        /// Use <see cref="RequiredMissingCount"/> to know if required dependencies exist. 
        /// It is never null and there are no duplicates in this list and a required dependency "hides" an optional one:
        /// if a dependency is both required and optional, only the required one appears in this list.
        /// </summary>
        public IEnumerable<string> MissingDependencies { get { return _missingDep ?? Util.EmptyStringArray; } }
    }

}
