using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public abstract class SqlExprBaseMonoToken<T> : SqlExpr 
        where T : SqlToken 
    {
        protected SqlExprBaseMonoToken( T t )
            : this( CreateArray( SqlToken.EmptyOpenPar, t, SqlToken.EmptyClosePar ) )
        {
        }

        internal SqlExprBaseMonoToken( ISqlItem[] components )
            : base( components )
        {
        }

        public T Token { get { return (T)Slots[1]; } }

    }


}
