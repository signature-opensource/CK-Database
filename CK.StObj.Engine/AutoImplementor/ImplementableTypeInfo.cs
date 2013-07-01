using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

namespace CK.Core
{
    public class ImplementableTypeInfo
    {
        public class StubImplementor : IAutoImplementorMethod, IAutoImplementorProperty
        {
            public bool Implement( IActivityLogger logger, MethodInfo m, IDynamicAssembly dynamicAssembly, TypeBuilder b, bool isVirtual )
            {
                CK.Reflection.EmitHelper.ImplementEmptyStubMethod( b, m, isVirtual );
                return true;
            }

            public bool Implement( IActivityLogger logger, PropertyInfo p, IDynamicAssembly dynamicAssembly, TypeBuilder b, bool isVirtual )
            {
                CK.Reflection.EmitHelper.ImplementStubProperty( b, p, isVirtual );
                return true;
            }
        }

        /// <summary>
        /// Exposes <see cref="IAutoImplementorMethod"/> and <see cref="IAutoImplementorProperty"/> that implement
        /// empty behavior.
        /// </summary>
        public static readonly StubImplementor EmptyImplementor = new StubImplementor();

        Type _lastGeneratedType;

        /// <summary>
        /// Gets or sets a simple <see cref="IListener"/> to be aware of any implementor changes.
        /// </summary>
        public IListener Listener { get; set; }

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
        /// Gets whether at least a property or a method is associated to an implementor that has not been used.
        /// </summary>
        public bool ExpectImplementation
        {
            get { return PropertiesToImplement.Any( info => info.ExpectImplementation ) || MethodsToImplement.Any( info => info.ExpectImplementation ); }
        }

        /// <summary>
        /// Gets the last generated type. Null if no type has been generated yet.
        /// </summary>
        public Type LastGeneratedType
        {
            get { return _lastGeneratedType; }
        }

        /// <summary>
        /// Gets the current base type. 
        /// This is the type that will be used as the base class of the new type created by <see cref="CreateTypeFromCurrent"/>.
        /// This is either the <see cref="LastGeneratedType"/> or <see cref="AbstractType"/> if none has been generated yet.
        /// </summary>
        public Type CurrentBaseType
        {
            get { return _lastGeneratedType ?? AbstractType; }
        }

        /// <summary>
        /// Simple relaying to mono listener.
        /// </summary>
        public interface IListener
        {
            /// <summary>
            /// Called wen an implementor changed.
            /// </summary>
            /// <param name="m">Abstract method that should be regenerated.</param>
            void ImplementorChanged( ImplementableAbstractMethodInfo m );

            /// <summary>
            /// Called wen an implementor changed.
            /// </summary>
            /// <param name="p">Abstract property that should be regenerated.</param>
            void ImplementorChanged( ImplementableAbstractPropertyInfo p );
        }

        ImplementableTypeInfo( Type t, IReadOnlyList<ImplementableAbstractPropertyInfo> p, IReadOnlyList<ImplementableAbstractMethodInfo> m )
        {
            AbstractType = t;
            PropertiesToImplement = p;
            MethodsToImplement = m;
            foreach( var ap in p ) ap._type = this;
            foreach( var am in m ) am._type = this;
        }

        /// <summary>
        /// Attempts to create a new <see cref="ImplementableTypeInfo"/>. If the type is marked with <see cref="PreventAutoImplementationAttribute"/> or that one
        /// of its abstract methods (or properties) misses <see cref="IAttributeAutoImplemented"/> or <see cref="IAutoImplementorMethod"/> (<see cref="IAutoImplementorProperty"/>
        /// for properties), null is returned.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="abstractType">Abstract type to automatically implement if possible.</param>
        /// <param name="attributeProvider">Attributes provider that will be used.</param>
        /// <returns>An instance of <see cref="ImplementableTypeInfo"/> or null if the type is not automatically implementable.</returns>
        static public ImplementableTypeInfo CreateImplementableTypeInfo( IActivityLogger logger, Type abstractType, ICustomAttributeProvider attributeProvider )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            if( abstractType == null ) throw new ArgumentNullException( "abstractType" );
            if( !abstractType.IsAbstract ) throw new ArgumentException( "Type must be abstract.", "abstractType" );
            if( attributeProvider == null ) throw new ArgumentNullException( "attributeProvider" );

            if( abstractType.IsDefined( typeof( PreventAutoImplementationAttribute ), false ) ) return null;

