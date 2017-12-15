using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CK.Testing
{
    public class TestHelperResolver : ITestHelperResolver
    {
        readonly ITestHelperConfiguration _config;
        readonly SimpleServiceContainer _container;

        public TestHelperResolver( ITestHelperConfiguration config )
        {
            _container = new SimpleServiceContainer();
            _container.Add<ITestHelperConfiguration>( config );
            _config = config;
            TransientMode = config.GetBoolean( "Resolver/TransientMode" ) ?? false;
        }

        /// <summary>
        /// Gets whether the test helper services must be resolved as transient ones.
        /// Defaults to false: the services are by default singletons.
        /// To activate transient mode, the configuration "Resolver/TransientMode" must be "true".
        /// </summary>
        public bool TransientMode { get; }

        /// <summary>
        /// Resolves an instance, either a singleton or a transient one depending on <see cref="TransientMode"/>.
        /// This method throw exceptions on failure and this is intended: test framework must be fully operational
        /// and any error are considered developper errors.
        /// </summary>
        /// <param name="t">The type to resolve.</param>
        /// <returns>The resolved instance.</returns>
        public object Resolve( Type t )
        {
            var container = TransientMode
                                ? new SimpleServiceContainer( _container )
                                : _container;
            return Resolve( container, t, true );
        }

        object Resolve( ISimpleServiceContainer container, Type t, bool throwOnError )
        {
            object result = container.GetService( t );
            if( result == null && t != typeof(ITestHelper) && t != typeof(IMixinTestHelper) )
            {
                if( !t.IsClass || t.IsAbstract )
                {
                    Type tMapped = MapType( t, throwOnError );
                    result = Create( container, tMapped, throwOnError );
                    if( result != null && !tMapped.Assembly.IsDynamic ) container.Add( tMapped, result );
                }
                else result = Create( container, t, throwOnError );
                if( result != null ) container.Add( t, result );
            }
            return result;
        }

        Type MapType( Type t, bool throwOnError )
        {
            Debug.Assert( t != typeof( ITestHelper ) && t != typeof( IMixinTestHelper ) );
            string typeName = _config.Get( "TestHelper/" + t.FullName );
            if( typeName != null )
            {
                // Always throw when config is used.
                Type fromConfig = SimpleTypeFinder.WeakResolver( typeName, true );
                if( typeof(IMixinTestHelper).IsAssignableFrom(fromConfig))
                {
                    throw new Exception( $"Mapped type '{fromConfig.FullName}' is a Mixin. It can not be explicitely implemented." );
                }
                return fromConfig;
            }
            if( t.IsInterface && t.Name[0] == 'I' )
            {
                var cName = t.Name.Substring( 1 );
                string fullName = $"{t.Namespace}.{cName}, {t.Assembly.FullName}";
                Type found = SimpleTypeFinder.WeakResolver( fullName, false );
                if( found == null && cName.EndsWith( "Core" ) )
                {
                    fullName = $"{t.Namespace}.{cName.Remove( cName.Length - 4 )}, {t.Assembly.FullName}";
                    found = SimpleTypeFinder.WeakResolver( fullName, false );
                }
                if( found != null && t.IsAssignableFrom( found ) )
                {
                    return found;
                }
                if( typeof( IMixinTestHelper ).IsAssignableFrom( t ) )
                {
                    if( t.GetMembers().Length > 0 )
                    {
                        throw new Exception( $"Interface '{t.FullName}' is a Mixin. It can not have members of its own." );
                    }
                    return MixinType.Create( t );
                }
            }
            if( !throwOnError ) return null;
            throw new Exception( $"Unable to locate an implementation for {t.AssemblyQualifiedName}." );
        }

        object Create( ISimpleServiceContainer container, Type t, bool throwOnError )
        {
            Debug.Assert( t != null && t.IsClass && !t.IsAbstract );
            var longestCtor = t.GetConstructors()
                                .Select( x => Tuple.Create( x, x.GetParameters() ) )
                                .OrderByDescending( x => x.Item2.Length )
                                .Select( x => new
                                {
                                    Ctor = x.Item1,
                                    Parameters = x.Item2,
                                    Values = new object[x.Item2.Length]
                                } )
                                .FirstOrDefault();
            if( longestCtor == null )
            {
                if( throwOnError ) throw new Exception( $"Unable to find a public constructor for '{t.FullName}'." );
                return null;
            }
            for( int i = 0; i < longestCtor.Parameters.Length; ++i )
            {
                var p = longestCtor.Parameters[i];
                object resolved = Resolve( container, p.ParameterType, !p.HasDefaultValue );
                if( resolved == null ) resolved = p.DefaultValue;
                longestCtor.Values[i] = resolved;
            }
            return longestCtor.Ctor.Invoke( longestCtor.Values );
        }

        public static ITestHelperResolver Default { get; } = new TestHelperResolver( TestHelperConfiguration.Default );
    }
}
