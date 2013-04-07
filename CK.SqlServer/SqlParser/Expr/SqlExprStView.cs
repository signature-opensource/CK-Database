using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprStView : SqlExprBaseSt
    {
        public SqlExprStView( SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type, SqlExprMultiIdentifier name, SqlExprColumnList columns, SqlExprUnmodeledTokens options, SqlTokenIdentifier asToken, SqlExprStUnmodeled select, SqlTokenTerminal term )
            : base( Build( alterOrCreate, type, name, columns, options, asToken, select ), term )
        {
        }

        internal SqlExprStView( IAbstractExpr[] newComponents )
            : base( newComponents )
        {
        }

        static IAbstractExpr[] Build( SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type, SqlExprMultiIdentifier name, SqlExprColumnList columns, SqlExprUnmodeledTokens options, SqlTokenIdentifier asToken, SqlExprStUnmodeled select )
        {
            if( columns == null ) 
            {
                if( options == null ) 
                {
                    return CreateArray( alterOrCreate, type, name, asToken, select );
                }
                else
                {
                    return CreateArray( alterOrCreate, type, name, options, asToken, select );
                }
            }
            else 
            {
                if( options == null ) 
                {
                    return CreateArray( alterOrCreate, type, name, columns, asToken, select );
                }
                else
                {
                    return CreateArray( alterOrCreate, type, name, columns, options, asToken, select );
                }
            }
        }

        public SqlTokenIdentifier AlterOrCreate { get { return (SqlTokenIdentifier)At(0); } }

        public SqlTokenIdentifier ObjectType { get { return (SqlTokenIdentifier)At(1); } }

        public SqlExprMultiIdentifier Name { get { return (SqlExprMultiIdentifier)At(2); } }

        public bool HasOptions { get { return Count == 6 || (Count > 4 && At(3) is SqlExprUnmodeledTokens); } }

        public bool HasColumns { get { return Count == 6 || (Count > 4 && At(3) is SqlExprColumnList); } }

        public SqlExprColumnList Columns 
        { 
            get { return Count != 4 ? At(3) as SqlExprColumnList : null; } 
        }

        public SqlExprUnmodeledTokens Options 
        { 
            get 
            { 
                if( Count == 4 ) return null;
                if( Count == 6 ) return (SqlExprUnmodeledTokens)At(4);
                return At(3) as SqlExprUnmodeledTokens; 
            } 
        }

        public SqlTokenIdentifier AsToken { get { return  (SqlTokenIdentifier)At(Count-2); } }

        public SqlExprStUnmodeled Select { get { return (SqlExprStUnmodeled)At(Count-1); } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }
}
