using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;
using System;

namespace CK.SqlServer.Setup
{
    public partial class SqlCallableItem<T>
    {
        IFunctionScope ISqlCallableItem.AssumeSourceCommandBuilder( IActivityMonitor monitor, IDynamicAssembly dynamicAssembly )
        {
            if( SqlObject == null ) return null;
            INamespaceScope ns = dynamicAssembly.Code.Global.FindOrCreateNamespace( "SqlGen" );
            ITypeScope tB = ns.FindType( "static class CreatorForSqlCommand" );
            if( tB == null )
            {
                tB = ns.EnsureUsing( "System.Data" )
                       .EnsureUsing( "System.Data.SqlClient" )
                       .CreateType( "static class CreatorForSqlCommand" );
            }
            
            string methodKey = "CreatorForSqlCommand" + '.' + FullName;
            var m = (IFunctionScope)dynamicAssembly.Memory.GetValueWithDefault( methodKey, null );
            if( m == null )
            {
                using( monitor.OpenTrace( $"Low level SqlCommand create method for: '{SqlObject.ToStringSignature( true )}'." ) )
                {
                    try
                    {
                        m = GenerateCreateSqlCommand( tB, FullName, $"Cmd{dynamicAssembly.NextUniqueNumber()}", SqlObject );
                        dynamicAssembly.Memory[methodKey] = m;
                        foreach( var p in SqlObject.Parameters )
                        {
                            if( p.IsPureOutput && p.DefaultValue != null )
                            {
                                monitor.Warn( $"Sql parameter '{p.Name}' is an output parameter but has a default value: if it is used as an input parameter it should be marked as /*input*/output." );
                            }
                        }
                    }
                    catch( Exception ex )
                    {
                        monitor.Error( ex );
                    }
                }
            }
            return m;
        }

        private IFunctionScope GenerateCreateSqlCommand( ITypeScope tB, string fullName, string name, T sqlObject )
        {
            IFunctionScope mB = tB.CreateFunction( t => t.Append( "public static SqlCommand " ).Append( name ).Append( "()" ) );
            mB.Append( $"var cmd = new SqlCommand(" ).Append( sqlObject.SchemaName.ToSourceString() ).Append( ");" ).NewLine()
              .Append( "cmd.CommandType = CommandType.StoredProcedure;" ).NewLine();
            ISqlServerFunctionScalar func = sqlObject as ISqlServerFunctionScalar;
            if( func != null )
            {
                GenerateCreateSqlParameter( mB, "pR", new SqlParameterReturnedValue( func.ReturnType ) );
                mB.Append( "cmd.Parameters.Add( pR ).Direction = ParameterDirection.ReturnValue;" ).NewLine();
            }
            int idxP = 0;
            foreach( ISqlServerParameter p in sqlObject.Parameters )
            {
                var pName = GenerateCreateSqlParameter( mB, $"p{++idxP}", p );
                if( p.IsOutput )
                {
                    mB.Append( pName )
                       .Append( ".Direction = ParameterDirection." )
                       .Append( p.IsInputOutput ? "InputOutput" : "Output" )
                       .Append( ";" )
                       .NewLine();
                }
                mB.Append( "cmd.Parameters.Add(").Append( pName ).Append( ");" ).NewLine();
            }
            mB.Append( "return cmd;" ).NewLine();
            return mB;
        }

        static string GenerateCreateSqlParameter( IFunctionScope b, string name, ISqlServerParameter p )
        {
            int size = p.SqlType.SyntaxSize;
            if( size == 0 ) size = 1;
            b.Append( "var " )
             .Append( name )
             .Append( " = new SqlParameter( " )
             .AppendSourceString( p.Name )
             .Append( ", SqlDbType." )
             .Append( p.SqlType.DbType.ToString() );
            if( size != 0 && size != -2 )
            {
                b.Append( ", " ).Append( size );
            }
            b.Append( ");" ).NewLine();
            var precision = p.SqlType.SyntaxPrecision;
            if( precision != 0 )
            {
                b.Append( name ).Append( ".Precision = " ).Append( precision ).Append( ";" ).NewLine();
                var scale = p.SqlType.SyntaxScale;
                if( scale != 0 )
                {
                    b.Append( name ).Append( ".Scale = " ).Append( scale ).Append( ";" ).NewLine();
                }
            }
            return name;
        }
    }
}
