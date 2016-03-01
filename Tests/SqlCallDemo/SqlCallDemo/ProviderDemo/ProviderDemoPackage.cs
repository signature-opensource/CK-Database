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

namespace SqlCallDemo.ProviderDemo
{

    [SqlPackage( Schema = "Provider", ResourcePath = "Res" ), Versions( "1.0.0" )]
    public abstract partial class ProviderDemoPackage : SqlPackage
    {
        [SqlProcedureNonQuery( "sActorOnly" )]
        public abstract string ActorOnly( [ParameterSource]IActorCallContext ctx );

        [SqlProcedureNonQuery( "sActorOnly" )]
        public abstract Task<string> ActorOnlyAsync( [ParameterSource]IActorCallContext ctx );

        [SqlProcedureNonQuery( "sCultureOnly" )]
        public abstract string CultureOnly( [ParameterSource]ICultureCallContext ctx );

        [SqlProcedureNonQuery( "sCultureOnly" )]
        public abstract Task<string> CultureOnlyAsync( [ParameterSource]ICultureCallContext ctx );

        [SqlProcedureNonQuery( "sTenantOnly" )]
        public abstract string TenantOnly( [ParameterSource]ITenantCallContext ctx );

        [SqlProcedureNonQuery( "sTenantOnly" )]
        public abstract Task<string> TenantOnlyAsync( [ParameterSource]ITenantCallContext ctx );

        [SqlProcedureNonQuery( "sActorCulture" )]
        public abstract Task<string> ActorCulture( [ParameterSource]IActorCultureCallContext ctx );

        [SqlProcedureNonQuery( "sActorCulture" )]
        public abstract Task<string> ActorCultureAsync( [ParameterSource]IActorCultureCallContext ctx );

        [SqlProcedureNonQuery( "sCultureTenant" )]
        public abstract Task<string> CultureTenant( [ParameterSource]ICultureTenantCallContext ctx );

        [SqlProcedureNonQuery( "sCultureTenant" )]
        public abstract Task<string> CultureTenantAsync( [ParameterSource]ICultureTenantCallContext ctx );

        [SqlProcedureNonQuery( "sAllContexts" )]
        public abstract Task<string> AllContexts( [ParameterSource]IAllCallContext ctx );

        [SqlProcedureNonQuery( "sAllContexts" )]
        public abstract Task<string> AllContextsAsync( [ParameterSource]IAllCallContext ctx );

    }
}
