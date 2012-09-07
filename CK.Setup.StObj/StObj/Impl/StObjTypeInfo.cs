using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection;
using System.Diagnostics;

namespace CK.Setup
{
    internal class StObjTypeInfo : AmbiantTypeInfo
    {
        internal StObjTypeInfo( IActivityLogger logger, AmbiantTypeInfo parent, Type t )
            : base( parent, t )
        {
            #region Ambiant properties.
            {
                // For type that have no Generalization: we must handle [AmbiantProperty] on base classes (no AmbiantTypeInfo since they are not Ambiant contract).
                // Currently, the ambiant properties information is not cached and rebuilt each time.
                // May be once, a cache will be here, but for the moment, I consider it useless.
                IEnumerable<AmbiantPropertyInfo> fromParent = DirectGeneralization != null ? DirectGeneralization.AmbiantProperties : CreateAllAmbiantPropertyList( Type.BaseType, logger );
                // Ambiant properties for the exact Type (can be null).
                IList<AmbiantPropertyInfo> collector = CreateAmbiantPropertyList( Type, logger );
                // Both fromParent and collector can be null: MergeAboveAmbiantProperties handles it.
                AmbiantProperties = MergeAboveAmbiantProperties( fromParent, collector, logger ).ToReadOnlyCollection();
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

        public readonly IReadOnlyCollection<AmbiantPropertyInfo> AmbiantProperties;

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
        /// An ambiant property must be public or protected in order to be "specialized" either by overriding (for virtual ones)
        /// or by masking ('new' keyword in C#), typically to support covariance return type.
        /// The "Property Covariance" trick can be supported here because ambiant properties are conceptually "read only" properties:
        /// they must be settable only to enable the framework (and no one else) to actually set their values.
        /// </summary>
        List<AmbiantPropertyInfo> CreateAmbiantPropertyList( Type t, IActivityLogger logger )
        {
            var properties = t.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly ).Where( p => !p.Name.Contains( '.' ) );
            List<AmbiantPropertyInfo> result = null;
            foreach( var p in properties )
            {
                AmbiantPropertyAttribute attr = (AmbiantPropertyAttribute)Attribute.GetCustomAttribute( p, typeof( AmbiantPropertyAttribute ), false );
                if( attr == null ) continue;

                var mSet = p.GetSetMethod( true );
                var mGet = p.GetGetMethod( true );
                if( (mSet == null || mSet.IsPrivate) || (mGet == null || mGet.IsPrivate) )
                {
                    // Warning: not a "Property Covariance" compliant property since
                    // specialized classes will not be able to "override" its signature.
                    // Even if it is not the "Property Covariance" that is targeted, a private get or set
                    // implies a "slicing" of the (base) type that defeats the specialization paradigm (with Covariance).
                    logger.Error( "Property '{0}' of '{1}' can not be considered as an Ambiant property. An Ambiant property must be readable and writeable (no private setter or getter).", p.Name, p.DeclaringType.FullName );
                }
                else
                {
                    if( result == null ) result = new List<AmbiantPropertyInfo>();
                    Debug.Assert( result.Find( a => a.Name == p.Name ) == null, "No homonym properties in .Net framework." );
                    result.Add( new AmbiantPropertyInfo( this, p, attr ) );
                }
            }
            return result;
        }

        private IEnumerable<AmbiantPropertyInfo> MergeAboveAmbiantProperties( IEnumerable<AmbiantPropertyInfo> above, IList<AmbiantPropertyInfo> collector, IActivityLogger logger )
        {
            if( collector == null || collector.Count == 0 ) return above ?? ReadOnlyListEmpty<AmbiantPropertyInfo>.Empty;
            if( above != null )
            {
                // Add 'above' into 'collector' before returning it.
                List<AmbiantPropertyInfo> fromAbove = null;
                foreach( AmbiantPropertyInfo a in above )
                {
                    AmbiantPropertyInfo exists = collector.FirstOrDefault( p => p.Name == a.Name );
                    if( exists != null )
                    {
                        // Covariance ?
                        if( exists.PropertyType != a.PropertyType && !a.PropertyType.IsAssignableFrom( exists.PropertyType ) )
                        {
                            logger.Error( "Ambiant property '{0}.{1}' type is not compatible with base property '{2}.{1}'.", exists.DeclaringType.FullName, exists.Name, a.DeclaringType.FullName );
                        }
                        // A required property can not become optional.
                        if( exists.IsOptional && !a.IsOptional )
                        {
                            logger.Error( "Ambiant property '{0}.{1}' states that it is optional but base property '{2}.{1}' is required.", exists.DeclaringType.FullName, exists.Name, a.DeclaringType.FullName );
                        }
                        // Context inheritance (if not defined).
                        if( exists.Context == null )
                        {
                            exists.Context = a.Context;
                        }
                        // We do not need to keep the fact that this property overrides one above
                        // as long as we have checked that no conflict/incoherency occur.
                        // We may keep the Generalization (ie. setting exists._directGeneralization = a) but not a
                        // reference to the specialization since we are not Contextualized here, but only on
                        // a pure Type level.
                    }
                    else
                    {
                        if( fromAbove == null ) fromAbove = new List<AmbiantPropertyInfo>();
                        fromAbove.Add( a );
                    }
                }
                if( fromAbove != null ) collector.AddRange( fromAbove );
            }
            return collector;
        }

        private IEnumerable<AmbiantPropertyInfo> CreateAllAmbiantPropertyList( Type type, IActivityLogger logger )
        {
            if( type == typeof( object ) ) return null;
            return MergeAboveAmbiantProperties( CreateAllAmbiantPropertyList( type.BaseType, logger ), CreateAmbiantPropertyList( type, logger ), logger );
        }



    }
}
