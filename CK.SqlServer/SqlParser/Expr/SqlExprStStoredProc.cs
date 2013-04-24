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
            : base( Build( alterOrCreate, type, name, parameters, options, asToken, null, bodyStatements, null ), term )
        {
        }

        public SqlExprStStoredProc( SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type, SqlExprMultiIdentifier name, SqlExprParameterList parameters, SqlExprUnmodeledTokens options, SqlTokenIdentifier asToken, SqlTokenIdentifier begin, SqlExprStatementList bodyStatements, SqlTokenIdentifier end, SqlTokenTerminal term )
            : base( Build( alterOrCreate, type, name, parameters, options, asToken, begin, bodyStatements, end ), term )
        {
        }

        internal SqlExprStStoredProc( ISqlItem[] items )
            : base( items )
        {
        }

        static ISqlItem[] Build( SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type, SqlExprMultiIdentifier name, SqlExprParameterList parameters, SqlExprUnmodeledTokens options, SqlTokenIdentifier asToken, SqlTokenIdentifier begin, SqlExprStatementList bodyStatements, SqlTokenIdentifier end )
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

        public SqlTokenIdentifier AlterOrCreate { get { return (SqlTokenIdentifier)Slots[0]; } }

        public SqlTokenIdentifier ObjectType { get { return (SqlTokenIdentifier)Slots[1]; } }

        public SqlExprMultiIdentifier Name { get { return (SqlExprMultiIdentifier)Slots[2]; } }

        public SqlExprParameterList Parameters { get { return (SqlExprParameterList)Slots[3]; } }

        public bool HasOptions { get { return Slots.Length == 9 || Slots.Length == 7; } }

        public SqlExprUnmodeledTokens Options { get { return HasOptions ? (SqlExprUnmodeledTokens)Slots[4] : null; } }

        public SqlTokenIdentifier AsToken { get { return (SqlTokenIdentifier)Slots[HasOptions ? 5 : 4]; } }

        public bool HasBeginEnd { get { return Slots.Length == 8 || Slots.Length == 6; } }

        public SqlTokenIdentifier Begin { get { return HasBeginEnd ? (SqlTokenIdentifier)Slots[Slots.Length - 3] : null; } }

        public SqlExprStatementList BodyStatements { get { return (SqlExprStatementList)Slots[ HasBeginEnd ? Slots.Length - 2 : Slots.Length - 1 ]; } }

        public SqlTokenIdentifier End { get { return HasBeginEnd ? (SqlTokenIdentifier)Slots[ Slots.Length - 1 ] : null; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }
}
