using CK.CodeGen;
using CK.Core;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup.Engine
{
    /// <summary>
    /// Implements IPoco registration for CK.Dapper.
    /// This is triggered by the static extension class of CK.SqlServer.Dapper.
    /// </summary>
    public sealed class DapperIPocoRegistrationImpl : ICSCodeGenerator
    {
        CSCodeGenerationResult ICSCodeGenerator.Implement( IActivityMonitor monitor, ICSCodeGenerationContext ctx )
        {
            var rootCtor = ctx.Assembly.Code.Global.FindOrCreateNamespace( "CK.StObj" )
                                                   .FindType( StObjContextRoot.RootContextTypeName )?
                                                   .FindFunction( $"{StObjContextRoot.RootContextTypeName}(IActivityMonitor)", false );

            if( rootCtor == null )
            {
                Throw.InvalidOperationException( $"Unable to find the '{StObjContextRoot.RootContextTypeName}(IActivityMonitor)' constructor." );
            }
            var dapperSupport = rootCtor.CreatePart();
            dapperSupport.GeneratedByComment()
                         .Append( @"
        // Since Dapper uses static fields to be configured, this cannot support
        // multiple StObjMap to be loaded with a common IPoco since Dapper's query cache captures
        // the mapping once for all the first time it sees a Type.
        // Purging the cache here would be too dangerous (existing TypeHandlers will be evicted).
        Dapper.SqlMapper.AddAbstractTypeMap( current =>
        {
            // Here we call the previous handler before the new one: there is no good solution here
            // 
            // Multiple StObjMap in the same process/domain is not a supported scenario
            // (except in tests unfortunately). We live with this for the moment. The right solution
            // is simply to replace Dapper with code generation (based on view/select signatures).
            //
            return type =>
            {
                var m = current?.Invoke( type );
                if( m == null )
                {
                    var f = PocoDirectory_CK.Instance.Find( type );
                    if( f != null ) m = f.PocoClassType;
                }
                return m;
            };
        } );" );
            return CSCodeGenerationResult.Success;
        }
    }
}
