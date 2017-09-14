#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\Impl\TypeInfo\AmbientPropertyOrInjectContractInfo.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using CK.Core;

namespace CK.Setup
{

    internal abstract class AmbientPropertyOrInjectContractInfo : CovariantPropertyInfo
    {
        readonly bool _isOptionalDefined;
        bool _isOptional;

        internal AmbientPropertyOrInjectContractInfo( PropertyInfo p, bool isOptionalDefined, bool isOptional, int definerSpecializationDepth, int index )
            : base( p, definerSpecializationDepth, index )
        {
            _isOptionalDefined = isOptionalDefined;
            _isOptional = isOptional;
            ContextAttribute c = p.GetCustomAttribute<ContextAttribute>(false);
            if( c != null ) Context = c.Context;
        }

        /// <summary>
        /// This is settable in order for base class property's context to be used if it is not explicitly defined
        /// by the specialized property.
        /// </summary>
        public string Context { get; private set; }

        public bool IsOptional => _isOptional; 

        protected override void SetGeneralizationInfo( IActivityMonitor monitor, CovariantPropertyInfo g )
        {
            base.SetGeneralizationInfo( monitor, g );
            AmbientPropertyOrInjectContractInfo gen = (AmbientPropertyOrInjectContractInfo)g;
            // A required property can not become optional.
            if( IsOptional && !gen.IsOptional )
            {
                if( _isOptionalDefined )
                {
                    monitor.Error( $"{Kind}: Property '{DeclaringType.FullName}.{Name}' states that it is optional but base property '{gen.DeclaringType.FullName}.{Name}' is required." );
                }
                _isOptional = false;
            }
            // Context inheritance (if not defined).
            if( Context == null )
            {
                Context = gen.Context;
            }
        }

        /// <summary>
        /// An ambient property must be public or protected in order to be "specialized" either by overriding (for virtual ones)
        /// or by masking ('new' keyword in C#), typically to support covariance return type.
        /// The "Property Covariance" trick can be supported here because ambient properties are conceptually "read only" properties:
        /// they must be settable only to enable the framework (and no one else) to actually set their values.
        /// </summary>
        static public void CreateAmbientPropertyListForExactType( 
            IActivityMonitor monitor, 
            Type t, 
            int definerSpecializationDepth, 
            List<StObjPropertyInfo> stObjProperties, 
            out IList<AmbientPropertyInfo> apListResult,
            out IList<InjectContractInfo> acListResult )
        {
            Debug.Assert( stObjProperties != null );
            
            var properties = t.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly ).Where( p => !p.Name.Contains( '.' ) );
            apListResult = null;
            acListResult = null;
            foreach( var p in properties )
            {
                StObjPropertyAttribute stObjAttr = p.GetCustomAttribute<StObjPropertyAttribute>(false);
                if( stObjAttr != null )
                {
                    string nP = String.IsNullOrEmpty( stObjAttr.PropertyName ) ? p.Name : stObjAttr.PropertyName;
                    Type tP = stObjAttr.PropertyType == null ? p.PropertyType : stObjAttr.PropertyType;
                    if( stObjProperties.Find( sp => sp.Name == nP ) != null )
                    {
                        monitor.Error( $"StObj property named '{p.Name}' for '{p.DeclaringType.FullName}' is defined more than once. It should be declared only once." );
                        continue;
                    }
                    stObjProperties.Add( new StObjPropertyInfo( t, stObjAttr.ResolutionSource, nP, tP, p ) );
                    // Continue to detect Ambient properties. Properties that are both Ambient and StObj must be detected.
                }
                AmbientPropertyAttribute ap = p.GetCustomAttribute<AmbientPropertyAttribute>( false );
                IAmbientPropertyOrInjectContractAttribute ac = p.GetCustomAttribute<InjectContractAttribute>( false );
                if( ac != null || ap != null )
                {
                    if( stObjAttr != null || (ac != null && ap != null) )
                    {
                        monitor.Error( $"Property named '{p.Name}' for '{p.DeclaringType.FullName}' can not be both an Ambient Contract, an Ambient Property or a StObj property." );
                        continue;
                    }
                    IAmbientPropertyOrInjectContractAttribute attr = ac ?? ap;
                    string kindName = attr.IsAmbientProperty ? AmbientPropertyInfo.KindName : InjectContractInfo.KindName;

                    var mGet = p.GetGetMethod( true );
                    if( mGet == null || mGet.IsPrivate )
                    {
                        monitor.Error( $"Property '{p.Name}' of '{p.DeclaringType.FullName}' can not be marked as {kindName}. Did you forget to make it protected or public?" );
                        continue;
                    }
                    if( attr.IsAmbientProperty )
                    {
                        if( apListResult == null ) apListResult = new List<AmbientPropertyInfo>();
                        var amb = new AmbientPropertyInfo( p, attr.IsOptionalDefined, attr.IsOptional, ap.IsResolutionSourceDefined, ap.ResolutionSource, definerSpecializationDepth, apListResult.Count );
                        apListResult.Add( amb );
                    }
                    else
                    {
                        if( acListResult == null ) acListResult = new List<InjectContractInfo>();
                        var amb = new InjectContractInfo( p, attr.IsOptionalDefined, attr.IsOptional, definerSpecializationDepth, acListResult.Count );
                        acListResult.Add( amb );
                    }
                }
            }
        }
    }

}
