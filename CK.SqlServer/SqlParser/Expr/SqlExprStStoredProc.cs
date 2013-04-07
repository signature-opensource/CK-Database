using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer
{
    public class SqlExprStStoredProc : SqlExprBaseSt
    {
        public SqlExprStStoredProc( SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type, SqlExprMultiIdentifier name, SqlExprParameterList parameters, SqlExprUnmodeledTokens options, SqlTokenIdentifier asToken, SqlExprStatementList bodyStatements, SqlTokenTerminal term )
            : base( BuildComponents( alterOrCreate, type, name, parameters, options, asToken, null, bodyStatements, null ), term )
        {
        }

        public SqlExprStStoredProc( SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type, SqlExprMultiIdentifier name, SqlExprParameterList parameters, SqlExprUnmodeledTokens options, SqlTokenIdentifier asToken, SqlTokenIdentifier begin, SqlExprStatementList bodyStatements, SqlTokenIdentifier end, SqlTokenTerminal term )
            : base( BuildComponents( alterOrCreate, type, name, parameters, options, asToken, begin, bodyStatements, end ), term )
        {
        }

        internal SqlExprStStoredProc( SqlExprStStoredProc e, IAbstractExpr[] newComponents )
            : base( CreateArray( newComponents ), e.StatementTerminator )
        {
        }

        static IAbstractExpr[] BuildComponents( SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type, SqlExprMultiIdentifier name, SqlExprParameterList parameters, SqlExprUnmodeledTokens options, SqlTokenIdentifier asToken, SqlTokenIdentifier begin, SqlExprStatementList bodyStatements, SqlTokenIdentifier end )
        {
            if( options != null )
            {
                if( begin != null )
                {
                    if( end == null ) throw new ArgumentNullException( "end can not be null if begin exists." );
                    return CreateArray( alterOrCreate, type, name, parameters, options, asToken, begin, bodyStatements, end );
                }
                else
                {
                    return CreateArray( alterOrCreate, type, name, parameters, options, asToken, bodyStatements );
                }
            }
            else
            {
                if( begin != null )
                {
                    if( end == null ) throw new ArgumentNullException( "end can not be null if begin exists." );
                    return CreateArray( alterOrCreate, type, name, parameters, asToken, begin, bodyStatements, end );
                }
                else
                {
                    return CreateArray( alterOrCreate, type, name, parameters, asToken, bodyStatements );
                }
            }
        }

        public SqlTokenIdentifier AlterOrCreate { get { return (SqlTokenIdentifier)At(0); } }

        public SqlTokenIdentifier ObjectType { get { return (SqlTokenIdentifier)At(1); } }

        public SqlExprMultiIdentifier Name { get { return (SqlExprMultiIdentifier)At(2); } }

        public SqlExprParameterList Parameters { get { return (SqlExprParameterList)At(3); } }

        public bool HasOptions { get { return Count == 9 || Count == 7; } }

        public SqlExprUnmodeledTokens Options { get { return HasOptions ? (SqlExprUnmodeledTokens)At(4) : null; } }

        public SqlTokenIdentifier AsToken { get { return (SqlTokenIdentifier)At(HasOptions ? 5 : 4); } }

        public bool HasBeginEnd { get { return Count == 8 || Count == 6; } }

        public SqlTokenIdentifier Begin { get { return HasBeginEnd ? (SqlTokenIdentifier)At(Count - 3) : null; } }

        public SqlExprStatementList BodyStatements { get { return (SqlExprStatementList)At( HasBeginEnd ? Count - 2 : Count - 1 ); } }

        public SqlTokenIdentifier End { get { return HasBeginEnd ? (SqlTokenIdentifier)At( Count - 1 ) : null; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }
}
