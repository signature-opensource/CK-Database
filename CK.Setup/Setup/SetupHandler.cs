using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class SetupHandler : ISetupHandler
    {
        protected SetupHandler( SetupDriver d )
        {
            Driver = d;
            Driver.AddHandler( this );
        }

        protected SetupDriver Driver { get; private set; }

        void CheckCall( SetupDriver d )
        {
            if( d != Driver ) throw new ArgumentException( "Call mismatch: handler is bound to another driver." );
        }

        bool ISetupHandler.Init( SetupDriver d )
        {
            CheckCall( d );
            return Init();
        }

        bool ISetupHandler.Install( SetupDriver d )
        {
            CheckCall( d );
            return Install();
        }

        bool ISetupHandler.Settle( SetupDriver d )
        {
            CheckCall( d );
            return Settle();
        }

        protected virtual bool Init()
        {
            return true;
        }

        protected virtual bool Install()
        {
            return true;
        }

        protected virtual bool Settle()
        {
            return true;
        }

    }
}
