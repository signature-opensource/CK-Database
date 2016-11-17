using CK.Core;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup
{
    public static class SetupObjectItemAttributeRegistererExtension
    {

        /// <summary>
        /// Builds a context-location-name from a setup object name (typically from the attribute) 
        /// in a <see cref="SqlPackageBase"/>. 
        /// </summary>
        /// <param name="this">This registerer.</param>
        /// <param name="b">The behavior (define, replace or transform).</param>
        /// <param name="attributeName">Name of the object defined in the attribute.</param>
        /// <returns>The context-location-name.</returns>
        public static IContextLocNaming SqlBuildFullName( this SetupObjectItemAttributeRegisterer @this, SetupObjectItemBehavior b, string attributeName )
        {
            SqlPackageBaseItem p = (SqlPackageBaseItem)@this.Container;
            var name = new SqlContextLocName( attributeName );
            if( name.Context == null ) name.Context = p.Context;
            if( name.Location == null ) name.Location = p.Location;
            if( name.Schema == null ) name.Schema = p.ActualObject.Schema;
            if( name.TransformArg != null )
            {
                var target = new SqlContextLocName( name.TransformArg );
                if( target.Context == null ) target.Context = name.Context;
                if( target.Location == null ) target.Location = name.Location;
                if( target.Schema == null ) target.Schema = name.Schema;
                name.TransformArg = target.FullName;
            }
            else
            {
                if( b == SetupObjectItemBehavior.Transform )
                {
                    name = new SqlContextLocName( p.Context, p.Location, p.Name + '(' + name.FullName + ')' );
                }
            }
            return name;
        }
    }
}
