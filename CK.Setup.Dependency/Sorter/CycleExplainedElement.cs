using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface ICycleExplainedElement
    {
        char Relation { get; }

        IDependentItem Item { get; }
    }

    public class CycleExplainedElement : ICycleExplainedElement
    {
        public readonly static char Start                   = '↳';  // Unicode: \u21B3
        public readonly static char ElementOf               = '∈';  // Unicode: \u2208
        public readonly static char Contains                = '∋';  // Unicode: \u220B
        public readonly static char Requires                = '⇀';  // Unicode: \u21C0
        public readonly static char RequiredBy              = '↽';  // Unicode: \u21BD
        public readonly static char RequiredByRequires      = '⇌';  // Unicode: \u21CC
        public readonly static char ElementOfContainer      = '⊏';  // Unicode: \u228F
        public readonly static char ContainerContains       = '⊐';  // Unicode: \u2290
        public readonly static char GeneralizedBy           = '↟';  // ↟ Unicode: \u219F

        internal char Relation;
        internal readonly DependencySorter.Entry Item;

        internal CycleExplainedElement( char r, DependencySorter.Entry i )
        {
            Relation = r;
            Item = i;
        }

        public override string ToString()
        {
            return String.Format( "{0} {1}", Relation, Item.FullName );
        }

        char ICycleExplainedElement.Relation { get { return Relation; } }

        IDependentItem ICycleExplainedElement.Item { get { return Item.Item; } }
    }

}
