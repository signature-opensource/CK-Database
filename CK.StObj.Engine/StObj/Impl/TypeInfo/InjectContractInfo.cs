using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Setup
{
    internal class InjectContractInfo : AmbientPropertyOrInjectContractInfo
    {
        public new readonly static string KindName = "[AmbientContract]";
        
        internal InjectContractInfo( PropertyInfo p, bool isOptionalDefined, bool isOptional, int definerSpecializationDepth, int index )
            : base( p, isOptionalDefined, isOptional, definerSpecializationDepth, index )
        {
        }

        public override string Kind 
        { 
            get { return KindName; } 
        }

    }
}
