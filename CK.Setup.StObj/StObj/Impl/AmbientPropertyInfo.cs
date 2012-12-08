using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Setup
{
    internal class AmbientPropertyInfo : AmbientPropertyOrContractInfo
    {
        internal AmbientPropertyInfo( PropertyInfo p, bool isOptionalDefined, bool isOptional, int definerSpecializationDepth, int index )
            : base( p, isOptionalDefined, isOptional, definerSpecializationDepth, index )
        {
        }

        /// <summary>
        /// Link to the ambient property above.
        /// </summary>
        public AmbientPropertyInfo Generalization { get; internal set; } 

    }
}
