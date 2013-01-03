using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    class BuildValueCollector
    {
        public readonly List<object> Values;

        public BuildValueCollector()
        {
            Values = new List<object>();
            Values.Add( null );
        }

        public int RegisterValue( object o )
        {
            if( o == null ) return 0;
            int idx = Values.IndexOf( o, 1 );
            if( idx < 0 )
            {
                idx = Values.Count;
                Values.Add( o );
            }
            return idx;
        }

    }
}
