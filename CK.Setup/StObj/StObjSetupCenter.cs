using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class StObjSetupCenter
    {
        readonly IActivityLogger _logger;

        public StObjSetupCenter( IActivityLogger logger )
        {
            if( logger != null ) throw new ArgumentNullException( "logger" );
            _logger = logger;
        }

        public void Register( StObjCollectorResult c )
        {
            if( c == null ) throw new ArgumentNullException( "c" );
            if( c.HasFatalError ) throw new ArgumentException( "Collector must have no fatal error.", "c" );

            Dictionary<IStObj,IDependentItem> setupableItems = new Dictionary<IStObj, IDependentItem>();
            foreach( var r in c.RootStObjs )
            {
                foreach( var o in r.SpecializationPath )
                {
                    setupableItems.Add( o, CreateItem( o ) );
                }
            }

        }

        IDependentItem CreateItem( IStObj o )
        {
            Debug.Assert( o != null );
            string fullName = SetupNameAttribute.GetFullName( _logger, true, o.ObjectType );
            string versions = AvailableVersionsAttribute.GetVersionsString( o.ObjectType );

            return null;
        }
    }
}
