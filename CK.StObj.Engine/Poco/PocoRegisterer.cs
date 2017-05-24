using CK.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Registerer for <see cref="IPoco"/> interfaces.
    /// </summary>
    class PocoRegisterer
    {
        class PocoType
        {
            public readonly Type Type;
            public readonly PocoType Root;
            public readonly List<Type> RootCollector;

            public PocoType( Type type, PocoType root )
            {
                Type = type;
                if( root != null )
                {
                    Root = root;
                    root.RootCollector.Add( type );
                }
                else
                {
                    Root = this;
                    RootCollector = new List<Type>();
                    RootCollector.Add( type );
                }
            }
        }
        readonly Dictionary<Type, PocoType> _all;
        readonly List<List<Type>> _result;
        readonly string _namespace;
        int _uniqueNumber;

        /// <summary>
        /// Initializes a new <see cref="PocoRegisterer"/>.
        /// </summary>
        /// <param name="namespace">Namespace into which dynamic types will be cerated.</param>
        public PocoRegisterer( string @namespace = "CK._g.poco" )
        {
            _namespace = @namespace ?? "CK._g.poco";
            _all = new Dictionary<Type, PocoType>();
            _result = new List<List<Type>>();
        }

        /// <summary>
        /// Registers a <see cref="IPoco"/> interface.
        /// </summary>
        /// <param name="monitor">Monitor that will be used to signal errors.</param>
        /// <param name="t">Poco type to register (must extend IPoco interface).</param>
        /// <returns>True on success, false on error.</returns>
        public bool Register( IActivityMonitor monitor, Type t ) => DoRegister( monitor, t ) != null;

        PocoType DoRegister( IActivityMonitor monitor, Type t )
        {
            Debug.Assert( typeof( IPoco ).IsAssignableFrom( t ) );
            PocoType p;
            if( !_all.TryGetValue( t, out p ) )
            {
                PocoType theOnlyRoot = null;
                foreach( Type b in t.GetInterfaces() )
                {
                    if( b != typeof(IPoco) )
                    {
                        // Base interface must be a IPoco.
                        if( !typeof( IPoco ).IsAssignableFrom( b ) )
                        {
                            monitor.Fatal().Send( $"Poco interface '{t.AssemblyQualifiedName}' extends '{b.Name}'. '{b.Name}' must be marked with CK.Core.IPoco interface." );
                            return null;
                        }
                        // Attempts to register the base.
                        var baseType = DoRegister( monitor, b );
                        if( baseType == null ) return null;
                        // Detect multiple root Poco.
                        if( theOnlyRoot != null )
                        {
                            if( theOnlyRoot != baseType.Root )
                            {
                                monitor.Fatal().Send( $"Poco interface '{t.AssemblyQualifiedName}' extends both '{theOnlyRoot.Type.Name}' and '{baseType.Root.Type.Name}' (via '{baseType.Type.Name}')." );
                                return null;
                            }
                        }
                        else theOnlyRoot = baseType.Root;
                    }
                }
                p = new PocoType( t, theOnlyRoot );
                _all.Add( t, p );
                if( theOnlyRoot == null ) _result.Add( p.RootCollector );
            }
            return p;
        }

        class Result : IPocoSupportResult
        {
            public readonly List<ClassInfo> Roots;
            public readonly Dictionary<Type, InterfaceInfo> Interfaces;
            public Type FinalFactory { get; internal set; }
            IReadOnlyCollection<InterfaceInfo> _exportedInterfaces;

            public Result()
            {
                Roots = new List<ClassInfo>();
                Interfaces = new Dictionary<Type, InterfaceInfo>();
                _exportedInterfaces = new CKReadOnlyCollectionOnICollection<InterfaceInfo>( Interfaces.Values );
            }

            IReadOnlyList<IPocoRootInfo> IPocoSupportResult.Roots => Roots;

            IPocoInterfaceInfo IPocoSupportResult.Find( Type pocoInterface )
            {
                return Interfaces.GetValueWithDefault( pocoInterface, null );
            }

            IReadOnlyCollection<IPocoInterfaceInfo> IPocoSupportResult.AllInterfaces => _exportedInterfaces;

        }

        class ClassInfo : IPocoRootInfo
        {
            public Type PocoClass { get; }
            public readonly MethodBuilder StaticMethod;
            public readonly List<InterfaceInfo> Interfaces;
            IReadOnlyList<IPocoInterfaceInfo> IPocoRootInfo.Interfaces => Interfaces;

            public ClassInfo( Type pocoClass, MethodBuilder method )
            {
                PocoClass = pocoClass;
                StaticMethod = method;
                Interfaces = new List<InterfaceInfo>();
            }
        }

        class InterfaceInfo : IPocoInterfaceInfo
        {
            public readonly ClassInfo Root;
            public Type PocoInterface { get; }
            public Type PocoFactoryInterface { get; }

            IPocoRootInfo IPocoInterfaceInfo.Root => Root;

            public InterfaceInfo( ClassInfo root, Type pocoInterface, Type pocoFactoryInterface )
            {
                Root = root;
                PocoInterface = pocoInterface;
                PocoFactoryInterface = pocoFactoryInterface;
            }
        }

        public IPocoSupportResult Finalize( ModuleBuilder moduleB, IActivityMonitor monitor )
        {
            _uniqueNumber = 0;
            var tB = moduleB.DefineType( _namespace + ".Factory" );
            Result r = CreateResult( moduleB, monitor, tB );
            if( r == null ) return null;
            ImplementFactories( monitor, tB, r );
            r.FinalFactory = tB.CreateTypeInfo().AsType();
            return r;
        }

        void ImplementFactories( IActivityMonitor monitor, TypeBuilder tB, Result r )
        {
            foreach( var cInfo in r.Roots )
            {
                var g = cInfo.StaticMethod.GetILGenerator();
                g.Emit( OpCodes.Newobj, cInfo.PocoClass.GetConstructor( Type.EmptyTypes ) );
                g.Emit( OpCodes.Ret );
            }
        }

        Result CreateResult( ModuleBuilder moduleB, IActivityMonitor monitor, TypeBuilder tB )
        {
            MethodInfo typeFromToken = typeof( Type ).GetMethod( nameof( Type.GetTypeFromHandle ), BindingFlags.Static | BindingFlags.Public );

            Result r = new Result();
            int idMethod = 0;
            foreach( var signature in _result )
            {
                Type tPoco = CreatePocoType( moduleB, monitor, signature );
                if( tPoco == null ) return null;
                MethodBuilder realMB = tB.DefineMethod( "DoC" + r.Roots.Count.ToString(), MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, tPoco, Type.EmptyTypes );
                var cInfo = new ClassInfo( tPoco, realMB );
                r.Roots.Add( cInfo );
                foreach( var i in signature )
                {
                    Type iCreate = typeof( IPocoFactory<> ).MakeGenericType( i );
                    tB.AddInterfaceImplementation( iCreate );
                    {
                        MethodBuilder mB = tB.DefineMethod( "C" + (idMethod++).ToString(), MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.Final, i, Type.EmptyTypes );
                        ILGenerator g = mB.GetILGenerator();
                        g.Emit( OpCodes.Call, realMB );
                        g.Emit( OpCodes.Ret );
                        tB.DefineMethodOverride( mB, iCreate.GetMethod( nameof( IPocoFactory<IPoco>.Create ) ) );
                    }
                    {
                        MethodBuilder mB = tB.DefineMethod( "get_T" + (idMethod++).ToString(), MethodAttributes.Virtual | MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Final, typeof(Type), Type.EmptyTypes );
                        ILGenerator g = mB.GetILGenerator();
                        g.Emit( OpCodes.Ldtoken, tPoco );
                        g.Emit( OpCodes.Call, typeFromToken );
                        g.Emit( OpCodes.Ret );
                        tB.DefineMethodOverride( mB, iCreate.GetProperty( nameof( IPocoFactory<IPoco>.PocoClassType ) ).GetGetMethod() );
                    }
                    var iInfo = new InterfaceInfo( cInfo, i, iCreate );
                    cInfo.Interfaces.Add( iInfo );
                    r.Interfaces.Add( i, iInfo );
                }
            }
            return r;
        }

        Type CreatePocoType( ModuleBuilder moduleB, IActivityMonitor monitor, IReadOnlyList<Type> interfaces )
        {
            var tB = moduleB.DefineType( $"{_namespace}.Poco{_uniqueNumber++}" );
            Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();
            foreach( var i in interfaces )
            {
                tB.AddInterfaceImplementation( i );
                foreach( var p in i.GetProperties() )
                {
                    PropertyInfo implP;
                    if( properties.TryGetValue( p.Name, out implP ) )
                    {
                        if( implP.PropertyType != p.PropertyType )
                        {
                            monitor.Error().Send( $"Interface '{i.FullName}' and '{implP.DeclaringType.FullName}' both declare property '{p.Name}' but their type differ ({p.PropertyType.Name} vs. {implP.PropertyType.Name})." );
                            return null;
                        }
                    }
                    else
                    {
                        EmitHelper.ImplementStubProperty( tB, p, false, true );
                        properties.Add( p.Name, p );
                    }
                }
            }
            return tB.CreateTypeInfo().AsType();
        }
    }
}
