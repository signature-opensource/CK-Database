using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Reflection.Emit;
using CK.Reflection;

namespace CK.SqlServer.Setup
{
    public partial class SqlProcedureAttributeImpl 
    {

        private void GenerateCreateSqlCommand( MethodInfo m, TypeBuilder tB, bool isVirtual, SqlExprParameterList sqlParameters )
        {
            MethodAttributes mA = m.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.VtableLayoutMask);
            if( isVirtual ) mA |= MethodAttributes.Virtual;
            MethodBuilder mB = tB.DefineMethod( m.Name, mA, m.ReturnType, Type.EmptyTypes );
            ILGenerator g = mB.GetILGenerator();

            Type tSqlCommand = typeof( SqlCommand );
            Type tParameterCollection = typeof( SqlParameterCollection );
            Type tSqlParameter = typeof( SqlParameter );

            LocalBuilder locCmd = g.DeclareLocal( tSqlCommand );
            LocalBuilder locParams = g.DeclareLocal( tParameterCollection );
            LocalBuilder locOneParam = g.DeclareLocal( tSqlParameter );

            g.Emit( OpCodes.Ldstr, _item.SchemaName );
            g.Emit( OpCodes.Newobj, tSqlCommand.GetConstructor( new Type[] { typeof( string ) } ) );
            g.StLoc( locCmd );
            g.LdLoc( locCmd );
            g.LdInt32( (int)CommandType.StoredProcedure );

            g.Emit( OpCodes.Callvirt, tSqlCommand.GetProperty( "CommandType" ).GetSetMethod() );
            g.LdLoc( locCmd );
            g.Emit( OpCodes.Callvirt, tSqlCommand.GetProperty( "Parameters", tParameterCollection ).GetGetMethod() );
            g.StLoc( locParams );
            
            var sqlParameterCtor2 = tSqlParameter.GetConstructor( new Type[] { typeof( string ), typeof( SqlDbType ) } );
            var sqlParameterCtor3 = tSqlParameter.GetConstructor( new Type[] { typeof( string ), typeof( SqlDbType ), typeof( Int32 ) } );
            var addParameter = tParameterCollection.GetMethod( "Add", new Type[] { tSqlParameter } );
            var setParamDirection = tSqlParameter.GetProperty( "Direction" ).GetSetMethod();

            foreach( SqlExprParameter p in sqlParameters )
            {
                g.Emit( OpCodes.Ldstr, p.Variable.Identifier.Name );
                g.LdInt32( (int)p.Variable.TypeDecl.ActualType.DbType );
                int size = p.Variable.TypeDecl.ActualType.SyntaxSize;
                if( size != 0 && size != -2 )
                {
                    g.LdInt32( size );
                    g.Emit( OpCodes.Newobj, sqlParameterCtor3 );
                }
                else
                {
                    g.Emit( OpCodes.Newobj, sqlParameterCtor2 );
                }
                g.StLoc( locOneParam );
                if( p.IsOutput )
                {
                    g.LdLoc( locOneParam );
                    ParameterDirection dir = p.IsInputOutput ? ParameterDirection.InputOutput : ParameterDirection.Output;
                    g.LdInt32( (int)dir );
                    g.Emit( OpCodes.Callvirt, setParamDirection );
                }
                g.LdLoc( locParams );
                g.LdLoc( locOneParam );
                g.Emit( OpCodes.Callvirt, addParameter );
            }
            g.StLoc( locCmd );
            g.Emit( OpCodes.Ret );
        }


    }
}
