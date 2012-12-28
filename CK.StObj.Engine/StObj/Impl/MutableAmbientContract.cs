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
    /// Describes an Ambient Contract property.
    /// </summary>
    internal class MutableAmbientContract : MutableReferenceOptional, IStObjMutableAmbientContract
    {
        internal readonly AmbientContractInfo AmbientContractInfo;

        internal MutableAmbientContract( MutableItem owner, AmbientContractInfo info )
            : base( owner, StObjMutableReferenceKind.AmbientContract )
        {
            AmbientContractInfo = info;
            Type = AmbientContractInfo.PropertyType;
            IsOptional = AmbientContractInfo.IsOptional;
        }

        public override string Name { get { return AmbientContractInfo.Name; } }

        internal override string KindName { get { return "AmbientContract"; } }

        internal override Type UnderlyingType { get { return AmbientContractInfo.PropertyType; } }

        public override string ToString()
        {
            return String.Format( "Ambient Contract '{0}' of '{1}'", Name, Owner.ToString() );
        }

    }
}
