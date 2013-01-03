using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    internal class MutableReference : IStObjMutableReference
    {
        /// <summary>
        /// Owner of the reference corresponds to the exact type of the object that has the Construct method for parameters.
        /// For Ambient Properties, the Owner is the Specialization.
        /// This is because a property has de facto more than one Owner when masking is used (note that handling of mask and covariance type checking is done
        /// by StObjTypeInfo: StObjTypeInfo.AmbientProperties already contains a merged information).
        /// </summary>
        internal readonly MutableItem Owner;
        readonly StObjMutableReferenceKind _kind;
        string _context;
        static protected readonly MutableItem UnresolvedMarker = new MutableItem();

        internal MutableReference( MutableItem owner, StObjMutableReferenceKind kind )
        {
            Owner = owner;
            _kind = kind;
            if( _kind == StObjMutableReferenceKind.Requires || _kind == StObjMutableReferenceKind.Group || _kind == StObjMutableReferenceKind.AmbientContract || (_kind & StObjMutableReferenceKind.Container) != 0 )
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

        IStObjMutableItem IStObjReference.Owner { get { return Owner; } }

        public StObjMutableReferenceKind Kind { get { return _kind; } }

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

        internal virtual MutableItem ResolveToStObj( IActivityLogger logger, StObjCollectorResult collector, StObjCollectorContextualResult cachedCollector )
        {           
            MutableItem result = null;
            if( Type == null || StObjRequirementBehavior == Setup.StObjRequirementBehavior.ExternalReference ) return result;
          
            if( _context != null )
            {
                // Context is not null: search inside this exact context.
                // Even if the context for this reference is the one of our Owner's context, since it is explicitely set,
                // we expect the type to actually be in this context.
                StObjCollectorContextualResult ctxResult = cachedCollector == null || cachedCollector.Context != _context ? collector.FindContext( _context ) : cachedCollector;
                if( ctxResult == null ) 
                {
                    Error( logger, String.Format( "Undefined Typed context '{0}'", _context ) );
                    return null;
                }
                result = ctxResult.InternalMapper.ToHighestImpl( Type );
                if( result == null )
                {
                    WarnOrErrorIfStObjRequired( logger, String.Format( "{0} not found", AmbientContractCollector.FormatContextualFullName( _context, Type ) ) );
                    return null;
                }
            }
            else
            {
                if( cachedCollector == null || cachedCollector.Context != Owner.Context.Context ) cachedCollector = collector.FindContext( Owner.Context.Context );
                // Context is not set: first look for the type in the Owners's context.
                // If it is not foud, look for a single type across the different contexts.
                result = cachedCollector.InternalMapper.ToHighestImpl( Type );
                if( result == null )
                {
                    var all = collector.FindHighestImplFor( Type ).ToList();
                    if( all.Count == 0 )
                    {
                        // Do not use WarnOrErrorIfStObjRequired since we want to handle optional value type or string not found without any warning.
                        if( StObjRequirementBehavior == Setup.StObjRequirementBehavior.ErrorIfNotStObj )
                        {
                            Error( logger, String.Format( "Type '{0}' not found in any context", Type.FullName ) );
                        }
                        else if( StObjRequirementBehavior == Setup.StObjRequirementBehavior.WarnIfNotStObj )
                        {
                            if( !Type.IsValueType && Type != typeof(string) ) Warn( logger, String.Format( "Type '{0}' not found in any context", Type.FullName ) );
                        }
                        return null;
                    }
                    if( all.Count > 1 )
                    {
                        Error( logger, String.Format( "Type '{0}' exists in more than one context: '{1}'. A context for this relation must be specified", 
                                                        Type.FullName, 
                                                        String.Join( "', '", all.Select( m => m.Context ) ) ) );
                        return null;
                    }
                    result = all[0];
                }
            }
            return result;
        }

        private void WarnOrErrorIfStObjRequired( IActivityLogger logger, string text )
        {
            if( StObjRequirementBehavior == Setup.StObjRequirementBehavior.ErrorIfNotStObj )
            {
                Error( logger, text );
            }
            else if( StObjRequirementBehavior == Setup.StObjRequirementBehavior.WarnIfNotStObj )
            {
                Warn( logger, text );
            }
        }

        protected void Warn( IActivityLogger logger, string text )
        {
            logger.Warn( "{0}: {1}.", ToString(), text );
        }

        protected void Error( IActivityLogger logger, string text )
        {
            logger.Error( "{0}: {1}.", ToString(), text );
        }

        public override string ToString()
        {
            return String.Format( "{0} reference for '{1}'", _kind.ToString(), Owner.ToString() );
        }
    }
}
