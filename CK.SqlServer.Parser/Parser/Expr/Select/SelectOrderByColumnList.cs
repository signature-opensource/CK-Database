using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// List of comma separated <see cref="SelectOrderByColumn"/>
    /// </summary>
    public class SelectOrderByColumnList : SqlNoExprList<SelectOrderByColumn>
    {
        public SelectOrderByColumnList( IList<ISqlItem> components )
            : base( components )
        {
        }

        internal SelectOrderByColumnList( ISqlItem[] items )
            : base( items )
        {
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }
    }


}
