using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class SetupHandlerContainer : ISetupHandlerContainer
    {
        protected SetupHandlerContainer( SetupDriverContainer d )
        {
            if( d == null ) throw new ArgumentNullException();
            Driver = d;
            Driver.AddHandler( this );
        }

        protected SetupDriverContainer Driver { get; private set; }

        void CheckCall( SetupDriverContainer d )
        {
            if( d != Driver ) throw new ArgumentException( "Call mismatch: handler is bound to another driver." );
        }

        bool ISetupHandlerContainer.Init( SetupDriverContainer d )
        {
            CheckCall( d );
            return Init();
        }

        bool ISetupHandlerContainer.InitContent( SetupDriverContainer d )
        {
            CheckCall( d );
            return InitContent();
        }

        bool ISetupHandlerContainer.Install( SetupDriverContainer d )
        {
            CheckCall( d );
            return Install();
        }

        bool ISetupHandlerContainer.InstallContent( SetupDriverContainer d )
        {
            CheckCall( d );
            return InstallContent();
        }

        bool ISetupHandlerContainer.Settle( SetupDriverContainer d )
        {
            CheckCall( d );
            return Settle();
        }

        bool ISetupHandlerContainer.SettleContent( SetupDriverContainer d )
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
