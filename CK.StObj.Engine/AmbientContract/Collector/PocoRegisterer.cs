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

        /// <summary>
        /// Initializes a new <see cref="PocoRegisterer"/>.
        /// </summary>
        public PocoRegisterer()
        {
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
            IReadOnlyList<IPocoRootInfo> IPocoSupportResult.Roots => Roots;
            public Type FinalFactory { get; internal set; }


            public Result()
            {
                Roots = new List<ClassInfo>();
                Interfaces = new Dictionary<Type, InterfaceInfo>();
            }

            public IPocoInterfaceInfo Find( Type pocoInterface )
            {
                return Interfaces.GetValueWithDefault( pocoInterface, null );
            }
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

        public IPocoSupportResult Finalize( DynamicAssembly a, IActivityMonitor monitor )
        {
            const string name = "CK.PocoFactory";
            IPocoSupportResult result = (IPocoSupportResult)a.Memory[name];
            if( result == null )
            {
                var tB = a.ModuleBuilder.DefineType( name );
                Result r = CreateResult( a, monitor, tB );
                if( r == null ) return null;
                ImplementFactories( a, monitor, tB, r );
                r.FinalFactory = tB.CreateType();
                a.Memory.Add( name, result = r );
            }
            return result;
        }

        void ImplementFactories( DynamicAssembly a, IActivityMonitor monitor, TypeBuilder tB, Result r )
        {
            foreach( var cInfo in r.Roots )
            {
                var g = cInfo.StaticMethod.GetILGenerator();
                g.Emit( OpCodes.Newobj, cInfo.PocoClass.GetConstructor( Type.EmptyTypes ) );
                g.Emit( OpCodes.Ret );
            }
        }

        Result CreateResult( DynamicAssembly a, IActivityMonitor monitor, TypeBuilder tB )
        {
            Result r = new Result();
            int idMethod = 0;
            foreach( var signature in _result )
            {
                Type tPoco = CreatePocoType( a, monitor, signature );
                if( tPoco == null ) return null;
                MethodBuilder realMB = tB.DefineMethod( "DoC" + r.Roots.Count.ToString(), MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.Final, tPoco, Type.EmptyTypes );
                var cInfo = new ClassInfo( tPoco, realMB );
                r.Roots.Add( cInfo );
                foreach( var i in signature )
                {
                    Type iCreate = typeof( IPocoFactory<> ).MakeGenericType( i );
                    tB.AddInterfaceImplementation( iCreate );
                    MethodBuilder mB = tB.DefineMethod( "C" + (idMethod++).ToString(), MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final, i, Type.EmptyTypes );
                    ILGenerator g = mB.GetILGenerator();
                    g.Emit( OpCodes.Call, realMB );
                    g.Emit( OpCodes.Ret );
                    cInfo.Interfaces.Add( new InterfaceInfo( cInfo, i, iCreate ) );
                    tB.DefineMethodOverride( mB, iCreate.GetMethod( "Create" ) );
                }
            }
            return r;
        }

        static Type CreatePocoType( DynamicAssembly a, IActivityMonitor monitor, IReadOnlyList<Type> interfaces )
        {
            var tB = a.ModuleBuilder.DefineType( $"Poco<{a.NextUniqueNumber()}>" );
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
                        EmitHelper.ImplementStubProperty( tB, p );
                        properties.Add( p.Name, p );
                    }
                }
            }
            return tB.CreateType();
        }

    }
}
