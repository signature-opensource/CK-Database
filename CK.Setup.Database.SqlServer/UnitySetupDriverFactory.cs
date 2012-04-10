using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;

namespace CK.Setup.Database.SqlServer
{
    public class UnitySetupDriverFactory : ISetupDriverFactory
    {
        IUnityContainer _container;

        public UnitySetupDriverFactory( IUnityContainer container )
        {
            if( container == null ) throw new ArgumentNullException( "container" );
            _container = container;
        }

        public SetupDriver CreateDriver( Type driverType, SetupDriver.BuildInfo info )
        {
            return (SetupDriver)_container.Resolve( driverType, new DependencyOverride<SetupDriver.BuildInfo>( info ) );
        }

        public SetupDriverContainer CreateDriverContainer( Type containerType, SetupDriverContainer.BuildInfo info )
        {
            return (SetupDriverContainer)_container.Resolve( containerType, new DependencyOverride<SetupDriverContainer.BuildInfo>( info ) );
        }
    }
}
