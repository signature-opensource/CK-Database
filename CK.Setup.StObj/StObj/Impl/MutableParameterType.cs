using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Describes a parameter of a Construct method.
    /// </summary>
    internal class MutableParameterType : MutableReferenceType, IMutableParameterType
    {
        MutableItem _resolved;

        internal MutableParameterType( MutableItem owner, int index, string name )
            : base( owner, MutableReferenceKind.ConstructParameter )
        {
            Index = index;
            Name = name;
        }

        public int Index { get; private set; }

        public string Name { get; private set; }

        public bool IsOptional { get; set; }

        internal MutableItem Resolved 
        { 
            get { return _resolved; } 
        }

        internal override MutableItem Resolve( IActivityLogger logger, StObjCollectorResult collector, StObjCollectorContextualResult ownerCollector )
        {
            return (_resolved = base.Resolve( logger, collector, ownerCollector) );
        } 

        public override string ToString()
        {
            return String.Format( "Construct parameter '{0}' (n°{1}) for '{2}'", Name, Index, Owner.ToString() );
        }

    }
}
