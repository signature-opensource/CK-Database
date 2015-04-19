using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK
{
    public static class TypeExtension
    {

        public static bool HasBaseType( this Type @this, string fullName )
        {
            while( @this != null )
            {
                if( @this.FullName == fullName ) return true;
                @this = @this.BaseType;
            }
            return false;
        }

    }
}
