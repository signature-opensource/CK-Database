using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Reflection;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Specialized <see cref="AmbientTypeInfo"/> for <see cref="IAmbientObject"/> classes.
    /// </summary>
    internal class AmbientObjectClassInfo : AmbientTypeInfo, IStObjTypeInfoFromParent
    {
        Type[] _ambientInterfaces;
        Type[] _thisAmbientInterfaces;

        class TypeInfoForBaseClasses : IStObjTypeInfoFromParent
        {
            public IReadOnlyList<AmbientPropertyInfo> AmbientProperties { get; private set; }
            public IReadOnlyList<InjectSingletonInfo> AmbientContracts { get; private set; }
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

            static public IStObjTypeInfoFromParent GetFor( IActivityMonitor monitor, Type t, AmbientTypeKindDetector ambientTypeKind )
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
                            result.AmbientProperties = Util.Array.Empty<AmbientPropertyInfo>();
                            result.AmbientContracts = Util.Array.Empty<InjectSingletonInfo>();
                            result.StObjProperties = Util.Array.Empty<StObjPropertyInfo>();
                        }
                        else
                        {
                            // At least below object :-).
                            result.SpecializationDepth = 1;
                            // For ItemKind & TrackAmbientProperties, walks up the inheritance chain and combines the StObjAttribute.
                            // We compute the SpecializationDepth: once we know it, we can inject it the Ambient Properties discovery.
                            var a = StObjAttributesReader.GetStObjAttributeForExactType( t, monitor );
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
                                    var aAbove = StObjAttributesReader.GetStObjAttributeForExactType( tAbove, monitor );
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
                            IReadOnlyList<InjectSingletonInfo> acList;
                            CreateAllAmbientPropertyList( monitor, t, result.SpecializationDepth, ambientTypeKind, stObjProperties, out apList, out acList );
                            Debug.Assert( apList != null && acList != null );
                            result.AmbientProperties = apList;
                            result.AmbientContracts = acList;
                            result.StObjProperties = stObjProperties;
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
                IActivityMonitor monitor,
                Type type,
                int specializationLevel,
                AmbientTypeKindDetector ambientTypeKind,
                List<StObjPropertyInfo> stObjProperties,
                out IReadOnlyList<AmbientPropertyInfo> apListResult,
                out IReadOnlyList<InjectSingletonInfo> acListResult )
            {
                if( type == typeof( object ) )
                {
                    apListResult = Util.Array.Empty<AmbientPropertyInfo>();
                    acListResult = Util.Array.Empty<InjectSingletonInfo>();
                }
                else
                {
                    IList<AmbientPropertyInfo> apCollector;
                    IList<InjectSingletonInfo> acCollector;
                    AmbientPropertyOrInjectSingletonInfo.CreateAmbientPropertyListForExactType( monitor, type, specializationLevel, ambientTypeKind, stObjProperties, out apCollector, out acCollector );

                    CreateAllAmbientPropertyList( monitor, type.BaseType, specializationLevel - 1, ambientTypeKind, stObjProperties, out apListResult, out acListResult );

                    apListResult = AmbientPropertyOrInjectSingletonInfo.MergeWithAboveProperties( monitor, apListResult, apCollector );
                    acListResult = AmbientPropertyOrInjectSingletonInfo.MergeWithAboveProperties( monitor, acListResult, acCollector );
                }
            }
        }

        internal AmbientObjectClassInfo( IActivityMonitor monitor, AmbientObjectClassInfo parent, Type t, IServiceProvider provider, AmbientTypeKindDetector ambientTypeKind, bool isExcluded )
            : base( monitor, parent, t, provider, isExcluded )
        {
            Debug.Assert( parent == Generalization );
            if( IsExcluded ) return;

            IStObjTypeInfoFromParent infoFromParent = Generalization ?? TypeInfoForBaseClasses.GetFor( monitor, t.BaseType, ambientTypeKind );
            SpecializationDepth = infoFromParent.SpecializationDepth + 1;

            // StObj properties are initialized with inherited (non Ambient Object ones).
            List<StObjPropertyInfo> stObjProperties = new List<StObjPropertyInfo>();
            if( Generalization == null ) stObjProperties.AddRange( infoFromParent.StObjProperties );
            // StObj properties are then read from StObjPropertyAttribute on class
            foreach( StObjPropertyAttribute p in t.GetCustomAttributes( typeof( StObjPropertyAttribute ), Generalization == null ) )
            {
                if( String.IsNullOrWhiteSpace( p.PropertyName ) )
                {
                    monitor.Error( $"Unnamed or whitespace StObj property on '{t.FullName}'. Attribute must be configured with a valid PropertyName." );
                }
                else if( p.PropertyType == null )
                {
                    monitor.Error( $"StObj property named '{p.PropertyName}' for '{t.FullName}' has no PropertyType defined. It should be typeof(object) to explicitly express that any type is accepted." );
                }
                else if( stObjProperties.Find( sP => sP.Name == p.PropertyName ) != null )
                {
                    monitor.Error( $"StObj property named '{p.PropertyName}' for '{t.FullName}' is defined more than once. It should be declared only once." );
                }
                else
                {
                    stObjProperties.Add( new StObjPropertyInfo( t, p.ResolutionSource, p.PropertyName, p.PropertyType, null ) );
                }
            }
            // Ambient properties for the exact Type (can be null). 
            // In the same time, StObjPropertyAttribute that are associated to actual properties are collected into stObjProperties.
            IList<AmbientPropertyInfo> apCollector;
            IList<InjectSingletonInfo> acCollector;
            AmbientPropertyInfo.CreateAmbientPropertyListForExactType( monitor, Type, SpecializationDepth, ambientTypeKind, stObjProperties, out apCollector, out acCollector );
            // For type that have no Generalization: we must handle [AmbientProperty], [AmbientContract] and [StObjProperty] on base classes (we may not have AmbientTypeInfo object 
            // since they are not necessarily IAmbientContract, we use infoFromParent abstraction).
            AmbientProperties = AmbientPropertyInfo.MergeWithAboveProperties( monitor, infoFromParent.AmbientProperties, apCollector );
            AmbientContracts = AmbientPropertyInfo.MergeWithAboveProperties( monitor, infoFromParent.AmbientContracts, acCollector );
            StObjProperties = stObjProperties;
            Debug.Assert( AmbientContracts != null && AmbientProperties != null && StObjProperties != null );

            // Simple detection of name clashing: I prefer to keep it simple and check property kind coherency here instead of injecting 
            // the detection inside CreateAmbientPropertyListForExactType and MergeWithAboveProperties with a multi-type property collector. 
            // Code is complicated enough and it should be not really less efficient to use the dictionary below once all properties
            // have been resolved...
            {
                var names = new Dictionary<string, INamedPropertyInfo>();
                foreach( var newP in AmbientProperties.Cast<INamedPropertyInfo>().Concat( AmbientContracts ).Concat( StObjProperties ) )
                {
                    INamedPropertyInfo exists;
                    if( names.TryGetValue( newP.Name, out exists ) )
                    {
                        monitor.Error( $"{newP.Kind} property '{newP.DeclaringType.FullName}.{newP.Name}' is declared as a '{exists.Kind}' property by '{exists.DeclaringType.FullName}'. Property names must be distinct." );
                    }
                    else names.Add( newP.Name, newP );
                }
            }

            #region IStObjAttribute (ItemKind, Container & Type requirements).
            // There is no Container inheritance at this level.
            var a = StObjAttributesReader.GetStObjAttributeForExactType( t, monitor );
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
            // We inherit only from non Ambient Object base classes, not from Generalization if it exists.
            // This is to let the inheritance of these 3 properties take dynamic configuration (IStObjStructuralConfigurator) 
            // changes into account: inheritance will take place after configuration so that a change on a base class
            // will be inherited if not explicitly defined at the class level.
            if( Generalization == null )
            {
                if( Container == null ) Container = infoFromParent.Container;
                if( ItemKind == DependentItemKind.Unknown ) ItemKind = infoFromParent.ItemKind;
                if( TrackAmbientProperties == TrackAmbientPropertiesMode.Unknown ) TrackAmbientProperties = infoFromParent.TrackAmbientProperties;
            }
            // Requires, Children, Groups and RequiredBy are directly handled by MutableItem (they are wrapped in MutableReference 
            // so that IStObjStructuralConfigurator objects can alter them).
            #endregion

            #region StObjConstruct method & parameters
            StObjConstruct = t.GetMethod( StObjContextRoot.ConstructMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly );
            // From Construct to StObjConstruct...
            if( StObjConstruct == null )
            {
                StObjConstruct = t.GetMethod( "Construct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly );
                if( StObjConstruct != null )
                {
                    monitor.Warn( $"Deprecated: Method '{t.FullName}.Construct' must be named '{StObjContextRoot.ConstructMethodName}' instead." );
                }
            }
            if( StObjConstruct != null )
            {
                if( StObjConstruct.IsVirtual )
                {
                    monitor.Error( $"Method '{t.FullName}.{StObjContextRoot.ConstructMethodName}' must NOT be virtual." );
                }
                else
                {
                    ConstructParameters = StObjConstruct.GetParameters();
                    ContainerConstructParameterIndex = -1;
                    for( int i = 0; i < ConstructParameters.Length; ++i )
                    {
                        var p = ConstructParameters[i];

                        // Is it marked with ContainerAttribute?
                        bool isContainerParameter = p.GetCustomAttribute<ContainerAttribute>() != null;
                        if( isContainerParameter )
                        {
                            if( ContainerConstructParameterIndex >= 0 )
                            {
                                monitor.Error( $"'{t.FullName}.{StObjContextRoot.ConstructMethodName}' method has more than one parameter marked with [Container] attribute." );
                            }
                            else
                            {
                                // The Parameter is the Container.
                                if( Container != null && Container != p.ParameterType )
                                {
                                    monitor.Error( $"'{t.FullName}.{StObjContextRoot.ConstructMethodName}' method parameter '{p.Name}' defines the Container as '{p.ParameterType.FullName}' but an attribute on the class declares the Container as '{Container.FullName}'." );
                                }
                                ContainerConstructParameterIndex = i;
                                Container = p.ParameterType;
                            }
                        }
                    }
                }
            }
            #endregion

            #region StObjInitialize method checks: (non virtual) void Initialize( IActivityMonitor, IStObjMap )
            var initialize = t.GetMethod( StObjContextRoot.InitializeMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly );
            if( initialize != null )
            {
                if( initialize.IsVirtual )
                {
                    monitor.Error( $"'{t.FullName}.{StObjContextRoot.InitializeMethodName}' method must NOT be virtual." );
                }
                else
                {
                    var parameters = initialize.GetParameters();
                    if( parameters.Length != 2
                        || parameters[0].ParameterType != typeof( IActivityMonitor )
                        || parameters[1].ParameterType != typeof( IStObjMap ) )
                    {
                        monitor.Error( $"'{t.FullName}.{StObjContextRoot.InitializeMethodName}' method parameters must be (IActivityMonitor, IStObjMap)." );
                    }
                }
            }
            #endregion

        }

        public new AmbientObjectClassInfo Generalization => (AmbientObjectClassInfo)base.Generalization;

        public IReadOnlyList<AmbientPropertyInfo> AmbientProperties { get; private set; }

        public IReadOnlyList<InjectSingletonInfo> AmbientContracts { get; private set; }

        public IReadOnlyList<StObjPropertyInfo> StObjProperties { get; private set; }

        public Type Container { get; private set; }

        /// <summary>
        /// Gets the specialization depth from root object type (Object's depth being 0).
        /// </summary>
        public int SpecializationDepth { get; private set; }
        
        public DependentItemKind ItemKind { get; private set; }

        public TrackAmbientPropertiesMode TrackAmbientProperties { get; private set; }

        public readonly Type[] Requires;

        public readonly Type[] RequiredBy;

        public readonly Type[] Children;

        public readonly Type[] Groups;

        public readonly MethodInfo StObjConstruct;

        public readonly ParameterInfo[] ConstructParameters;

        public readonly int ContainerConstructParameterIndex;

        Type[] EnsureAllAmbientInterfaces()
        {
            return _ambientInterfaces
                ?? (_ambientInterfaces = Type.GetInterfaces().Where( t => t != typeof( IAmbientObject )
                                                                          && typeof( IAmbientObject ).IsAssignableFrom( t ) ).ToArray());
        }

        internal Type[] EnsureThisAmbientInterfaces()
        {
            return _thisAmbientInterfaces ?? (_thisAmbientInterfaces = Generalization != null
                                                        ? EnsureAllAmbientInterfaces().Except( Generalization.EnsureAllAmbientInterfaces() ).ToArray()
                                                        : EnsureAllAmbientInterfaces());
        }


        internal bool CreateMutableItemsPath(
            IActivityMonitor monitor,
            IServiceProvider services,
            StObjObjectEngineMap engineMap,
            MutableItem generalization,
            IDynamicAssembly tempAssembly,
            List<(MutableItem, ImplementableTypeInfo)> lastConcretes,
            List<Type> abstractTails )
        {
            Debug.Assert( tempAssembly != null );
            var item = new MutableItem( this, generalization, engineMap );
            bool concreteBelow = false;
            foreach( AmbientObjectClassInfo c in Specializations )
            {
                Debug.Assert( !c.IsExcluded );
                concreteBelow |= c.CreateMutableItemsPath( monitor, services, engineMap, item, tempAssembly, lastConcretes, abstractTails );
            }
            if( !concreteBelow )
            {
                ImplementableTypeInfo autoImplementor = null;
                if( Type.IsAbstract
                    && (autoImplementor = CreateAbstractTypeImplementation( monitor, tempAssembly)) == null )
                {
                    abstractTails.Add( Type );
                    Generalization?.RemoveSpecialization( this );
                }
                else
                {
                    lastConcretes.Add( (item, autoImplementor) );
                    concreteBelow = true;
                }
            }
            return concreteBelow;
        }

    }
}
