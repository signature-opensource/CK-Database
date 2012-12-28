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
            public IReadOnlyList<AmbientPropertyInfo> AmbientProperties { get; private set; }
            public IReadOnlyList<AmbientContractInfo> AmbientContracts { get; private set; }
            public IReadOnlyList<StObjPropertyInfo> StObjProperties { get; private set; }
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
                            result.AmbientContracts = ReadOnlyListEmpty<AmbientContractInfo>.Empty;
                            result.StObjProperties = ReadOnlyListEmpty<StObjPropertyInfo>.Empty;
                        }
                        else
                        {
                            // At least below object :-).
                            result.SpecializationDepth = 1;
                            // For ItemKind & TrackAmbientProperties, walks up the inheritance chain and combines the StObjAttribute.
                            // We compute the SpecializationDepth: once we know it, we can inject it the Ambient Properties discovery.
                            var a = AttributesReader.GetStObjAttributeForExactType( t, logger );
                            if( a != null )
                            {
                                result.Container = a.Container;
                                result.ItemKind = (DependentItemKind)a.ItemKind;
                                result.TrackAmbientProperties = a.TrackAmbientProperties;
                            }
                            Type tAbove = t.BaseType;
                            while( tAbove != typeof( object ) )
                            {
                                result.SpecializationDepth = result.SpecializationDepth + 1;
                                if( !result.IsFullyDefined )
                                {
                                    var aAbove = AttributesReader.GetStObjAttributeForExactType( tAbove, logger );
                                    if( aAbove != null )
                                    {
                                        if( result.Container == null ) result.Container = aAbove.Container;
                                        if( result.ItemKind == DependentItemKind.Unknown ) result.ItemKind = (DependentItemKind)aAbove.ItemKind;
                                        if( result.TrackAmbientProperties == TrackAmbientPropertiesMode.Unknown ) result.TrackAmbientProperties = aAbove.TrackAmbientProperties;
                                    }
                                }
                                tAbove = tAbove.BaseType;
                            }
                            // Ambient, Contracts & StObj Properties (uses a recursive function).
                            List<StObjPropertyInfo> stObjProperties = new List<StObjPropertyInfo>();
                            IReadOnlyList<AmbientPropertyInfo> apList;
                            IReadOnlyList<AmbientContractInfo> acList;
                            CreateAllAmbientPropertyList( logger, t, result.SpecializationDepth, stObjProperties, out apList, out acList );
                            Debug.Assert( apList != null && acList != null );
                            result.AmbientProperties = apList;
                            result.AmbientContracts = acList;
                            result.StObjProperties = stObjProperties.ToReadOnlyList();
                        }
                        _cache.Add( t, result );
                    }
                }
                return result;
            }

            /// <summary>
            /// Recursive function to collect/merge Ambient Properties, Contracts and StObj Properties on base (non IAmbientContract) types.
            /// </summary>
            static void CreateAllAmbientPropertyList(
                IActivityLogger logger,
                Type type,
                int specializationLevel,
                List<StObjPropertyInfo> stObjProperties,
                out IReadOnlyList<AmbientPropertyInfo> apListResult,
                out IReadOnlyList<AmbientContractInfo> acListResult )
            {
                if( type == typeof( object ) )
                {
                    apListResult = ReadOnlyListEmpty<AmbientPropertyInfo>.Empty;
                    acListResult = ReadOnlyListEmpty<AmbientContractInfo>.Empty;
                }
                else
                {
                    IList<AmbientPropertyInfo> apCollector;
                    IList<AmbientContractInfo> acCollector;
                    AmbientPropertyOrContractInfo.CreateAmbientPropertyListForExactType( logger, type, specializationLevel, stObjProperties, out apCollector, out acCollector );

                    CreateAllAmbientPropertyList( logger, type.BaseType, specializationLevel - 1, stObjProperties, out apListResult, out acListResult );

                    apListResult = AmbientPropertyOrContractInfo.MergeWithAboveProperties( logger, apListResult, apCollector );
                    acListResult = AmbientPropertyOrContractInfo.MergeWithAboveProperties( logger, acListResult, acCollector );
                }
            }
        }

        internal static readonly StObjTypeInfo Empty = new StObjTypeInfo();

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
                if( String.IsNullOrWhiteSpace( p.PropertyName ) )
                {
                    logger.Error( "Unamed or whitespace StObj property on '{0}'. Attribute must be configured with a valid PropertyName.", t.FullName );
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
                    stObjProperties.Add( new StObjPropertyInfo( t, p.ResolutionSource, p.PropertyName, p.PropertyType, null ) );
                }
            }
            // Ambient properties for the exact Type (can be null). 
            // In the same time, StObjPropertyAttribute that are associated to actual properties are collected into stObjProperties.
            IList<AmbientPropertyInfo> apCollector;
            IList<AmbientContractInfo> acCollector;
            AmbientPropertyInfo.CreateAmbientPropertyListForExactType( logger, Type, SpecializationDepth, stObjProperties, out apCollector, out acCollector );
            // For type that have no Generalization: we must handle [AmbientProperty], [AmbientContract] and [StObjProperty] on base classes (we may not have AmbientTypeInfo object 
            // since they are not necessarily IAmbientContract, we use infoFromParent abstraction).
            AmbientProperties = AmbientPropertyInfo.MergeWithAboveProperties( logger, infoFromParent.AmbientProperties, apCollector );
            AmbientContracts = AmbientPropertyInfo.MergeWithAboveProperties( logger, infoFromParent.AmbientContracts, acCollector );
            StObjProperties = stObjProperties.ToReadOnlyList();
            Debug.Assert( AmbientContracts != null && AmbientProperties != null && StObjProperties != null );

            // Simple detection of name clashing: I prefer to keep it simple and check property kind conherency here instead of injecting 
            // the detection inside CreateAmbientPropertyListForExactType and MergeWithAboveProperties with a multi-type property collector. 
            // Code is complicated enough and it should be not reaally less efficient to use the dictionary below once all properties
            // have been resolved...
            {
                var names = new Dictionary<string, INamedPropertyInfo>();
                foreach( var newP in AmbientProperties.Cast<INamedPropertyInfo>().Concat( AmbientContracts ).Concat( StObjProperties ) )
                {
                    INamedPropertyInfo exists;
                    if( names.TryGetValue( newP.Name, out exists ) )
                    {
                        logger.Error( "{0} property '{1}.{2}' is declared as a '{3}' property by '{4}'. Property names must be distinct.", newP.Kind, newP.DeclaringType.FullName, newP.Name, exists.Kind, exists.DeclaringType.FullName );
                    }
                    else names.Add( newP.Name, newP );
                }
            }

            #region IStObjAttribute (ItemKind, Container & Type requirements).
            // There is no Container inheritance at this level.
            var a = AttributesReader.GetStObjAttributeForExactType( t, logger );
            if( a != null )
            {
                Container = a.Container;
                ItemKind = (DependentItemKind)a.ItemKind;
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

        }

        /// <summary>
        /// Used only for Empty Object Pattern implementations.
        /// </summary>
        private StObjTypeInfo()
            : base() 
        {
        }

        public new StObjTypeInfo Generalization { get { return (StObjTypeInfo)base.Generalization; } }

        public IReadOnlyList<AmbientPropertyInfo> AmbientProperties { get; private set; }

        public IReadOnlyList<AmbientContractInfo> AmbientContracts { get; private set; }

        public IReadOnlyList<StObjPropertyInfo> StObjProperties { get; private set; }

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

        public string FindContextFromMapAttributes( Type t )
        {
            // Attribute ContextMap( Type, string ) is not implemented.
            return null;
        }

        protected internal override TC CreateContextTypeInfo<T, TC>( string context, TC specialization )
        {
            return (TC)(object)(new MutableItem( this, context, (MutableItem)((object)specialization) ));
        }

    }
}
