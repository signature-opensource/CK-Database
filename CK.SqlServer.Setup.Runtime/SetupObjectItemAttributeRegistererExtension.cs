using CK.Core;
using CK.Setup;
using CK.SqlServer.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    public static class SetupObjectItemAttributeRegistererExtension
    {

        /// <summary>
        /// Builds a Sql context-location-name (with the <see cref="SqlContextLocName.Schema"/>) from a setup object 
        /// name (typically from an attribute) and its <see cref="SqlPackageBase"/> container that provides
        /// ambient context, location and schema if the <paramref name="attributeName"/> does not define them.
        /// When the behavior is <see cref="SetupObjectItemBehavior.Transform"/>, the name 
        /// </summary>
        /// <param name="this">This registerer.</param>
        /// <param name="b">The behavior (define, replace or transform).</param>
        /// <param name="attributeName">Name of the object defined in the attribute.</param>
        /// <returns>The Sql context-location-name.</returns>
        public static SqlContextLocName SqlBuildFullName( this SetupObjectItemAttributeRegisterer @this, SetupObjectItemBehavior b, string attributeName )
        {
            SqlPackageBaseItem p = (SqlPackageBaseItem)@this.Container;
            var name = new SqlContextLocName( attributeName );
            if( name.Context == null ) name.Context = p.Context;
            if( name.Location == null ) name.Location = p.Location;
            if( name.Schema == null ) name.Schema = p.ActualObject.Schema;
            // Now handling transformation.
            if( name.TransformArg != null )
            {
                // The provided name is a transformation: resolves context/location/schema from container 
                // on the target component if they are not define.
                var target = new SqlContextLocName( name.TransformArg );
                if( target.Context == null ) target.Context = name.Context;
                if( target.Location == null ) target.Location = name.Location;
                if( target.Schema == null ) target.Schema = name.Schema;
                name.TransformArg = target.FullName;
            }
            else if( b == SetupObjectItemBehavior.Transform )
            {
                // The name is not the name of a transformation however it should be:
                // we consider it to be the default transformation of the (target) name by the container.
                name = new SqlContextLocName( p.Context, p.Location, p.Name + '(' + name.FullName + ')' );
            }
            return name;
        }
    }
}
