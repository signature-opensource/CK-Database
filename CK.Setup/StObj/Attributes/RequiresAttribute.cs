using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class RequiresAttribute : Attribute
    {
        readonly string _requires;

        /// <summary>
        /// Defines requirements by their names.
        /// </summary>
        /// <param name="requires">Comma separated list of requirement item names.</param>
        public RequiresAttribute( string requires )
        {
            _requires = requires;
        }

        static internal DependentItemList GetRequirements( IActivityLogger logger, Type t, Type attrType )
        {
            Debug.Assert( logger != null );
            Debug.Assert( t != null );
            Debug.Assert( attrType != null && typeof( RequiresAttribute ).IsAssignableFrom( attrType ) );
            DependentItemList result = new DependentItemList();
            var all = (RequiresAttribute[])t.GetCustomAttributes( attrType, false );
            foreach( var a in all )
            {
                result.AddCommaSeparatedString( a._requires );
            }
            return result;
        }

    }
}
