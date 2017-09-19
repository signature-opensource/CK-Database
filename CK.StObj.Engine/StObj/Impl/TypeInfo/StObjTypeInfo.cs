#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\Impl\TypeInfo\StObjTypeInfo.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
            public IReadOnlyList<InjectContractInfo> AmbientContracts { get; private set; }
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

            static public IStObjTypeInfoFromParent GetFor( IActivityMonitor monitor, Type t )
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
                            result.AmbientContracts = Util.Array.Empty<InjectContractInfo>();
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
                            Type tAbove = t.GetTypeInfo().BaseType;
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
                                tAbove = tAbove.GetTypeInfo().BaseType;
                            }
                            // Ambient, Contracts & StObj Properties (uses a recursive function).
                            List<StObjPropertyInfo> stObjProperties = new List<StObjPropertyInfo>();
                            IReadOnlyList<AmbientPropertyInfo> apList;
                            IReadOnlyList<InjectContractInfo> acList;
                            CreateAllAmbientPropertyList( monitor, t, result.SpecializationDepth, stObjProperties, out apList, out acList );
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
                List<StObjPropertyInfo> stObjProperties,
                out IReadOnlyList<AmbientPropertyInfo> apListResult,
                out IReadOnlyList<InjectContractInfo> acListResult )
            {
                if( type == typeof( object ) )
                {
                    apListResult = Util.Array.Empty<AmbientPropertyInfo>();
                    acListResult = Util.Array.Empty<InjectContractInfo>();
                }
                else
                {
                    IList<AmbientPropertyInfo> apCollector;
                    IList<InjectContractInfo> acCollector;
                    AmbientPropertyOrInjectContractInfo.CreateAmbientPropertyListForExactType( monitor, type, specializationLevel, stObjProperties, out apCollector, out acCollector );

                    CreateAllAmbientPropertyList( monitor, type.GetTypeInfo().BaseType, specializationLevel - 1, stObjProperties, out apListResult, out acListResult );

                    apListResult = AmbientPropertyOrInjectContractInfo.MergeWithAboveProperties( monitor, apListResult, apCollector );
                    acListResult = AmbientPropertyOrInjectContractInfo.MergeWithAboveProperties( monitor, acListResult, acCollector );
                }
            }
        }

        internal static readonly StObjTypeInfo Empty = new StObjTypeInfo();

        internal StObjTypeInfo( IActivityMonitor monitor, AmbientTypeInfo parent, Type t )
            : base( parent, t )
        {
            IStObjTypeInfoFromParent infoFromParent = Generalization ?? TypeInfoForBaseClasses.GetFor( monitor, t.GetTypeInfo().BaseType );
            SpecializationDepth = infoFromParent.SpecializationDepth + 1;

            // StObj properties are initialized with inherited (non Ambient Contract ones).
            List<StObjPropertyInfo> stObjProperties = new List<StObjPropertyInfo>();
            if( Generalization == null ) stObjProperties.AddRange( infoFromParent.StObjProperties );
            // StObj properties are then read from StObjPropertyAttribute on class
            foreach( StObjPropertyAttribute p in t.GetTypeInfo().GetCustomAttributes( typeof( StObjPropertyAttribute ), Generalization == null ) )
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
            IList<InjectContractInfo> acCollector;
            AmbientPropertyInfo.CreateAmbientPropertyListForExactType( monitor, Type, SpecializationDepth, stObjProperties, out apCollector, out acCollector );
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
            // We inherit only from non Ambient Contract base classes, not from Generalization if it exists.
            // This is to let the inheritance of these 3 properties take dynamic configuration (IStObjStructuralConfigurator) 
            // changes into account: inheritance will take place after configuration so that a change on a base class
            // will be inherited if not explicitly defined at the class level.
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

            #region StObjConstruct method & parameters
            StObjConstruct = t.GetMethod( StObjContextRoot.ConstructMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly );
            // From Construct to StObjConstruct...
            if( StObjConstruct == null )
            {
                StObjConstruct = t.GetMethod("Construct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if( StObjConstruct != null )
                {
                    monitor.Warn( $"Deprecated: Method '{t.FullName}.Construct' must be named '{StObjContextRoot.ConstructMethodName}' instead." );
                }
            }
            if ( StObjConstruct != null )
            {
                if( StObjConstruct.IsVirtual )
                {
                    monitor.Error( $"Method '{t.FullName}.{StObjContextRoot.ConstructMethodName}' must NOT be virtual.");
                }
                else
                {
                    ConstructParameters = StObjConstruct.GetParameters();
                    ConstructParameterTypedContext = ConstructParameters.Length > 0 ? new string[ConstructParameters.Length] : Util.Array.Empty<string>();
                    ContainerConstructParameterIndex = -1;
                    for( int i = 0; i < ConstructParameters.Length; ++i )
                    {
                        var p = ConstructParameters[i];

                        // Finds the Context.
                        string parameterContext;
                        ContextAttribute ctx = p.GetCustomAttribute<ContextAttribute>();
                        if( ctx != null ) parameterContext = ctx.Context;
                        else parameterContext = FindContextFromMapAttributes( p.ParameterType );
                        ConstructParameterTypedContext[i] = parameterContext;

                        // Is it marked with ContainerAttribute?
                        bool isContainerParameter = p.GetCustomAttribute<ContainerAttribute>() != null;
                        if(isContainerParameter)
                        {
                            if( ContainerConstructParameterIndex >= 0 )
                            {
                                monitor.Error( $"'{t.FullName}.{StObjContextRoot.ConstructMethodName}' method has more than one parameter marked with [Container] attribute.");
                            }
                            else
                            {
                                // The Parameter is the Container.
                                if( Container != null && Container != p.ParameterType )
                                {
                                    monitor.Error( $"'{t.FullName}.{StObjContextRoot.ConstructMethodName}' method parameter '{p.Name}' defines the Container as '{p.ParameterType.FullName}' but an attribute on the class declares the Container as '{Container.FullName}'." );
                                }
                                else if( ContainerContext != null && ContainerContext != parameterContext )
                                {
                                    monitor.Error( $"'{t.FullName}.{StObjContextRoot.ConstructMethodName}' method parameter '{p.Name}' targets the Container in '{parameterContext}' but an attribute on the class declares the Container context as '{ContainerContext}'.");
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

            #region StObjInitialize method checks: (non virtual) void Initialize( IActivityMonitor, IContextualStObjMap)
            var initialize = t.GetMethod(StObjContextRoot.InitializeMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (initialize != null)
            {
                if (initialize.IsVirtual)
                {
                    monitor.Error( $"'{t.FullName}.{StObjContextRoot.InitializeMethodName}' method must NOT be virtual.");
                }
                else
                {
                    var parameters = initialize.GetParameters();
                    if (parameters.Length != 2
                        || parameters[0].ParameterType != typeof(IActivityMonitor)
                        || parameters[1].ParameterType != typeof(IContextualStObjMap))
                    {
                        monitor.Error( $"'{t.FullName}.{StObjContextRoot.InitializeMethodName}' method parameters must be (IActivityMonitor, IContextualStObjMap).");
                    }
                }
            }
            #endregion

            }

        /// <summary>
        /// Used only for Empty Item Pattern implementations.
        /// </summary>
        private StObjTypeInfo()
            : base() 
        {
        }

        public new StObjTypeInfo Generalization => (StObjTypeInfo)base.Generalization;

        public IReadOnlyList<AmbientPropertyInfo> AmbientProperties { get; private set; }

        public IReadOnlyList<InjectContractInfo> AmbientContracts { get; private set; }

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

        public readonly MethodInfo StObjConstruct;

        public readonly ParameterInfo[] ConstructParameters;

        public readonly int ContainerConstructParameterIndex;

        public readonly string[] ConstructParameterTypedContext;

        public string FindContextFromMapAttributes( Type t )
        {
            // Attribute ContextMap( Type, string ) is not implemented.
            return null;
        }

        protected internal override TC CreateContextTypeInfo<T, TC>( IActivityMonitor monitor, IServiceProvider services, TC generalization, IContextualTypeMap context )
        {
            return (TC)(object)(new MutableItem( monitor, this, (MutableItem)((object)generalization), context, services ));
        }

    }
}
