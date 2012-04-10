using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class CycleExplainedElement
    {
        public readonly static char Start = '↳';
        public readonly static char Contains = '∈';
        public readonly static char ContainedBy = '∋';
        public readonly static char Requires = '⇒';
        public readonly static char RequiredByRequires = '⇆';

        internal CycleExplainedElement( char r, IDependentItem i )
        {
            Relation = r;
            Item = i;
        }

        public readonly char Relation;
        public readonly IDependentItem Item;

        public override string ToString()
        {
            return String.Format( "{0} {1}", Relation, Item.FullName );
        }
    }

}
