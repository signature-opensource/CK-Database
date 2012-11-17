using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    internal class MutableReferenceList : List<MutableReference>, IMutableReferenceList
    {
        MutableItem _owner;
        MutableReferenceKind _kind;

        internal MutableReferenceList( MutableItem owner, MutableReferenceKind kind )
        {
            _owner = owner;
            _kind = kind;
        }

        // To disambiguate types.
        internal List<MutableReference> AsList { get { return this; } }

        public IMutableReference AddNew( Type t, string context = null )
        {
            var m = new MutableReference( _owner, _kind ) { Type = t, Context = context };
            Add( m );
            return m;
        }

        public int IndexOf( object item )
        {
            MutableReference m = item as MutableReference;
            return m != null ? IndexOf( m ) : Int32.MaxValue;
        }

        public bool Contains( object item )
        {
            return IndexOf( item ) >= 0;
        }

        IMutableReference IReadOnlyList<IMutableReference>.this[int index]
        {
            get { return this[index]; }
        }

        IEnumerator<IMutableReference> IEnumerable<IMutableReference>.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
