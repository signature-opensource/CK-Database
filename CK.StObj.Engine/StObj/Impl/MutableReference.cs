#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\Impl\MutableReference.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Reflection;

namespace CK.Setup
{
    internal class MutableReference : IStObjMutableReference
    {
        /// <summary>
        /// Owner of the reference corresponds to the exact type of the object that has the StObjConstruct method
        /// for parameters.
        /// For Ambient Properties, the Owner is the Specialization.
        /// This is because a property has de facto more than one Owner when masking is used (note that handling of mask
        /// and covariance type checking is done by StObjTypeInfo: StObjTypeInfo.AmbientProperties already contains a
        /// merged information).
        /// </summary>
        internal readonly MutableItem Owner;
        readonly StObjMutableReferenceKind _kind;

        static protected readonly MutableItem UnresolvedMarker = new MutableItem();

        internal MutableReference( MutableItem owner, StObjMutableReferenceKind kind )
        {
            Owner = owner;
            _kind = kind;
            if( _kind == StObjMutableReferenceKind.Requires 
                || _kind == StObjMutableReferenceKind.Group 
                || _kind == StObjMutableReferenceKind.AmbientObject 
                || (_kind & StObjMutableReferenceKind.Container) != 0 )
            {
                StObjRequirementBehavior = StObjRequirementBehavior.ErrorIfNotStObj;
            }
            else if( _kind == StObjMutableReferenceKind.RequiredBy
                     || _kind == StObjMutableReferenceKind.AmbientProperty
                     || _kind == StObjMutableReferenceKind.SingletonReference )
            {
                StObjRequirementBehavior = StObjRequirementBehavior.None;
            }
            else
            {
                Debug.Assert( (_kind & StObjMutableReferenceKind.ConstructParameter) != 0 );
                StObjRequirementBehavior = StObjRequirementBehavior.None;
            }
        }

        IStObj IStObjReference.Owner => Owner;

        IStObjMutableItem IStObjMutableReference.Owner => Owner;

        public StObjMutableReferenceKind Kind => _kind;

        public StObjRequirementBehavior StObjRequirementBehavior { get; set; }

        /// <summary>
        /// Gets or sets the type of the reference. 
        /// Initialized with the <see cref="System.Reflection.PropertyInfo.PropertyType"/> for Ambient Properties, 
        /// with <see cref="System.Reflection.ParameterInfo.ParameterType"/> for parameters and with provided type 
        /// for other kind of reference (<see cref="StObjMutableReferenceKind.Requires"/>, <see cref="StObjMutableReferenceKind.RequiredBy"/> and <see cref="StObjMutableReferenceKind.Container"/>).
        /// </summary>
        public Type Type { get; set; }

        internal virtual MutableItem ResolveToStObj( IActivityMonitor monitor, StObjObjectEngineMap collector )
        {
            MutableItem result = null;
            if( Type != null && StObjRequirementBehavior != StObjRequirementBehavior.ExternalReference )
            {
                result = collector.ToHighestImpl( Type );
                if( result == null )
                {
                    if( _kind == StObjMutableReferenceKind.SingletonReference
                        || _kind == StObjMutableReferenceKind.ConstructParameter )
                    {
                        if( !collector.DefineAsSingletonReference( monitor, Type ) )
                        {
                            Error( monitor, $"{Type.FullName} cannot be referenced since it is not a singleton." );
                        }
                    }
                    else
                    {
                        // No warn on value type or string not found.
                        WarnOrErrorIfStObjRequired( monitor, skipWarnOnValueType: true, text: $"{Type.FullName} not found" );
                    }
                }
            }
            return result;
        }

        protected virtual void WarnOrErrorIfStObjRequired( IActivityMonitor monitor, bool skipWarnOnValueType, string text )
        {
            if( StObjRequirementBehavior == Setup.StObjRequirementBehavior.ErrorIfNotStObj )
            {
                Error( monitor, text );
            }
            else if( StObjRequirementBehavior == Setup.StObjRequirementBehavior.WarnIfNotStObj )
            {
                if( !skipWarnOnValueType || !(Type.IsValueType || Type == typeof( string )) )
                {
                    Warn( monitor, text );
                }
            }
        }

        protected void Warn( IActivityMonitor monitor, string text )
        {
            monitor.Warn( $"{ToString()}: {text}." );
        }

        protected void Error( IActivityMonitor monitor, string text )
        {
            monitor.Error( $"{ToString()}: {text}." );
        }

        public override string ToString() => $"{_kind.ToString()} reference for '{Owner}'";
    }
}
