using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    /// <summary>
    /// Wrapper for <see cref="ActualType">actual type</see> information (such as nvarchar(45), decimal(15,4), or datetime).
    /// </summary>
    public class SqlExprTypeDecl : SqlItem
    {
        readonly ISqlExprUnifiedTypeDecl[] _type;

        public SqlExprTypeDecl( ISqlExprUnifiedTypeDecl actualType )
        {
            if( actualType == null ) throw new ArgumentNullException( "actualType" );
            _type = new []{ actualType };
        }

        public override IEnumerable<ISqlItem> Components { get { return _type; } }

        public override IEnumerable<SqlToken> Tokens { get { return _type[0].Tokens; } }

        public override SqlToken FirstOrEmptyToken { get { return _type[0].FirstOrEmptyToken; } }

        public override SqlToken LastOrEmptyToken { get { return _type[0].LastOrEmptyToken; } }

        /// <summary>
        /// Gets a unified type for different kind of type declaration.
        /// </summary>
        public ISqlExprUnifiedTypeDecl ActualType { get { return _type[0]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }

}
