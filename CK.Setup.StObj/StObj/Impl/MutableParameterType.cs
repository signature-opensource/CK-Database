using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Reflection;

namespace CK.Setup
{
    /// <summary>
    /// Describes a parameter of a Construct method.
    /// </summary>
    internal class MutableParameterType : MutableReferenceType, IMutableParameterType
    {
        ParameterInfo _param;
        MutableItem _resolved;
        static readonly MutableItem _unresolved = new MutableItem( null, AmbiantContractCollector.DefaultContext, AmbiantContractCollector.DefaultContext, AmbiantContractCollector.DefaultContext );

        internal MutableParameterType( MutableItem owner, ParameterInfo param, bool isContainer )
            : base( owner, isContainer ? MutableReferenceKind.ConstructParameter|MutableReferenceKind.Container : MutableReferenceKind.ConstructParameter )
        {
            _param = param;
            _resolved = _unresolved;
            Type = param.ParameterType;
            IsOptional = param.IsOptional;
        }

        public int Index { get { return _param.Position; } }

        public string Name { get { return _param.Name; } }

        public bool IsOptional { get; set; }

        internal MutableItem Resolved 
        { 
            get 
            {
                Debug.Assert( _resolved != _unresolved );
                return _resolved; 
            } 
        }

        internal override MutableItem Resolve( IActivityLogger logger, StObjCollectorResult collector, StObjCollectorContextualResult ownerCollector )
        {
            if( _resolved != _unresolved ) return _resolved;
            if( Type == null && !IsOptional )
            {
                Error( logger, "Type can not be null since the parameter is not optional" );
            }
            return _resolved = base.Resolve( logger, collector, ownerCollector );
        } 

        public override string ToString()
        {
            string s = String.Format( "Construct parameter '{0}' (n°{1}) for '{2}'", Name, Index, Owner.ToString() );
            if( (Kind & MutableReferenceKind.Container) != 0 ) s += " (Container)";
            return s;
        }

    }
}
