using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class ContainerHandler : IContainerHandler
    {
        protected ContainerHandler( ContainerDriver d )
        {
            if( d == null ) throw new ArgumentNullException();
            Driver = d;
            Driver.AddHandler( this );
        }

        protected ContainerDriver Driver { get; private set; }

        void CheckCall( ContainerDriver d )
        {
            if( d != Driver ) throw new ArgumentException( "Call mismatch: handler is bound to another driver." );
        }

        bool IContainerHandler.Init( ContainerDriver d )
        {
            CheckCall( d );
            return Init();
        }

        bool IContainerHandler.InitContent( ContainerDriver d )
        {
            CheckCall( d );
            return InitContent();
        }

        bool IContainerHandler.Install( ContainerDriver d )
        {
            CheckCall( d );
            return Install();
        }

        bool IContainerHandler.InstallContent( ContainerDriver d )
        {
            CheckCall( d );
            return InstallContent();
        }

        bool IContainerHandler.Settle( ContainerDriver d )
        {
            CheckCall( d );
            return Settle();
        }

        bool IContainerHandler.SettleContent( ContainerDriver d )
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
