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
            if( d == null ) throw new ArgumentNullException();
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

        bool ISetupHandler.InitContent( SetupDriver d )
        {
            CheckCall( d );
            return InitContent();
        }

        bool ISetupHandler.Install( SetupDriver d )
        {
            CheckCall( d );
            return Install();
        }

        bool ISetupHandler.InstallContent( SetupDriver d )
        {
            CheckCall( d );
            return InstallContent();
        }

        bool ISetupHandler.Settle( SetupDriver d )
        {
            CheckCall( d );
            return Settle();
        }

        bool ISetupHandler.SettleContent( SetupDriver d )
        {
            CheckCall( d );
            return SettleContent();
        }

        protected virtual bool Init()
        {
            return true;
        }

        protected virtual bool InitContent()
        {
            return true;
        }

        protected virtual bool Install()
        {
            return true;
        }

        protected virtual bool InstallContent()
        {
            return true;
        }

        protected virtual bool Settle()
        {
            return true;
        }

        protected virtual bool SettleContent()
        {
            return true;
        }

    }
}