            var candidates = abstractType.GetMethods( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public ).Where( m => !m.IsSpecialName && m.IsAbstract );
            int nbUncovered = 0;
            List<ImplementableAbstractMethodInfo> methods = new List<ImplementableAbstractMethodInfo>();
            foreach( var m in candidates )
            {
                ++nbUncovered;
                IAutoImplementorMethod impl = attributeProvider.GetCustomAttributes<IAutoImplementorMethod>( m ).SingleOrDefault();
                if( impl == null && attributeProvider.IsDefined( m, typeof( IAttributeAutoImplemented ) ) ) impl = EmptyImplementor;
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
                        logger.Error( "Property {0}.{1} is not a valid abstract property (both getter and setter must exist and be abstract).", p.DeclaringType.FullName, p.Name );
                    }
                    else
                    {
                        IAutoImplementorProperty impl = attributeProvider.GetCustomAttributes<IAutoImplementorProperty>( p ).SingleOrDefault();
                        if( impl == null && attributeProvider.IsDefined( p, typeof( IAttributeAutoImplemented ) ) ) impl = EmptyImplementor;
                        if( impl != null )
                        {
                            --nbUncovered;
                            properties.Add( new ImplementableAbstractPropertyInfo( p, impl ) );
                        }
                    }
                }
            }
            if( nbUncovered > 0 ) return null;
            return new ImplementableTypeInfo( abstractType, properties.ToReadOnlyList(), methods.ToReadOnlyList() );
        }

        /// <summary>
        /// Implements a new Type in a dynamic assembly that specializes <see cref="CurrentBaseType"/> and returns it.
        /// On success, resulting type becomes the <see cref="LastGeneratedType"/>. Of course, implemented methods and properties are let virtual.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <returns>The newly created type in the dynamic assembly. Null if an error occured.</returns>
        public Type CreateTypeFromCurrent( IActivityLogger logger, IDynamicAssembly assembly )
        {
            Type t = DoCreateType( logger, assembly, CurrentBaseType, false );
            if( t != null )
            {
                _lastGeneratedType = t;
                // Transfers ImplementorToUse to LastImplementor.
                foreach( var m in MethodsToImplement )
                {
                    if( m.ImplementorToUse != null )
                    {
                        m._last = m.ImplementorToUse;
                        m.ImplementorToUse = null;
                    }
                }
                foreach( var p in PropertiesToImplement )
                {
                    if( p.ImplementorToUse != null )
                    {
                        p._last = p.ImplementorToUse;
                        p.ImplementorToUse = null;
                    }
                }
            }
            return t;
        }

        /// <summary>
        /// Implements a final Type in a dynamic assembly that specializes <see cref="AbstractType"/> and returns it.
        /// All current or last <see cref="IAutoImplementorMethod"/> and <see cref="IAutoImplementorProperty"/> are used.
        /// Implemented method and properties are not virtual and the resulting type is sealed.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="assembly">Dynamic assembly into which the type must be created.</param>
        /// <param name="storeAsLastGeneratedType">True to update <see cref="LastGeneratedType"/> with the created type.</param>
        /// <returns>The newly created type in the dynamic assembly. Null if an error occured.</returns>
        public Type CreateFinalType( IActivityLogger logger, IDynamicAssembly assembly, bool storeAsLastGeneratedType = false )
        {
            Type t = DoCreateType( logger, assembly, AbstractType, true );
            if( t != null && storeAsLastGeneratedType ) _lastGeneratedType = t;
            return t;
        }


        private Type DoCreateType( IActivityLogger logger, IDynamicAssembly assembly, Type current, bool finalImplementation )
        {
            TypeAttributes tA = TypeAttributes.Class | TypeAttributes.Public;
            if( finalImplementation ) tA |= TypeAttributes.Sealed;
            TypeBuilder b = assembly.ModuleBuilder.DefineType( current.Name + assembly.NextUniqueNumber(), tA, current );
            foreach( var am in MethodsToImplement )
            {
                if( finalImplementation || am.ExpectImplementation )
                {
                    IAutoImplementorMethod m = am.ImplementorToUse;
                    if( m == null && finalImplementation ) m = am.LastImplementor;
                    if( m == null || (m == EmptyImplementor && finalImplementation) )
                    {
                        logger.Fatal( "Method '{0}.{1}' has no valid associated IAutoImplementorMethod.", AbstractType.FullName, am.Method.Name );
                    }
                    else
                    {
                        try
                        {
                            if( !m.Implement( logger, am.Method, assembly, b, !finalImplementation ) )
                            {
                                if( finalImplementation )
                                {
                                    logger.Fatal( "Method '{0}.{1}' can not be implemented by its IAutoImplementorMethod.", AbstractType.FullName, am.Method.Name );
                                }
                                else EmptyImplementor.Implement( logger, am.Method, assembly, b, true );
                            }
                        }
                        catch( Exception ex )
                        {
                            logger.Fatal( ex, "While implementing method '{0}.{1}'.", AbstractType.FullName, am.Method.Name );
                            return null;
                        }
                    }
                }
            }
            foreach( var ap in PropertiesToImplement )
            {
                if( finalImplementation || ap.ExpectImplementation )
                {
                    IAutoImplementorProperty p = ap.ImplementorToUse;
                    if( p == null && finalImplementation ) p = ap.LastImplementor;
                    if( p == null || (p == EmptyImplementor && finalImplementation) )
                    {
                        logger.Fatal( "Property '{0}.{1}' has no valid associated IAutoImplementorProperty.", AbstractType.FullName, ap.Property.Name );
                    }
                    else
                    {
                        try
                        {
                            if( !p.Implement( logger, ap.Property, assembly, b, !finalImplementation ) )
                            {
                                if( finalImplementation )
                                {
                                    logger.Fatal( "Property '{0}.{1}' can not be implemented by its IAutoImplementorProperty.", AbstractType.FullName, ap.Property.Name );
                                }
                                else EmptyImplementor.Implement( logger, ap.Property, assembly, b, true );
                            }
                        }
                        catch( Exception ex )
                        {
                            logger.Fatal( ex, "While implementing property '{0}.{1}'.", AbstractType.FullName, ap.Property.Name );
                            return null;
                        }
                    }
                }
            }
            try
            {
                return b.CreateType();
            }
            catch( Exception ex )
            {
                logger.Fatal( ex, "While implementing Type '{0}'.", AbstractType.FullName );
                return null;
            }
        }

        internal void ImplementorChanged( ImplementableAbstractMethodInfo m )
        {
            if( Listener != null ) Listener.ImplementorChanged( m );
        }

        internal void ImplementorChanged( ImplementableAbstractPropertyInfo p )
        {
            if( Listener != null ) Listener.ImplementorChanged( p );
        }
    }
}
