#if NET451
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Framework
{
    public static class Does
    {
        public static SubstringConstraint Contain( string v )
        {
            return Is.StringContaining( v );
        }

        public static EndsWithConstraint EndWith( string v )
        {
            return Is.StringEnding( v );
        }

        public static EndsWithConstraint EndWith( this ConstraintExpression @this, string v )
        {
            return @this.StringEnding( v );
        }

        public static StartsWithConstraint StartWith( string v )
        {
            return Is.StringStarting( v );
        }

        public static RegexConstraint Match( string v )
        {
            return Is.StringMatching( v );
        }
    }
}
#endif
