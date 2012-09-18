using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class UnitySqlSetupContext : SqlSetupContext
    {
        IUnityContainer _container;

        public UnitySqlSetupContext( string connectionString, IActivityLogger logger, IUnityContainer container )
            : base( connectionString, logger )
        {
            if( container == null ) throw new ArgumentNullException( "container" );
            _container = container;
        }

        public override SetupDriver CreateDriver( Type containerType, SetupDriver.BuildInfo info )
        {
            return (SetupDriver)_container.Resolve( containerType, new DependencyOverride<SetupDriver.BuildInfo>( info ) );
        }
    }
}
