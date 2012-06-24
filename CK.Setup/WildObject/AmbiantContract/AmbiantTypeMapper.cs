using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    internal class AmbiantTypeMapper : IAmbiantTypeMapper
    {
        Dictionary<Type,Type> _map;
        Type _context;

        internal AmbiantTypeMapper( Type context, Dictionary<Type, Type> m )
        {
            _context = context;
            _map = m;
        }

        public Type Context 
        {
            get { return _context; }
        }

        public Type this[Type t]
        {
            get { return _map.GetValueWithDefault( t, null ); }
        }

        public int Count { get { return _map.Count; } }

        public bool IsMapped( Type t )
        {
            return _map.ContainsKey( t );
        }

    }
}
