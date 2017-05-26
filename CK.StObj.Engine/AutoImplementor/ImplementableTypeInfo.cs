#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\AutoImplementor\ImplementableTypeInfo.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using CK.Reflection;
using CK.CodeGen;

namespace CK.Core
{
    public class ImplementableTypeInfo
    {
        public class NoImplementationMarker : IAutoImplementorMethod, IAutoImplementorProperty
        {
            public bool Implement( IActivityMonitor monitor, MethodInfo m, IDynamicAssembly dynamicAssembly, System.Reflection.Emit.TypeBuilder b, bool isVirtual )
            {
                throw new NotSupportedException();
            }

            public bool Implement( IActivityMonitor monitor, PropertyInfo p, IDynamicAssembly dynamicAssembly, System.Reflection.Emit.TypeBuilder b, bool isVirtual )
            {
                throw new NotSupportedException();
            }

            public bool Implement( IActivityMonitor monitor, MethodInfo m, IDynamicAssembly dynamicAssembly, ClassBuilder b )
            {
                throw new NotSupportedException();
            }

            public bool Implement( IActivityMonitor monitor, PropertyInfo p, IDynamicAssembly dynamicAssembly, ClassBuilder b )
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Exposes <see cref="IAutoImplementorMethod"/> and <see cref="IAutoImplementorProperty"/> that implement
        /// empty behavior.
        /// </summary>
        public static readonly NoImplementationMarker UnimplementedMarker = new NoImplementationMarker();

        Type _stubType;

        /// <summary>
        /// Gets the starting type that must be automatically implemented.
        /// </summary>
        public readonly Type AbstractType;

        /// <summary>
        /// Gets the current property information for all abstract properties of the <see cref="AbstractType"/>.
        /// </summary>
        public readonly IReadOnlyList<ImplementableAbstractPropertyInfo> PropertiesToImplement;

        /// <summary>
        /// Gets the current method information for all abstract methods of the <see cref="AbstractType"/>.
        /// </summary>
        public readonly IReadOnlyList<ImplementableAbstractMethodInfo> MethodsToImplement;

        /// <summary>
        /// Gets the stub type. Null if <see cref="CreateStubType"/> has not been called yet.
        /// </summary>
        public Type StubType => _stubType;

        ImplementableTypeInfo( Type t, IReadOnlyList<ImplementableAbstractPropertyInfo> p, IReadOnlyList<ImplementableAbstractMethodInfo> m )
        {
            AbstractType = t;
            PropertiesToImplement = p;
            MethodsToImplement = m;
        }

        /// <summary>
        /// Attempts to create a new <see cref="ImplementableTypeInfo"/>. If the type is marked with <see cref="PreventAutoImplementationAttribute"/> or that one
        /// of its abstract methods (or properties) misses <see cref="IAttributeAutoImplemented"/> or <see cref="IAutoImplementorMethod"/> (<see cref="IAutoImplementorProperty"/>
        /// for properties), null is returned.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="abstractType">Abstract type to automatically implement if possible.</param>
        /// <param name="attributeProvider">Attributes provider that will be used.</param>
        /// <returns>An instance of <see cref="ImplementableTypeInfo"/> or null if the type is not automatically implementable.</returns>
        static public ImplementableTypeInfo CreateImplementableTypeInfo( IActivityMonitor monitor, Type abstractType, ICKCustomAttributeProvider attributeProvider )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            if( abstractType == null ) throw new ArgumentNullException( nameof( abstractType ) );
            if( !abstractType.GetTypeInfo().IsAbstract ) throw new ArgumentException( "Type must be abstract.", nameof( abstractType ) );
            if( attributeProvider == null ) throw new ArgumentNullException( nameof( attributeProvider ) );

            if( abstractType.GetTypeInfo().IsDefined( typeof( PreventAutoImplementationAttribute ), false ) ) return null;

