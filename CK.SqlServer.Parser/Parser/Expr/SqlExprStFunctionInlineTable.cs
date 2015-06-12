#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Parser\Parser\Expr\SqlExprStFunctionScalar.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.SqlServer.Parser
{
    public class SqlExprStFunctionInlineTable : SqlExprStFunction
    {
        public SqlExprStFunctionInlineTable( 
            SqlTokenIdentifier alterOrCreate, 
            SqlTokenIdentifier type, 
            SqlExprMultiIdentifier name, 
            SqlExprParameterList parameters,
            SqlTokenIdentifier returns,
            SqlTokenIdentifier table,
            SqlExprUnmodeledItems options,
            SqlTokenIdentifier asToken,
            SqlTokenIdentifier returnToken,
            SelectSpecification query, 
            SqlTokenTerminal term )
            : base( Build( alterOrCreate, type, name, parameters, returns, table, options, asToken, returnToken, query ), term )
        {
        }

        internal SqlExprStFunctionInlineTable( ISqlItem[] items )
            : base( items )
        {
        }


        static ISqlItem[] Build(
            SqlTokenIdentifier alterOrCreate,
            SqlTokenIdentifier type,
            SqlExprMultiIdentifier name,
            SqlExprParameterList parameters,
            SqlTokenIdentifier returns,
            SqlTokenIdentifier table,
            SqlExprUnmodeledItems options,
            SqlTokenIdentifier asToken,
            SqlTokenIdentifier returnToken,
            SelectSpecification query )
        {
            if( options != null )
            {
                if( asToken != null )
                {
                    return CreateArray( alterOrCreate, type, name, parameters, returns, table, options, asToken, returnToken, query );
                }
                else
                {
                    return CreateArray( alterOrCreate, type, name, parameters, returns, table, options, returnToken, query );
                }
            }
            else
            {
                if( asToken != null )
                {
                    return CreateArray( alterOrCreate, type, name, parameters, returns, table, asToken, returnToken, query );
                }
                else
                {
                    return CreateArray( alterOrCreate, type, name, parameters, returns, table, returnToken, query );
                }
            }
        }

        public SqlExprMultiIdentifier Name { get { return (SqlExprMultiIdentifier)Slots[2]; } }

        public SqlExprParameterList Parameters { get { return (SqlExprParameterList)Slots[3]; } }

        public SelectSpecification Select { get { return (SelectSpecification)Slots[SlotsLengthWithoutTerminator - 1]; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( ISqlItemVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }


    }
}
