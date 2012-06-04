using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Core;
using System.Reflection;


namespace CK.Setup.Database
{


    public class SetupObjectRegisterer
    {
        AmbiantContractCollector _cc;

        public SetupObjectRegisterer()
        {
            _cc = new AmbiantContractCollector();
        }

        public void RegisterTypes( Assembly a, IActivityLogger logger )
        {
            if( a == null ) throw new ArgumentNullException( "a" );
            if( logger == null ) throw new ArgumentNullException( "logger" );
            
            using( logger.OpenGroup( LogLevel.Trace, "Registering assembly '{0}'.", a.FullName ) )
            {
                int nbAlready = _cc.RegisteredTypeCount;
                _cc.Register( a.GetTypes(), Util.ActionVoid );
                logger.CloseGroup( String.Format( "{0} types(s) registered.", _cc.RegisteredTypeCount - nbAlready ) );
            }
        }

        public void CreateSetupObjects( IActivityLogger logger )
        {
            AmbiantContractCollectorResult r = _cc.GetResult();
            if( r.CheckErrorAndWarnings( logger ) )
            {
                foreach( var t in r.ConcreteClasses )
                {

                }
            }
        }

    }

}
