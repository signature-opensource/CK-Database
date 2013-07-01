using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    static public class StObjModelExtension
    {
        public static T Obtain<T>( this IContextualStObjMap @this )
        {
            return (T)@this.Obtain( typeof( T ) );
        }
    }
}
