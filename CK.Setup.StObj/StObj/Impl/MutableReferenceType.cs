using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    internal class MutableReferenceType : IMutableReferenceType
    {
        readonly MutableItem _owner;
        readonly MutableReferenceKind _kind;

        internal MutableReferenceType( MutableItem owner, MutableReferenceKind kind )
        {
            _owner = owner;
            _kind = kind;
            if( _kind == MutableReferenceKind.Requires || (_kind&MutableReferenceKind.Container) != 0 ) StObjRequirementBehavior = StObjRequirementBehavior.ErrorIfNotStObj;
            else if( _kind == MutableReferenceKind.RequiredBy ) StObjRequirementBehavior = StObjRequirementBehavior.None;
            else
            {
                Debug.Assert( (_kind & MutableReferenceKind.ConstructParameter) != 0 );
                StObjRequirementBehavior = StObjRequirementBehavior.WarnIfNotStObj;
            }
        }

        public IStObjMutableItem Owner { get { return _owner; } }

        public MutableReferenceKind Kind { get { return _kind; } }

        public StObjRequirementBehavior StObjRequirementBehavior { get; set; }

        public Type Context { get; set; }

        public Type Type { get; set; }

        internal virtual MutableItem Resolve( IActivityLogger logger, StObjCollectorResult collector, StObjCollectorContextualResult ownerCollector )
        {
            MutableItem result = null;
            if( Type == null ) return null;

            if( !AmbiantContractCollector.IsAmbiantContract( Type ) )
            {
                WarnOrErrorIfStObjRequired( logger, String.Format( "Type '{0}' is not an Ambiant contract", Type.FullName ) );
                return null;
            }

            if( Context != null )
            {
                // Context is not null: search inside this exact context.
                // Even if the context for this reference is the one of our Owner's context, since it is explicitely set,
                // we expect the type to actually be in this context.
                StObjCollectorContextualResult ctxResult = Context != ownerCollector.Context ? collector[ Context ] : ownerCollector;
                if( ctxResult == null ) 
                {
                    Error( logger, String.Format( "Undefined Typed context '{0}'", Context.Name ) );
                    return null;
                }
                result = ctxResult.Find( Type );
                if( result == null )
                {
                    WarnOrErrorIfStObjRequired( logger, String.Format( "{0} not found", AmbiantContractCollector.DisplayName( Context, Type ) ) );
                    return null;
                }
            }
            else
            {
                // Context is not set: first look for the type in the Owners's context.
                // If it is not foud, look for a single type across the different contexts.
                result = ownerCollector.Find( Type );
                if( result == null )
                {
                    var all = collector.FindMutableItemsFor( Type ).ToList();
                    if( all.Count == 0 )
                    {
                        WarnOrErrorIfStObjRequired( logger, String.Format( "Type '{0}' not found in any context", Type.FullName ) );
                        return null;
                    }
                    if( all.Count > 1 )
                    {
                        Error( logger, String.Format( "Type '{0}' exists in more than one context: '{1}'. A context for this relation must be specified", 
                                                        Type.FullName, 
                                                        String.Join( "', '", all.Select( m => m.Context.Name ) ) ) );
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
            return String.Format( "'{0}' reference for {1}", _kind.ToString(), _owner.ToString() );
        }
    }
}
