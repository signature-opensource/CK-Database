using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    /// <summary>
    /// Captures the "Union all | Except | Intersect" between select specification.
    /// This is the separator of <see cref="SqlExprSelectQuery"/> that is a list of <see cref="SqlExprSelectSpec"/>
    /// </summary>
    public class SelectSpecUnion : SqlExprAbstract
    {
        readonly IAbstractExpr[] _components;

        public SelectSpecUnion( SqlTokenIdentifier unionOrExceptOrIntersect, SqlTokenIdentifier all = null )
        {
            if( all != null )
            {
                if( !unionOrExceptOrIntersect.NameEquals( "union" ) ) throw new ArgumentException( "ALL applies only to UNION.", "all" );
                _components = CreateArray( unionOrExceptOrIntersect, all );
            }
            else _components = CreateArray( unionOrExceptOrIntersect );
        }

        public override IEnumerable<IAbstractExpr> Components
        {
            get { return _components; }
        }
    }


}
