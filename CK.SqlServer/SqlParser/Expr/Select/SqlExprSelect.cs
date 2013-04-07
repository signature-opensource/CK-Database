//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Globalization;
//using System.Linq;
//using System.Text;
//using CK.Core;

//namespace CK.SqlServer
//{

//    public class SqlExprSelect : SqlExpr
//    {
//        readonly SelectHeader _header;
//        readonly SelectColumnList _columns;
//        readonly IReadOnlyList<IAbstractExpr> _remainder;

//        public SqlExprSelect( SqlTokenIdentifier select, IEnumerable<SqlExpr> expressions )
//        {
//            _select = select;
//        }

//        protected override IEnumerable<SqlToken> GetStatementTokens()
//        {
//            return Flatten( _expr );
//        }

//        [DebuggerStepThrough]
//        internal protected override T Accept<T>( IExprVisitor<T> visitor )
//        {
//            return visitor.Visit( this );
//        }

//    }


//}
