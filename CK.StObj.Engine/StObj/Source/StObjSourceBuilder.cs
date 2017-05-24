//using System;
//using System.Collections.Generic;
//using System.Text;
//using CK.CodeGen;

//namespace CK.Setup
//{
//    class StObjSourceBuilder
//    {

//        public static string GenerateContextSource( StringBuilder b, StObjCollectorResult result )
//        {
//            foreach( var ctx in result.Contexts )
//            {
//                b.AppendLine( $"_context = {ctx.Context.ToSourceString()};" );
//            }
//        }
//    }
//}
