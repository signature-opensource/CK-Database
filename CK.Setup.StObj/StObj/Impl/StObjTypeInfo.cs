using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection;
using System.Diagnostics;

namespace CK.Setup
{
    internal class StObjTypeInfo : AmbientTypeInfo
    {
        internal StObjTypeInfo( IActivityLogger logger, AmbientTypeInfo parent, Type t )
            : base( parent, t )
        {
            #region Ambient properties.
            {
                // For type that have no Generalization: we must handle [AmbientProperty] on base classes (no AmbientTypeInfo since they are not Ambient contract).
                // Currently, the ambient properties information is not cached and rebuilt each time.
                // May be once, a cache will be here, but for the moment, I consider it useless.
                IEnumerable<AmbientPropertyInfo> fromParent = DirectGeneralization != null ? DirectGeneralization.AmbientProperties : CreateAllAmbientPropertyList( Type.BaseType, logger );
                // Ambient properties for the exact Type (can be null).
                IList<AmbientPropertyInfo> collector = CreateAmbientPropertyList( Type, logger );
                // Both fromParent and collector can be null: MergeAboveAmbientProperties handles it.
                AmbientProperties = MergeAboveAmbientProperties( fromParent, collector, logger ).ToReadOnlyCollection();
            }
            #endregion

            #region IStObjAttribute (Container & Type requirements).
            StObjAttribute = CK.Setup.StObjAttribute.GetStObjAttribute( t, logger );
            if( StObjAttribute != null )
            {
                Container = StObjAttribute.Container;
                if( Container != null ) ContainerContext = FindContextFromMapAttributes( Container );
            }
            #endregion

            #region Construct method & parameters
            Construct = t.GetMethod( "Construct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
            if( Construct != null )
            {
                if( Construct.IsVirtual )
                {
                    logger.Error( "Method '{0}.Construct' must NOT be virtual.", t.FullName );
                }
                else
                {
                    ConstructParameters = Construct.GetParameters();
                    ConstructParameterTypedContext = ConstructParameters.Length > 0 ? new Type[ConstructParameters.Length] : Type.EmptyTypes;
                    ContainerConstructParameterIndex = -1;
                    for( int i = 0; i < ConstructParameters.Length; ++i )
                    {
                        var p = ConstructParameters[i];

                        // Finds the Context.
                        Type parameterContext;
                        ContextAttribute ctx = (ContextAttribute)Attribute.GetCustomAttribute( p, typeof( ContextAttribute ) );
                        if( ctx != null ) parameterContext = ctx.Context;
                        else parameterContext = FindContextFromMapAttributes( p.ParameterType );
                        ConstructParameterTypedContext[i] = parameterContext;

                        // Is it marked with ContainerAttribute?
                        bool isContainerParameter = Attribute.GetCustomAttribute( p, typeof( ContainerAttribute ) ) != null;
                        if( Attribute.GetCustomAttribute( p, typeof( ContainerAttribute ) ) != null )
                        {
                            if( ContainerConstructParameterIndex >= 0 )
                            {
                                logger.Error( "Construct method of class '{0}' has more than one parameter marked with [Container] attribute.", t.FullName );
                            }
                            else
                            {
                                // The Parameter is the Container.
                                if( Container != null && Container != p.ParameterType )
                                {
                                    logger.Error( "Construct parameter '{0}' for class '{1}' defines the Container as '{2}' but an attribute on the class declares the Container as '{3}'.",
                                                                    p.Name, t.FullName, p.ParameterType.FullName, Container.FullName );
                                }
                                else if( ContainerContext != null && ContainerContext != parameterContext )
                                {
                                    logger.Error( "Construct parameter '{0}' for class '{1}' targets the Container in '{2}' but an attribute on the class declares the Container context as '{3}'.",
                                                                    p.Name, t.FullName, parameterContext.Name, ContainerContext.Name );
                                }
                                ContainerConstructParameterIndex = i;
                                Container = p.ParameterType;
                                ContainerContext = parameterContext;
                            }
                        }
                    }
                }
            }
            #endregion

            ConfiguratorAttributes = (IStObjStructuralConfigurator[])Type.GetCustomAttributes( typeof( IStObjStructuralConfigurator ), false );
        }

        public new StObjTypeInfo DirectGeneralization { get { return (StObjTypeInfo)base.DirectGeneralization; } }

        public readonly IReadOnlyCollection<AmbientPropertyInfo> AmbientProperties;

        public readonly IStObjAttribute StObjAttribute;

        public readonly Type Container;

        public readonly Type ContainerContext;

        public readonly MethodInfo Construct;

        public readonly ParameterInfo[] ConstructParameters;

        public readonly int ContainerConstructParameterIndex;

        public readonly Type[] ConstructParameterTypedContext;

        public readonly IStObjStructuralConfigurator[] ConfiguratorAttributes;

        public Type FindContextFromMapAttributes( Type t )
        {
            return null;
        }

        /// <summary>
        /// An ambient property must be public or protected in order to be "specialized" either by overriding (for virtual ones)
        /// or by masking ('new' keyword in C#), typically to support covariance return type.
        /// The "Property Covariance" trick can be supported here because ambient properties are conceptually "read only" properties:
        /// they must be settable only to enable the framework (and no one else) to actually set their values.
        /// </summary>
        List<AmbientPropertyInfo> CreateAmbientPropertyList( Type t, IActivityLogger logger )
        {
            var properties = t.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly ).Where( p => !p.Name.Contains( '.' ) );
            List<AmbientPropertyInfo> result = null;
            foreach( var p in properties )
            {
                AmbientPropertyAttribute attr = (AmbientPropertyAttribute)Attribute.GetCustomAttribute( p, typeof( AmbientPropertyAttribute ), false );
                if( attr == null ) continue;

                var mSet = p.GetSetMethod( true );
                var mGet = p.GetGetMethod( true );
                bool isMergeable = typeof(IMergeable).IsAssignableFrom( p.PropertyType );
                bool isWriteable = mSet != null && !mSet.IsPrivate;
                if( (!isWriteable && !isMergeable ) || (mGet == null || mGet.IsPrivate) )
                {
                    // Warning: not a "Property Covariance" compliant property since
                    // specialized classes will not be able to "override" its signature.
                    // Even if it is not the "Property Covariance" that is targeted, a private get or set
                    // implies a "slicing" of the (base) type that defeats the specialization paradigm (with Covariance).
                    logger.Error( "Property '{0}' of '{1}' can not be considered as an Ambient property. An Ambient property must be readable and writeable (no private setter or getter), or implement CK.Core.IMergeable interface.", p.Name, p.DeclaringType.FullName );
                }
                else
                {
                    if( !isWriteable && isMergeable )
                    {
                        throw new NotImplementedException( "Not writeable mergeable properties are not yet supported (need a IStObjMutableItem.SetPropertyStructuralSetter to initialize it)." );
                    }
                    if( result == null ) result = new List<AmbientPropertyInfo>();
                    Debug.Assert( result.Find( a => a.Name == p.Name ) == null, "No homonym properties in .Net framework." );
                    result.Add( new AmbientPropertyInfo( this, p, attr, isWriteable, isMergeable ) );
                }
            }
            return result;
        }

