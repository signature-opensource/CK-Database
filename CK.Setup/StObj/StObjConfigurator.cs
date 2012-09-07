using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{

    public class StObjConfigurator : IAmbiantContractDispatcher, IStObjStructuralConfigurator, IStObjDependencyResolver, IStObjSetupConfigurator
    {
        bool IAmbiantContractDispatcher.IsAmbiantContractClass( Type t )
        {
            return false;
        }

        void IAmbiantContractDispatcher.Dispatch( Type t, ISet<Type> contexts )
        {
        }

        void IStObjStructuralConfigurator.Configure( IActivityLogger logger, IStObjMutableItem o )
        {
        }

        object IStObjDependencyResolver.ResolveParameterValue( IActivityLogger logger, IParameter parameter )
        {
            return Type.Missing;
        }

        object IStObjDependencyResolver.ResolvePropertyValue( IActivityLogger logger, IAmbiantProperty ambiantProperty )
        {
            return Type.Missing;
        }

        void IStObjSetupConfigurator.ConfigureDependentItem( IActivityLogger logger, StObjSetupData data )
        {
        }

    }

}
