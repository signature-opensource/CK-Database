using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Provides AddStObjMap method on <see cref="ISimpleServiceContainer"/>.
    /// </summary>
    public static class StObjSimpleServiceContainerExtensions
    {
        /// <summary>
        /// Registers the <see cref="IStObjMap.StObjs"/>, the <see cref="IStObjMap"/> itself
        /// and <see cref="IStObjMap.Services"/> from a <see cref="IStObjMap"/>.
        /// </summary>
        /// <param name="container">This container.</param>
        /// <param name="map">The StObjMap to register. Must not be null.</param>
        /// <returns>The container to enable fluent syntax.</returns>
        public static ISimpleServiceContainer AddStObjMap( this ISimpleServiceContainer container, IStObjMap map )
        {
            if( map == null ) throw new ArgumentNullException( nameof( map ) );
            // Singletons: the StObjMap has alreay created the instances.
            foreach( var kv in map.StObjs.Mappings )
            {
                container.Add( kv.Key, kv.Value );
            }
            // Scoped (created on demand and cached).
            // 1 - Direct type mapping: use the local Create helper.
            foreach( var kv in map.Services.SimpleMappings )
            {
                container.Add( kv.Key, () => Create( kv.Value, container ) );
            }
            // 2 - Manual type: Use the automatically generated code.
            foreach( var kv in map.Services.ManualMappings )
            {
                container.Add( kv.Key, () => kv.Value.CreateInstance( container ) );
            }
            return container;
        }

        static object Create( Type t, IServiceProvider services )
        {
            if( t == null ) throw new ArgumentNullException( nameof( t ) );
            var ctors = t.GetConstructors();
            if( ctors.Length == 0 || ctors.Length != 1 )
            {
                throw new Exception( $"Unable to find single a public constructor for '{t.FullName}'." );
            }
            var parameters = ctors[0].GetParameters();
            object[] values = new object[parameters.Length];
            for( int i = 0; i < parameters.Length; ++i )
            {
                var p = parameters[i];
                var resolved = services.GetService( p.ParameterType );
                if( resolved == null && !p.HasDefaultValue )
                {
                    throw new Exception( $"Resolution failed for '{t.FullName}': parameter '{p.Name}', type: '{p.ParameterType.Name}'." );
                }
                values[i] = resolved;
            }
            return ctors[0].Invoke( values );
        }
    }
}
