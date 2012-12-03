using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{

    public class StObjConfigurator : IAmbientContractDispatcher, IStObjStructuralConfigurator, IStObjValueResolver, IStObjSetupConfigurator, IStObjSetupItemFactory
    {
        bool IAmbientContractDispatcher.IsAmbientContractClass( Type t )
        {
            return false;
        }

        void IAmbientContractDispatcher.Dispatch( Type t, ISet<string> contexts )
        {
        }

        void IStObjStructuralConfigurator.Configure( IActivityLogger logger, IStObjMutableItem o )
        {
        }

        void IStObjValueResolver.ResolveParameterValue( IActivityLogger logger, IStObjFinalParameter p )
        {
        }

        void IStObjValueResolver.ResolveExternalPropertyValue( IActivityLogger logger, IStObjFinalAmbientProperty a )
        {
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityLogger logger, IMutableStObjSetupData data )
        {
        }

        IMutableSetupItem IStObjSetupItemFactory.CreateDependentItem( IActivityLogger logger, IStObjSetupData data )
        {
            return null;
        }

    }

}
