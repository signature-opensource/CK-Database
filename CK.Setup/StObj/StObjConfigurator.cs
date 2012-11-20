using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{

    public class StObjConfigurator : IAmbientContractDispatcher, IStObjStructuralConfigurator, IStObjDependencyResolver, IStObjSetupConfigurator, IStObjSetupItemFactory
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

        void IStObjDependencyResolver.ResolveParameterValue( IActivityLogger logger, IParameter p )
        {
        }

        void IStObjDependencyResolver.ResolveExternalPropertyValue( IActivityLogger logger, IAmbientProperty a )
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