            var candidates = abstractType.GetMethods( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public ).Where( m => !m.IsSpecialName && m.IsAbstract );
            int nbUncovered = 0;
            List<ImplementableAbstractMethodInfo> methods = new List<ImplementableAbstractMethodInfo>();
            foreach( var m in candidates )
            {
                ++nbUncovered;
                IAutoImplementorMethod impl = attributeProvider.GetCustomAttributes<IAutoImplementorMethod>( m ).SingleOrDefault();
                if( impl == null && attributeProvider.IsDefined( m, typeof( IAttributeAutoImplemented ) ) ) impl = UnimplementedMarker;
                if( impl != null )
                {
                    --nbUncovered;
                    methods.Add( new ImplementableAbstractMethodInfo( m, impl ) );
                }
            }
            List<ImplementableAbstractPropertyInfo> properties = new List<ImplementableAbstractPropertyInfo>();
            var pCandidates = abstractType.GetProperties( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
            foreach( var p in pCandidates )
            {
                MethodInfo mGet = p.GetGetMethod( true );
                MethodInfo mSet = p.GetSetMethod( true );
                bool isAbstract = (mGet != null && mGet.IsAbstract) || (mSet != null && mSet.IsAbstract);
                if( isAbstract )
                {
                    ++nbUncovered;
                    if( mGet == null || mSet == null || !mGet.IsAbstract || !mSet.IsAbstract )
                    {
                        monitor.Error().Send( $"Property {p.DeclaringType.FullName}.{p.Name} is not a valid abstract property (both getter and setter must exist and be abstract)." );
                    }
                    else
                    {
                        IAutoImplementorProperty impl = attributeProvider.GetCustomAttributes<IAutoImplementorProperty>( p ).SingleOrDefault();
                        if( impl == null && attributeProvider.IsDefined( p, typeof( IAttributeAutoImplemented ) ) ) impl = UnimplementedMarker;
                        if( impl != null )
                        {
                            --nbUncovered;
                            properties.Add( new ImplementableAbstractPropertyInfo( p, impl ) );
                        }
                    }
                }
            }
            if( nbUncovered > 0 ) return null;
            return new ImplementableTypeInfo( abstractType, properties, methods );
        }

