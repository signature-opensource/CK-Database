using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Collects injected values for properties and constructors.
    /// Once stored in <see cref="Values"/>, the index of the object is used.
    /// </summary>
    class BuildValueCollector
    {
        /// <summary>
        /// All the injected values.
        /// </summary>
        public readonly List<object> Values;

        public BuildValueCollector()
        {
            Values = new List<object>();
            Values.Add( null );
        }

        /// <summary>
        /// Registers a value. Uses standard equal check (<see cref="List{T}.IndexOf"/> is called) to 
        /// store same object value only once.
        /// The null reference is added at the start (its index is 0).
        /// </summary>
        /// <param name="o">The value to store.</param>
        /// <returns>The index of the stored value.</returns>
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
