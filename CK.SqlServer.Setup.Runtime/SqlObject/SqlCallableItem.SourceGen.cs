using CK.CodeGen;
using CK.Core;
using CK.SqlServer.Parser;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace CK.SqlServer.Setup
{
    public partial class SqlCallableItem<T>
    {
        MethodBuilder ISqlCallableItem.AssumeSourceCommandBuilder(IActivityMonitor monitor, IDynamicAssembly dynamicAssembly)
        {
            if (SqlObject == null) return null;
            ClassBuilder tB = (ClassBuilder)dynamicAssembly.Memory["CreatorForSqlCommand"];
            if (tB == null)
            {
                if( !dynamicAssembly.SourceBuilder.Usings.Contains( "System.Data" ) ) dynamicAssembly.SourceBuilder.Usings.Add( "System.Data" );
                if( !dynamicAssembly.SourceBuilder.Usings.Contains( "System.Data.SqlClient" ) ) dynamicAssembly.SourceBuilder.Usings.Add( "System.Data.SqlClient" );

                tB = dynamicAssembly.SourceBuilder.DefineClass("CreatorForSqlCommand");
                tB.FrontModifiers.Build().Add("static");
                dynamicAssembly.Memory.Add("CreatorForSqlCommand", tB);
            }
            string methodKey = "CreatorForSqlCommand" + '.' + FullName;
            var m = (MethodBuilder)dynamicAssembly.Memory[methodKey];
            if (m == null)
            {
                using (monitor.OpenTrace().Send("Low level SqlCommand create method for: '{0}'.", SqlObject.ToStringSignature(true)))
                {
                    try
                    {
                        m = GenerateCreateSqlCommand(tB, FullName, $"Cmd{dynamicAssembly.NextUniqueNumber()}", SqlObject);
                        dynamicAssembly.Memory[methodKey] = m;
                        foreach (var p in SqlObject.Parameters)
                        {
                            if (p.IsPureOutput && p.DefaultValue != null)
                            {
                                monitor.Warn().Send($"Sql parameter '{p.Name}' is an output parameter but has a default value: if it is used as an input parameter it should be marked as /*input*/output.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        monitor.Error().Send(ex);
                    }
                }
            }
            return m;
        }

        private MethodBuilder GenerateCreateSqlCommand(ClassBuilder tB, string fullName, string name, T sqlObject)
        {
            MethodBuilder mB = tB.DefineMethod("public static", name);
            mB.ReturnType = "SqlCommand";
            mB.Body
                .AppendLine( $"var cmd = new SqlCommand({sqlObject.SchemaName.ToSourceString()});" )
                .AppendLine( "cmd.CommandType = CommandType.StoredProcedure;" );
            ISqlServerFunctionScalar func = sqlObject as ISqlServerFunctionScalar;
            if (func != null)
            {
                GenerateCreateSqlParameter(mB.Body, "pR", new SqlParameterReturnedValue(func.ReturnType));
                mB.Body.AppendLine( "cmd.Parameters.Add( pR ).Direction = ParameterDirection.ReturnValue;" );
            }
            int idxP = 0;
            foreach (ISqlServerParameter p in sqlObject.Parameters)
            {
                var pName = GenerateCreateSqlParameter(mB.Body, $"p{++idxP}", p);
                if (p.IsOutput)
                {
                    mB.Body.AppendLine($"{pName}.Direction = ParameterDirection.{(p.IsInputOutput ? ParameterDirection.InputOutput : ParameterDirection.Output)};");
                }
                mB.Body.AppendLine($"cmd.Parameters.Add({pName});");
            }
            mB.Body.AppendLine("return cmd;");
            return mB;
        }

        static string GenerateCreateSqlParameter(StringBuilder b, string name, ISqlServerParameter p)
        {
            int size = p.SqlType.SyntaxSize;
            b.Append($"var {name} = new SqlParameter( {p.Name.ToSourceString()}, SqlDbType.{p.SqlType.DbType}");
            if (size != 0 && size != -2)
            {
                b.Append($", {size}");
            }
            b.AppendLine(");");
            var precision = p.SqlType.SyntaxPrecision;
            if (precision != 0)
            {
                b.AppendLine($"{name}.Precision = {precision};");
                var scale = p.SqlType.SyntaxScale;
                if (scale != 0)
                {
                    b.AppendLine($"{name}.Scale = {scale};");
                }
            }
            return name;
        }
    }
}
