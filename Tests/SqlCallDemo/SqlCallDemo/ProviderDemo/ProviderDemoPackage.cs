using System.Threading.Tasks;
using CK.Core;

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
        public abstract string ActorCulture( [ParameterSource]IActorCultureCallContext ctx );

        [SqlProcedure( "sActorCulture" )]
        public abstract Task<string> ActorCultureAsync( [ParameterSource]IActorCultureCallContext ctx );

        [SqlProcedure( "sCultureTenant" )]
        public abstract string CultureTenant( [ParameterSource]ICultureTenantCallContext ctx );

        [SqlProcedure( "sCultureTenant" )]
        public abstract Task<string> CultureTenantAsync( [ParameterSource]ICultureTenantCallContext ctx );

        [SqlProcedure( "sAllContexts" )]
        public abstract string AllContexts( [ParameterSource]IAllCallContext ctx );

        [SqlProcedure( "sAllContexts" )]
        public abstract Task<string> AllContextsAsync( [ParameterSource]IAllCallContext ctx );

    }
}
