using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection;
using System.Diagnostics;

namespace CK.Setup
{
    interface ITypeInfoFromParent
    {
        Type Container { get; }
        IReadOnlyCollection<AmbientPropertyInfo> AmbientProperties { get; }
        DependentItemType ItemKind { get; }
        TrackAmbientPropertiesMode TrackAmbientProperties { get; }
    }

    internal class StObjTypeInfo : AmbientTypeInfo, ITypeInfoFromParent
    {
        class TypeInfoForBaseClasses : ITypeInfoFromParent
        {
            public IReadOnlyCollection<AmbientPropertyInfo> AmbientProperties { get; private set; }
            public Type Container { get; private set; }
            public DependentItemType ItemKind { get; private set; }
            public TrackAmbientPropertiesMode TrackAmbientProperties { get; private set; }

            bool IsFullyDefined
            {
                get { return Container != null && ItemKind != DependentItemType.Unknown && TrackAmbientProperties != TrackAmbientPropertiesMode.Unknown; }
            }

            static object _lock = new object();
            static Dictionary<Type,TypeInfoForBaseClasses> _cache;

            static public ITypeInfoFromParent GetFor( IActivityLogger logger, Type t )
            {
                TypeInfoForBaseClasses result = null;
                // Poor lock: we don't care here. Really.
                lock( _lock )
                {
                    if( _cache == null ) _cache = new Dictionary<Type, TypeInfoForBaseClasses>();
                    else _cache.TryGetValue( t, out result );
                    if( result == null )
                    {
                        result = new TypeInfoForBaseClasses();
                        // Ambient Properties (uses a recursive function).
                        var all = CreateAllAmbientPropertyList( t, logger );
                        result.AmbientProperties = all != null ? all.ToReadOnlyCollection() : ReadOnlyListEmpty<AmbientPropertyInfo>.Empty;
                        // For ItemKind & TrackAmbientProperties, walks up the inheritance chain and combines the StObjAttribute.
                        var a = CK.Setup.StObjAttribute.GetStObjAttributeForExactType( t, logger );
                        if( a != null )
                        {
                            result.Container = a.Container;
                            result.ItemKind = a.ItemKind;
                            result.TrackAmbientProperties = a.TrackAmbientProperties;
                        }
                        Type tAbove = t.BaseType;
                        while( !result.IsFullyDefined && tAbove != null && tAbove != typeof( object ) )
                        {
                            var aAbove = CK.Setup.StObjAttribute.GetStObjAttributeForExactType( tAbove, logger );
                            if( aAbove != null )
                            {
                                if( result.Container == null ) result.Container = aAbove.Container;
                                if( result.ItemKind == DependentItemType.Unknown ) result.ItemKind = aAbove.ItemKind;
                                if( result.TrackAmbientProperties == TrackAmbientPropertiesMode.Unknown ) result.TrackAmbientProperties = aAbove.TrackAmbientProperties;
                            }
                            tAbove = tAbove.BaseType;
                        }
                        _cache.Add( t, result );
                    }
                }
                return result;
            }

            /// <summary>
            /// Recursive function to collect Ambient Properties on base types 
            /// </summary>
            static IEnumerable<AmbientPropertyInfo> CreateAllAmbientPropertyList( Type type, IActivityLogger logger )
            {
                if( type == typeof( object ) ) return null;
                return MergeAboveAmbientProperties( CreateAllAmbientPropertyList( type.BaseType, logger ), CreateAmbientPropertyListForExactType( type, logger ), logger );
            }
        }

