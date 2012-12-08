using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection;
using System.Diagnostics;

namespace CK.Setup
{
    internal class StObjTypeInfo : AmbientTypeInfo, IStObjTypeInfoFromParent
    {
        class TypeInfoForBaseClasses : IStObjTypeInfoFromParent
        {
            public IReadOnlyCollection<AmbientPropertyInfo> AmbientProperties { get; private set; }
            public IReadOnlyCollection<StObjPropertyInfo> StObjProperties { get; private set; }
            public int SpecializationDepth { get; private set; }
            public Type Container { get; private set; }
            public DependentItemKind ItemKind { get; private set; }
            public TrackAmbientPropertiesMode TrackAmbientProperties { get; private set; }

            bool IsFullyDefined
            {
                get { return Container != null && ItemKind != DependentItemKind.Unknown && TrackAmbientProperties != TrackAmbientPropertiesMode.Unknown; }
            }

            static object _lock = new object();
            static Dictionary<Type,TypeInfoForBaseClasses> _cache;

            static public IStObjTypeInfoFromParent GetFor( IActivityLogger logger, Type t )
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
                        if( t == typeof( object ) )
                        {
                            result.AmbientProperties = ReadOnlyListEmpty<AmbientPropertyInfo>.Empty;
                            result.StObjProperties = ReadOnlyListEmpty<StObjPropertyInfo>.Empty;
                        }
                        else
                        {
                            // At least below object :-).
                            result.SpecializationDepth = 1;
                            // For ItemKind & TrackAmbientProperties, walks up the inheritance chain and combines the StObjAttribute.
                            // We compute the SpecializationDepth: once we know it, we can inject it the Ambient Properties discovery.
                            var a = CK.Setup.StObjAttribute.GetStObjAttributeForExactType( t, logger );
                            if( a != null )
                            {
                                result.Container = a.Container;
                                result.ItemKind = a.ItemKind;
                                result.TrackAmbientProperties = a.TrackAmbientProperties;
                            }
                            Type tAbove = t.BaseType;
                            while( tAbove != typeof( object ) )
                            {
                                result.SpecializationDepth = result.SpecializationDepth + 1;
                                if( !result.IsFullyDefined )
                                {
                                    var aAbove = CK.Setup.StObjAttribute.GetStObjAttributeForExactType( tAbove, logger );
                                    if( aAbove != null )
                                    {
                                        if( result.Container == null ) result.Container = aAbove.Container;
                                        if( result.ItemKind == DependentItemKind.Unknown ) result.ItemKind = aAbove.ItemKind;
                                        if( result.TrackAmbientProperties == TrackAmbientPropertiesMode.Unknown ) result.TrackAmbientProperties = aAbove.TrackAmbientProperties;
                                    }
                                }
                                tAbove = tAbove.BaseType;
                            }
                            // Ambient Properties (uses a recursive function).
                            List<StObjPropertyInfo> stObjProperties = new List<StObjPropertyInfo>();
                            var all = AmbientPropertyInfo.CreateAllAmbientPropertyList( t, result.SpecializationDepth, logger, stObjProperties );
                            result.AmbientProperties = all != null ? all.ToReadOnlyCollection() : ReadOnlyListEmpty<AmbientPropertyInfo>.Empty;
                            result.StObjProperties = stObjProperties.ToReadOnlyList();
                        }
                        _cache.Add( t, result );
                    }
                }
                return result;
            }

        }

        internal StObjTypeInfo( IActivityLogger logger, AmbientTypeInfo parent, Type t )
            : base( parent, t )
        {
            IStObjTypeInfoFromParent infoFromParent = Generalization ?? TypeInfoForBaseClasses.GetFor( logger, t.BaseType );
            SpecializationDepth = infoFromParent.SpecializationDepth + 1;

            // StObj properties are initialized with inherited (non Ambient Contract ones).
            List<StObjPropertyInfo> stObjProperties = new List<StObjPropertyInfo>();
            if( Generalization == null ) stObjProperties.AddRange( infoFromParent.StObjProperties );
            // StObj properties are then read from StObjPropertyAttribute on class
            foreach( StObjPropertyAttribute p in t.GetCustomAttributes( typeof( StObjPropertyAttribute ), Generalization == null ) )
            {
                if( String.IsNullOrEmpty( p.PropertyName ) )
                {
                    logger.Error( "Unamed StObj property on '{1}'. Attribute must be configured with a valid PropertyName.", t.FullName );
                }
                else if( p.PropertyType == null )
                {
                    logger.Error( "StObj property named '{0}' for '{1}' has no PropertyType defined. It should be typeof(object) to explicitely express that any type is accepted.", p.PropertyName, t.FullName );
                }
                else if( stObjProperties.Find( sP => sP.Name == p.PropertyName ) != null )
                {
                    logger.Error( "StObj property named '{0}' for '{1}' is defined more than once. It should be declared only once.", p.PropertyName, t.FullName );
                }
                else
                {
                    stObjProperties.Add( new StObjPropertyInfo( p.PropertyName, p.PropertyType, null ) );
                }
            }           
            // Ambient properties for the exact Type (can be null). 
            // In the same time, StObjPropertyAttribute that are associated to properties are collected into stObjProperties.
            IList<AmbientPropertyInfo> apCollector;
            AmbientPropertyInfo.CreateAmbientPropertyListForExactType( logger, Type, SpecializationDepth, stObjProperties, out apCollector );
            // For type that have no Generalization: we must handle [AmbientProperty] on base classes (no AmbientTypeInfo since they are not Ambient contract).
            // Both fromParent and collector can be null: MergeAboveAmbientProperties handles it.
            AmbientProperties = AmbientPropertyInfo.MergeAboveAmbientProperties( logger, infoFromParent.AmbientProperties, apCollector ).ToReadOnlyCollection();

            StObjProperties = stObjProperties.ToReadOnlyList();

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
                if( ItemKind == DependentItemKind.Unknown ) ItemKind = infoFromParent.ItemKind;
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

        public IReadOnlyCollection<StObjPropertyInfo> StObjProperties { get; private set; }

        public Type Container { get; private set; }

        public int SpecializationDepth { get; private set; }
        
        public readonly string ContainerContext;

        public DependentItemKind ItemKind { get; private set; }

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

    }
}
