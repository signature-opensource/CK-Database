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
        [SqlProcedure( "sActorOnly" )]
        public abstract string ActorOnly( [ParameterSource]IActorCallContext ctx );

        [SqlProcedure( "sActorOnly" )]
        public abstract Task<string> ActorOnlyAsync( [ParameterSource]IActorCallContext ctx );

        [SqlProcedure( "sCultureOnly" )]
        public abstract string CultureOnly( [ParameterSource]ICultureCallContext ctx );

        [SqlProcedure( "sCultureOnly" )]
        public abstract Task<string> CultureOnlyAsync( [ParameterSource]ICultureCallContext ctx );

        [SqlProcedure( "sTenantOnly" )]
        public abstract string TenantOnly( [ParameterSource]ITenantCallContext ctx );

        [SqlProcedure( "sTenantOnly" )]
        public abstract Task<string> TenantOnlyAsync( [ParameterSource]ITenantCallContext ctx );

        [SqlProcedure( "sActorCulture" )]
        public abstract Task<string> ActorCulture( [ParameterSource]IActorCultureCallContext ctx );

        [SqlProcedure( "sActorCulture" )]
        public abstract Task<string> ActorCultureAsync( [ParameterSource]IActorCultureCallContext ctx );

        [SqlProcedure( "sCultureTenant" )]
        public abstract Task<string> CultureTenant( [ParameterSource]ICultureTenantCallContext ctx );

        [SqlProcedure( "sCultureTenant" )]
        public abstract Task<string> CultureTenantAsync( [ParameterSource]ICultureTenantCallContext ctx );

        [SqlProcedure( "sAllContexts" )]
        public abstract Task<string> AllContexts( [ParameterSource]IAllCallContext ctx );

        [SqlProcedure( "sAllContexts" )]
        public abstract Task<string> AllContextsAsync( [ParameterSource]IAllCallContext ctx );

    }
}
