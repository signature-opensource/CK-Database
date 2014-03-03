using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public class SqlExprStView : SqlExprBaseSt
    {
        public SqlExprStView( SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type, SqlExprMultiIdentifier name, SqlExprColumnList columns, SqlExprUnmodeledItems options, SqlTokenIdentifier asToken, SqlItem select, SqlTokenTerminal term )
            : base( Build( alterOrCreate, type, name, columns, options, asToken, select ), term )
        {
        }

        internal SqlExprStView( ISqlItem[] newComponents )
            : base( newComponents )
        {
        }

        static ISqlItem[] Build( SqlTokenIdentifier alterOrCreate, SqlTokenIdentifier type, SqlExprMultiIdentifier name, SqlExprColumnList columns, SqlExprUnmodeledItems options, SqlTokenIdentifier asToken, SqlItem select )
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

        public SqlTokenIdentifier AlterOrCreateT { get { return (SqlTokenIdentifier)Slots[0]; } }

        public SqlTokenIdentifier ObjectTypeT { get { return (SqlTokenIdentifier)Slots[1]; } }

        public SqlExprMultiIdentifier Name { get { return (SqlExprMultiIdentifier)Slots[2]; } }

        public bool HasOptions { get { return Slots.Length == 6 || (Slots.Length > 4 && Slots[3] is SqlExprUnmodeledItems); } }

        public bool HasColumns { get { return Slots.Length == 6 || (Slots.Length > 4 && Slots[3] is SqlExprColumnList); } }

        public SqlExprColumnList Columns 
        { 
            get { return Slots.Length != 4 ? Slots[3] as SqlExprColumnList : null; } 
        }

        public SqlExprUnmodeledItems Options 
        { 
            get 
            { 
                if( Slots.Length == 4 ) return null;
                if( Slots.Length == 6 ) return (SqlExprUnmodeledItems)Slots[4];
                return Slots[3] as SqlExprUnmodeledItems; 
            } 
        }

        public SqlTokenIdentifier AsT { get { return  (SqlTokenIdentifier)Slots[Slots.Length-2]; } }

        public SqlItem Select { get { return (SqlItem)Slots[Slots.Length - 1]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

    }
}