        private IEnumerable<AmbientPropertyInfo> MergeAboveAmbientProperties( IEnumerable<AmbientPropertyInfo> above, IList<AmbientPropertyInfo> collector, IActivityLogger logger )
        {
            if( collector == null || collector.Count == 0 ) return above ?? ReadOnlyListEmpty<AmbientPropertyInfo>.Empty;
            if( above != null )
            {
                // Add 'above' into 'collector' before returning it.
                List<AmbientPropertyInfo> fromAbove = null;
                foreach( AmbientPropertyInfo a in above )
                {
                    AmbientPropertyInfo exists = collector.FirstOrDefault( p => p.Name == a.Name );
                    if( exists != null )
                    {
                        // Covariance ?
                        if( exists.PropertyType != a.PropertyType && !a.PropertyType.IsAssignableFrom( exists.PropertyType ) )
                        {
                            logger.Error( "Ambient property '{0}.{1}' type is not compatible with base property '{2}.{1}'.", exists.DeclaringType.FullName, exists.Name, a.DeclaringType.FullName );
                        }
                        // A required property can not become optional.
                        if( exists.IsOptional && !a.IsOptional )
                        {
                            logger.Error( "Ambient property '{0}.{1}' states that it is optional but base property '{2}.{1}' is required.", exists.DeclaringType.FullName, exists.Name, a.DeclaringType.FullName );
                        }
                        // Context inheritance (if not defined).
                        if( exists.Context == null )
                        {
                            exists.Context = a.Context;
                        }
                        // We do not need to keep the fact that this property overrides one above
                        // as long as we have checked that no conflict/incoherency occur.
                        // We may keep the Generalization (ie. setting exists._generalization = a) but not a
                        // reference to the specialization since we are not Contextualized here, but only on
                        // a pure Type level.
                    }
                    else
                    {
                        if( fromAbove == null ) fromAbove = new List<AmbientPropertyInfo>();
                        fromAbove.Add( a );
                    }
                }
                if( fromAbove != null ) collector.AddRange( fromAbove );
            }
            return collector;
        }

        private IEnumerable<AmbientPropertyInfo> CreateAllAmbientPropertyList( Type type, IActivityLogger logger )
        {
            if( type == typeof( object ) ) return null;
            return MergeAboveAmbientProperties( CreateAllAmbientPropertyList( type.BaseType, logger ), CreateAmbientPropertyList( type, logger ), logger );
        }



    }
}