        /// <summary>
        /// Implements the <see cref="StubType"/> in a dynamic assembly that 
        /// specializes <see cref="AbstractType"/> and returns it.
        /// </summary>
        /// <param name="monitor">Logger to use.</param>
        /// <param name="assembly">Dynamic assembly.</param>
        /// <returns>The newly created type in the dynamic assembly. Null if an error occurred.</returns>
        public Type CreateStubType( IActivityMonitor monitor, IDynamicAssembly assembly )
        {
            if( _stubType != null ) throw new InvalidOperationException( "Must be called only if StubType is null." );
            try
            {
                TypeAttributes tA = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;
                System.Reflection.Emit.TypeBuilder b = assembly.ModuleBuilder.DefineType( assembly.AutoNextTypeName( AbstractType.Name ), tA, AbstractType );
                // Relayed constructors replicates all their potential attributes (included attributes on parameters).
                // We do not replicate attributes on parameters here. 
                b.DefinePassThroughConstructors( c => c.Attributes | MethodAttributes.Public, null, ( parameter, CustomAttributeData ) => false );
                foreach( var am in MethodsToImplement )
                {
                    CK.Reflection.EmitHelper.ImplementEmptyStubMethod( b, am.Method, false );
                }
                foreach( var ap in PropertiesToImplement )
                {
                    CK.Reflection.EmitHelper.ImplementStubProperty( b, ap.Property, false );
                }
                return _stubType = b.CreateTypeInfo().AsType();
            }
            catch( Exception ex )
            {
                monitor.Fatal().Send( ex, $"While implementing Stub for '{AbstractType.FullName}'." );
                return null;
            }
        }

#if NET461
        /// <summary>
        /// Implements a final Type in a dynamic assembly that specializes <see cref="AbstractType"/> and returns it.
        /// All current or last <see cref="IAutoImplementorMethod"/> and <see cref="IAutoImplementorProperty"/> are used.
        /// Implemented method and properties are not virtual and the resulting type is sealed.
        /// </summary>
        /// <param name="monitor">Logger to use.</param>
        /// <param name="assembly">Dynamic assembly into which the type must be created.</param>
        /// <returns>The newly created type in the dynamic assembly. Null if an error occurred.</returns>
        public Type CreateFinalType( IActivityMonitor monitor, IDynamicAssembly assembly )
        {
            try
            {
                TypeAttributes tA = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;
                System.Reflection.Emit.TypeBuilder b = assembly.ModuleBuilder.DefineType(assembly.AutoNextTypeName(AbstractType.Name), tA, AbstractType);
                // Relayed constructors replicates all their potential attributes (included attributes on parameters).
                b.DefinePassThroughConstructors(c => c.Attributes | MethodAttributes.Public);
                bool hasFatal = false;
                foreach (var am in MethodsToImplement)
                {
                    IAutoImplementorMethod m = am.ImplementorToUse;
                    if (m == null || m == UnimplementedMarker)
                    {
                        monitor.Fatal().Send("Method '{0}.{1}' has no valid associated IAutoImplementorMethod.", AbstractType.FullName, am.Method.Name);
                        hasFatal = true;
                    }
                    else
                    {
                        if (!m.Implement(monitor, am.Method, assembly, b, false))
                        {
                            monitor.Fatal().Send($"Method '{AbstractType.FullName}.{am.Method.Name}' can not be implemented by its IAutoImplementorMethod.");
                            hasFatal = true;
                        }
                    }
                }
                foreach (var ap in PropertiesToImplement)
                {
                    IAutoImplementorProperty p = ap.ImplementorToUse;
                    if (p == null || p == UnimplementedMarker)
                    {
                        monitor.Fatal().Send($"Property '{AbstractType.FullName}.{ap.Property.Name}' has no valid associated IAutoImplementorProperty.");
                        hasFatal = true;
                    }
                    else
                    {
                        if (!p.Implement(monitor, ap.Property, assembly, b, false))
                        {
                            monitor.Fatal().Send($"Property '{AbstractType.FullName}.{ap.Property.Name}' can not be implemented by its IAutoImplementorProperty.");
                            hasFatal = true;
                        }
                    }
                }
                if (hasFatal) return null;
                return b.CreateType();
            }
            catch (Exception ex)
            {
                monitor.Fatal().Send(ex, $"While implementing Type '{AbstractType.FullName}'.");
                return null;
            }
        }
#endif
        public string GenerateType( IActivityMonitor monitor, IDynamicAssembly a )
        {
            var cB = a.SourceBuilder.DefineClassWithPublicPassThroughConstructors( "public", _stubType.Name, AbstractType );
            foreach( var am in MethodsToImplement )
            {
                IAutoImplementorMethod m = am.ImplementorToUse;
                if( m == null || m == UnimplementedMarker )
                {
                    monitor.Fatal().Send( $"Method '{AbstractType.FullName}.{am.Method.Name}' has no valid associated IAutoImplementorMethod." );
                }
                else
                {
                    if( !m.Implement( monitor, am.Method, a, cB ) )
                    {
                        monitor.Fatal().Send( $"Method '{AbstractType.FullName}.{am.Method.Name}' can not be implemented by its IAutoImplementorMethod." );
                    }
                }
            }
            foreach( var ap in PropertiesToImplement )
            {
                IAutoImplementorProperty p = ap.ImplementorToUse;
                if( p == null || p == UnimplementedMarker )
                {
                    monitor.Fatal().Send( $"Property '{AbstractType.FullName}.{ap.Property.Name}' has no valid associated IAutoImplementorProperty." );
                }
                else
                {
                    if( !p.Implement( monitor, ap.Property, a, cB ) )
                    {
                        monitor.Fatal().Send( $"Property '{AbstractType.FullName}.{ap.Property.Name}' can not be implemented by its IAutoImplementorProperty." );
                    }
                }
            }
            return cB.FullName;
        }
    }
}