        internal StObjTypeInfo( IActivityLogger logger, AmbientTypeInfo parent, Type t )
            : base( parent, t )
        {
            ITypeInfoFromParent infoFromParent = Generalization ?? TypeInfoForBaseClasses.GetFor( logger, t.BaseType );

            #region Ambient properties.
            {
                // Ambient properties for the exact Type (can be null).
                IList<AmbientPropertyInfo> collector = CreateAmbientPropertyListForExactType( Type, logger );
                // For type that have no Generalization: we must handle [AmbientProperty] on base classes (no AmbientTypeInfo since they are not Ambient contract).
                // Both fromParent and collector can be null: MergeAboveAmbientProperties handles it.
                AmbientProperties = MergeAboveAmbientProperties( infoFromParent.AmbientProperties, collector, logger ).ToReadOnlyCollection();
            }
            #endregion

            #region IStObjAttribute (ItemKind, Container & Type requirements).
            // There is no Container inheritance at this level.
            var a = CK.Setup.StObjAttribute.GetStObjAttributeForExactType( t, logger );
            if( a != null )
            {
                Container = a.Container;
                ItemKind = a.ItemKind;
                TrackAmbientProperties = a.TrackAmbientProperties;
                RequiredBy = a.RequiredBy;
                Requires = a.Requires;
                Children = a.Children;
                Groups = a.Groups;
            }
            // We inherit only from non Ambient Contract base classes, not from Generalization if it exists.
            // This is to let the inheritance of these 3 properties take dynamic configuration (IStObjStructuralConfigurator) 
            // changes into account: inheritance will take place after configuration so that a change on a base class
            // will be inherited if not explicitely defined at the class level.
            if( Generalization == null )
            {
                if( Container == null ) Container = infoFromParent.Container;
                if( ItemKind == DependentItemType.Unknown ) ItemKind = infoFromParent.ItemKind;
                if( TrackAmbientProperties == TrackAmbientPropertiesMode.Unknown ) TrackAmbientProperties = infoFromParent.TrackAmbientProperties;
            }
            if( Container != null ) ContainerContext = FindContextFromMapAttributes( Container );
            // Requires, Children, Groups and RequiredBy are directly handled by MutableItem (they are wrapped in MutableReference 
            // so that IStObjStructuralConfigurator objects can alter them).
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
                    ConstructParameterTypedContext = ConstructParameters.Length > 0 ? new string[ConstructParameters.Length] : Util.EmptyStringArray;
                    ContainerConstructParameterIndex = -1;
                    for( int i = 0; i < ConstructParameters.Length; ++i )
                    {
                        var p = ConstructParameters[i];

                        // Finds the Context.
                        string parameterContext;
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
                                                                    p.Name, t.FullName, parameterContext, ContainerContext );
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

        public new StObjTypeInfo Generalization { get { return (StObjTypeInfo)base.Generalization; } }

        public IReadOnlyCollection<AmbientPropertyInfo> AmbientProperties { get; private set; }

        public Type Container { get; private set; }

        public readonly string ContainerContext;

        public DependentItemType ItemKind { get; private set; }

        public TrackAmbientPropertiesMode TrackAmbientProperties { get; private set; }

        public readonly Type[] Requires;

        public readonly Type[] RequiredBy;

        public readonly Type[] Children;

        public readonly Type[] Groups;

        public readonly MethodInfo Construct;

        public readonly ParameterInfo[] ConstructParameters;

        public readonly int ContainerConstructParameterIndex;

        public readonly string[] ConstructParameterTypedContext;

        public readonly IStObjStructuralConfigurator[] ConfiguratorAttributes;

        public string FindContextFromMapAttributes( Type t )
        {
            // Attribute ContextMap( Type, string ) is not implemented.
            return null;
        }

        /// <summary>
        /// An ambient property must be public or protected in order to be "specialized" either by overriding (for virtual ones)
        /// or by masking ('new' keyword in C#), typically to support covariance return type.
        /// The "Property Covariance" trick can be supported here because ambient properties are conceptually "read only" properties:
        /// they must be settable only to enable the framework (and no one else) to actually set their values.
        /// </summary>
        static List<AmbientPropertyInfo> CreateAmbientPropertyListForExactType( Type t, IActivityLogger logger )
        {
            var properties = t.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly ).Where( p => !p.Name.Contains( '.' ) );
            List<AmbientPropertyInfo> result = null;
            foreach( var p in properties )
            {
                AmbientPropertyAttribute attr = (AmbientPropertyAttribute)Attribute.GetCustomAttribute( p, typeof( AmbientPropertyAttribute ), false );
                if( attr == null ) continue;

                var mSet = p.GetSetMethod( true );
                var mGet = p.GetGetMethod( true );
                bool isValueMergeable = typeof(IMergeable).IsAssignableFrom( p.PropertyType );
                bool isWriteable = mSet != null && !mSet.IsPrivate;
                if( (!isWriteable && !isValueMergeable ) || (mGet == null || mGet.IsPrivate) )
                {
                    // Warning: not a "Property Covariance" compliant property since
                    // specialized classes will not be able to "override" its signature.
                    // Even if it is not the "Property Covariance" that is targeted, a private get or set
                    // implies a "slicing" of the (base) type that defeats the specialization paradigm (with Covariance).
                    logger.Error( "Property '{0}' of '{1}' can not be considered as an Ambient property. An Ambient property must be readable and writeable (no private setter or getter), or implement CK.Core.IMergeable interface.", p.Name, p.DeclaringType.FullName );
                }
                else
                {
                    if( !isWriteable && isValueMergeable )
                    {
                        throw new NotImplementedException( "Not Writeable but Mergeable properties are not yet supported (need a IStObjMutableItem.SetPropertyStructuralSetter to initialize it)." );
                    }
                    if( result == null ) result = new List<AmbientPropertyInfo>();
                    Debug.Assert( result.Find( a => a.Name == p.Name ) == null, "No homonym properties in .Net framework." );
                    result.Add( new AmbientPropertyInfo( p, attr, isWriteable, isValueMergeable ) );
                }
            }
            return result;
        }

        static IEnumerable<AmbientPropertyInfo> MergeAboveAmbientProperties( IEnumerable<AmbientPropertyInfo> above, IList<AmbientPropertyInfo> collector, IActivityLogger logger )
        {
            if( collector == null || collector.Count == 0 ) return above ?? ReadOnlyListEmpty<AmbientPropertyInfo>.Empty;
            if( above != null )
            {
                // Adds 'above' into 'collector' before returning it.
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




    }
}
