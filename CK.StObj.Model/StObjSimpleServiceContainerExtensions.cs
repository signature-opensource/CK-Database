using System;

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
        /// <param name="singletonOnly">
        /// Lifetime Scoped and Singleton filtering. When null, both kind are added.
        /// When true, only singletons are added.
        /// When false, only scoped services are added: this must be called on a container
        /// that already knows the singletons (typically in its <see cref="SimpleServiceContainer.BaseProvider"/>).
        /// </param>
        /// <returns>The container to enable fluent syntax.</returns>
        public static ISimpleServiceContainer AddStObjMap( this ISimpleServiceContainer container, IStObjMap map, bool? singletonOnly = null )
        {
            if( map == null ) throw new ArgumentNullException( nameof( map ) );
            // StObjs are Singletons: the StObjMap has alreay created the instances.
            if( singletonOnly != false )
            {
                foreach( var kv in map.StObjs.Mappings )
                {
                    container.Add( kv.Key, kv.Value );
                }
            }
            // Services: instances are created on demand and cached.
            // 1 - Direct type mapping: use the local Create helper.
            foreach( var kv in map.Services.SimpleMappings )
            {
                if( !singletonOnly.HasValue || singletonOnly.Value != kv.Value.IsScoped )
                {
                    container.Add( kv.Key, () => Create( kv.Value.ClassType, container ) );
                }
            }
            // 2 - Manual type: Use the automatically generated code.
            foreach( var kv in map.Services.ManualMappings )
            {
                if( !singletonOnly.HasValue || singletonOnly.Value != kv.Value.IsScoped )
                {
                    container.Add( kv.Key, () => kv.Value.CreateInstance( container ) );
                }
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
