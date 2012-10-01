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

        void IAmbientContractDispatcher.Dispatch( Type t, ISet<Type> contexts )
        {
        }

        void IStObjStructuralConfigurator.Configure( IActivityLogger logger, IStObjMutableItem o )
        {
        }

        void IStObjDependencyResolver.ResolveParameterValue( IActivityLogger logger, IParameter p )
        {
        }

        void IStObjDependencyResolver.ResolvePropertyValue( IActivityLogger logger, IAmbientProperty a )
        {
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityLogger logger, IMutableStObjSetupData data )
        {
        }

        IMutableDependentItem IStObjSetupItemFactory.CreateDependentItem( IActivityLogger logger, IStObjSetupData data )
        {
            return null;
        }

    }

}
