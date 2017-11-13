using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCake
{
    static class EnumerableExtensions
    {
        public static IEnumerable<T> BringToFront<T>( this IEnumerable<T> @this, Func<T,bool> predicate )
        {
            List<T> remainder = new List<T>();
            foreach( var e in @this )
            {
                if( predicate( e ) ) yield return e;
                else remainder.Add( e );
            }
            foreach( var e in remainder ) yield return e;
        }
    }
}
