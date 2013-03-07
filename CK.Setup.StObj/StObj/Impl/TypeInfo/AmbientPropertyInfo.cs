using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CK.Core;

namespace CK.Setup
{
    internal class AmbientPropertyInfo : AmbientPropertyOrContractInfo
    {
        public new readonly static string KindName = "[AmbientProperty]";

        internal AmbientPropertyInfo( PropertyInfo p, bool isOptionalDefined, bool isOptional, int definerSpecializationDepth, int index )
            : base( p, isOptionalDefined, isOptional, definerSpecializationDepth, index )
        {
        }

        /// <summary>
        /// Link to the ambient property above.
        /// </summary>
        public AmbientPropertyInfo Generalization { get; private set; }

        protected override void SetGeneralizationInfo( IActivityLogger logger, CovariantPropertyInfo g )
        {
            base.SetGeneralizationInfo( logger, g );

            AmbientPropertyInfo gen = (AmbientPropertyInfo)g;
            // Captures the Generalization.
            // We keep the fact that this property overrides one above (errors have been logged if conflict/incoherency occur).
            // We can keep the Generalization but not a reference to the specialization since we are 
            // not Contextualized here, but only on a pure Type level.
            Generalization = gen;
        }

        public override string Kind 
        { 
            get { return KindName; } 
        }
    }
}
