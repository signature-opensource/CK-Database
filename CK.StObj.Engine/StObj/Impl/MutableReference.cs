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
        string _context;
        static protected readonly MutableItem UnresolvedMarker = new MutableItem();

        internal MutableReference( MutableItem owner, StObjMutableReferenceKind kind )
        {
            Owner = owner;
            _kind = kind;
            if( _kind == StObjMutableReferenceKind.Requires 
                || _kind == StObjMutableReferenceKind.Group 
                || _kind == StObjMutableReferenceKind.AmbientContract 
                || (_kind & StObjMutableReferenceKind.Container) != 0 )
            {
                StObjRequirementBehavior = StObjRequirementBehavior.ErrorIfNotStObj;
            }
            else if( _kind == StObjMutableReferenceKind.RequiredBy || _kind == StObjMutableReferenceKind.AmbientProperty )
            {
                StObjRequirementBehavior = StObjRequirementBehavior.None;
            }
            else
            {
                Debug.Assert( (_kind & StObjMutableReferenceKind.ConstructParameter) != 0 );
                StObjRequirementBehavior = StObjRequirementBehavior.WarnIfNotStObj;
            }
        }

        IStObj IStObjReference.Owner => Owner;

        IStObjMutableItem IStObjMutableReference.Owner => Owner;

        public StObjMutableReferenceKind Kind => _kind;

        public StObjRequirementBehavior StObjRequirementBehavior { get; set; }

        /// <summary>
        /// Gets or sets the context for the referenced type. Null to use the <see cref="Owner"/>'s context.
        /// </summary>
        public string Context 
        {
            get { return _context; }
            set { _context = value; }
        }

        /// <summary>
        /// Gets or sets the type of the reference. 
        /// Initialized with the <see cref="System.Reflection.PropertyInfo.PropertyType"/> for Ambient Properties, 
        /// with <see cref="System.Reflection.ParameterInfo.ParameterType"/> for parameters and with provided type 
        /// for other kind of reference (<see cref="StObjMutableReferenceKind.Requires"/>, <see cref="StObjMutableReferenceKind.RequiredBy"/> and <see cref="StObjMutableReferenceKind.Container"/>).
        /// </summary>
        public Type Type { get; set; }

        internal virtual MutableItem ResolveToStObj( IActivityMonitor monitor, StObjCollectorResult collector, StObjCollectorContextualResult cachedCollector )
        {           
            MutableItem result = null;
            if( Type == null || StObjRequirementBehavior == Setup.StObjRequirementBehavior.ExternalReference ) return result;
          
            if( _context != null )
            {
                // Context is not null: search inside this exact context.
                // Even if the context for this reference is the one of our Owner's context, since it is explicitly set,
                // we expect the type to actually be in this context.
                StObjCollectorContextualResult ctxResult = cachedCollector == null || cachedCollector.Context != _context 
                                                                ? collector.FindContext( _context ) 
                                                                : cachedCollector;
                if( ctxResult == null ) 
                {
                    Error( monitor, $"Undefined Typed context '{_context}'" );
                    return null;
                }
                result = ctxResult.InternalMapper.ToHighestImpl( Type );
                if( result == null )
                {
                    WarnOrErrorIfStObjRequired( monitor, false, $"{AmbientContractCollector.FormatContextualFullName( _context, Type )} not found" );
                    return null;
                }
            }
            else
            {
                if( cachedCollector == null || cachedCollector.Context != Owner.Context.Context ) cachedCollector = collector.FindContext( Owner.Context.Context );
                // Context is not set: first look for the type in the Owners's context.
                // If it is not found, look for a single type across the different contexts.
                result = cachedCollector.InternalMapper.ToHighestImpl( Type );
                if( result == null )
                {
                    var all = collector.FindHighestImplFor( Type ).ToList();
                    if( all.Count == 0 )
                    {
                        // Do not when value type or string not found.
                        WarnOrErrorIfStObjRequired(monitor, true, $"{AmbientContractCollector.FormatContextualFullName(_context, Type)} not found");
                        return null;
                    }
                    if( all.Count > 1 )
                    {
                        Error( monitor, $"Type '{Type.FullName}' exists in more than one context: '{String.Join("', '", all.Select(m => m.Context))}'. A context for this relation must be specified" );
                        return null;
                    }
                    result = all[0];
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
                if( !skipWarnOnValueType || !(Type.GetTypeInfo().IsValueType || Type == typeof(string)))
                {
                    Warn(monitor, text);
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
