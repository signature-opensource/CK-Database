#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\SetupHandler.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class SetupHandler : ISetupHandler
    {
        protected SetupHandler( DependentItemSetupDriver d )
        {
            if( d == null ) throw new ArgumentNullException();
            Driver = d;
            Driver.AddHandler( this );
        }

        protected DependentItemSetupDriver Driver { get; private set; }

        void CheckCall( DependentItemSetupDriver d )
        {
            if( d != Driver ) throw new ArgumentException( "Call mismatch: handler is bound to another driver." );
        }

        bool ISetupHandler.Init( DependentItemSetupDriver d )
        {
            CheckCall( d );
            return Init();
        }

        bool ISetupHandler.InitContent( DependentItemSetupDriver d )
        {
            CheckCall( d );
            return InitContent();
        }

        bool ISetupHandler.Install( DependentItemSetupDriver d )
        {
            CheckCall( d );
            return Install();
        }

        bool ISetupHandler.InstallContent( DependentItemSetupDriver d )
        {
            CheckCall( d );
            return InstallContent();
        }

        bool ISetupHandler.Settle( DependentItemSetupDriver d )
        {
            CheckCall( d );
            return Settle();
        }

        bool ISetupHandler.SettleContent( DependentItemSetupDriver d )
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
