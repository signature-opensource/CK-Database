using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace SqlCallDemo
{

    [SqlPackage( Schema = "CK", ResourcePath = "Res" ), Versions( "2016.4.6" )]
    public abstract class CharAndNCharPackage : SqlPackage
    {
        [SqlScalarFunction( "fCharFunction" )]
        public abstract char CharFunction( SqlStandardCallContext ctx, char? c );

        [SqlScalarFunction( "fCharFunction" )]
        public abstract Task<char> CharFunctionAsync( SqlStandardCallContext ctx, char? c );


        [SqlScalarFunction( "fNCharFunction" )]
        public abstract char NCharFunction( SqlStandardCallContext ctx, char? c );

        [SqlScalarFunction( "fNCharFunction" )]
        public abstract Task<char> NCharFunctionAsync( SqlStandardCallContext ctx, char? c );


        [SqlProcedure( "sCharProc" )]
        public abstract void CharProc( SqlStandardCallContext ctx, char c1, char? c2, char cN1, char? cN2, out char cO, out char? cNO );

        public class CharProcResult
        {
            public char CO { get; set; }
            public char CNO { get; set; }
        }

        [SqlProcedure( "sCharProc" )]
        public abstract Task<CharProcResult> CharProcAsync( SqlStandardCallContext ctx, char c1, char? c2, char cN1, char? cN2 );

    }
}
