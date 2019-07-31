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
    public abstract class FunctionPackage : SqlPackage
    {
        [SqlScalarFunction( "fStringFunction" )]
        public abstract string StringFunction( SqlStandardCallContext ctx, int? v );

        [SqlScalarFunction( "fStringFunction" )]
        public abstract Task<string> StringFunctionAsync( SqlStandardCallContext ctx, int? v );

        [SqlScalarFunction( "fByteFunction" )]
        public abstract byte ByteFunction( SqlStandardCallContext ctx, int v );

        [SqlScalarFunction( "fByteFunction" )]
        public abstract Task<byte> ByteFunctionAsync( SqlStandardCallContext ctx, int v );

        [SqlScalarFunction( "fByteFunction" )]
        public abstract byte? NullableByteFunction( SqlStandardCallContext ctx, int v = -1 );

        [SqlScalarFunction( "fByteFunction" )]
        public abstract Task<byte?> NullableByteFunctionAsync( SqlStandardCallContext ctx, int v = -1 );


        public enum Power
        {
            None = 0,
            Min = 1,
            Med = 2,
            Max = 3,
            Overheat = 4
        }

        public enum BPower : byte
        {
            Zero = 0,
            One = 1,
            Two = 2
        }

        [SqlProcedure( "sWithEnumIO" )]
        public abstract Power ProcWithEnumIO( ISqlCallContext ctx, BPower bytePower, Power power );

        [SqlProcedure( "sWithEnumIO" )]
        public abstract Task<Power> ProcWithEnumIOAsync( ISqlCallContext ctx, BPower bytePower, Power power );

        [SqlProcedure( "sWithEnumIO" )]
        public abstract Power? ProcWithNullableEnumIO( ISqlCallContext ctx, BPower? bytePower, Power? power );

        [SqlProcedure( "sWithEnumIO" )]
        public abstract Task<Power?> ProcWithNullableEnumIOAsync( ISqlCallContext ctx, BPower? bytePower, Power? power );

        [SqlProcedure( "sWithEnumIO" )]
        public abstract void ProcWithNullableEnumIOByRef( ISqlCallContext ctx, BPower? bytePower, ref Power? power );

    }
}
