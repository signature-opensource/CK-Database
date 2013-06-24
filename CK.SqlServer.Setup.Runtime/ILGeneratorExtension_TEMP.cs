using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using CK.Reflection;

namespace CK.SqlServer.Setup
{
    static class ILGeneratorExtension_TEMP
    {
        /// <summary>
        /// Emits code that sets the parameter (that must be a 'ref' or 'out' parameter) to the default of its type.
        /// Handles static or instance methods and value or reference type.
        /// </summary>
        /// <param name="g">This <see cref="ILGenerator"/> object.</param>
        /// <param name="byRefParameter">The 'by ref' parameter.</param>
        public static void StoreDefaultValueForOutParameter( this ILGenerator g, ParameterInfo byRefParameter )
        {
            if( !byRefParameter.ParameterType.IsByRef ) throw new ArgumentException( "Parameter must be 'by ref'.", "byRefParameter" );
            Type pType = byRefParameter.ParameterType.GetElementType();
            // Adds 1 to skip 'this' parameter ?
            MethodBase m = (MethodBase)byRefParameter.Member;
            if( (m.CallingConvention & CallingConventions.HasThis) != 0 ) g.LdArg( byRefParameter.Position + 1 );
            else g.LdArg( byRefParameter.Position );
            if( pType.IsValueType )
            {
                g.Emit( OpCodes.Initobj, pType );
            }
            else
            {
                g.Emit( OpCodes.Ldnull );
                g.Emit( OpCodes.Stind_Ref );
            }
        }

        /// <summary>
        /// Emits a <see cref="LdArg"/> with an optional <see cref="OpCodes.Box"/> if <paramref name="parameterType"/> is 
        /// a value type or a generic parameter.
        /// </summary>
        /// <param name="g">This <see cref="ILGenerator"/> object.</param>
        /// <param name="idxParameter">Index of the parameter to load on the stack.</param>
        /// <param name="parameterType">Type of the parameter.</param>
        public static void LdArgBox( this ILGenerator g, int idxParameter, Type parameterType )
        {
            g.LdArg( idxParameter );
            if( parameterType.IsGenericParameter || parameterType.IsValueType )
            {
                g.Emit( OpCodes.Box, parameterType );
            }
        }

        /// <summary>
        /// Emits a <see cref="LdArg"/> with an optional <see cref="OpCodes.Box"/> if <paramref name="parameter"/>'s type is 
        /// a value type or a generic parameter.
        /// </summary>
        /// <param name="g">This <see cref="ILGenerator"/> object.</param>
        /// <param name="parameter">Parameter of the current method.</param>
        public static void LdArgBox( this ILGenerator g, ParameterInfo p )
        {
            int iP = p.Position;
            MethodBase m = (MethodBase)p.Member;
            if( (m.CallingConvention & CallingConventions.HasThis) != 0 ) ++iP;
            LdArgBox( g, iP, p.ParameterType );
        }

    }
}
