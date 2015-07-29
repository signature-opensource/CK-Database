#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprStatementList.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
    /// List of <see cref="SqlExprBaseSt">statements</see>. 
    /// It is not a statement itself: the <see cref="SqlExprStBlock"/> is the composite statement (begin...end).
    /// </summary>
    public class SqlExprStatementList : SqlItem, IReadOnlyList<SqlExprBaseSt>
    {
        readonly SqlExprBaseSt[] _statements;

        public SqlExprStatementList( IEnumerable<SqlExprBaseSt> statements )
        {
            _statements = statements.ToArray();
        }

        internal SqlExprStatementList( SqlExprBaseSt[] newStatements )
        {
            _statements = newStatements;
        }

        public override sealed IEnumerable<ISqlItem> Items
        {
            get { return _statements; }
        }

        public override SqlToken FirstOrEmptyT { get { return _statements[0].FirstOrEmptyT; } }

        public override SqlToken LastOrEmptyT { get { return _statements[_statements.Length-1].LastOrEmptyT; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public SqlExprBaseSt this[int index]
        {
            get { return _statements[index]; }
        }


        public int Count
        {
            get { return _statements.Length; }
        }

        public IEnumerator<SqlExprBaseSt> GetEnumerator()
        {
            return (IEnumerator<SqlExprBaseSt>)_statements.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _statements.GetEnumerator();
        }

    }


}
