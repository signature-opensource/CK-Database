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
        protected SetupHandler( SetupItemDriver d )
        {
            if( d == null ) throw new ArgumentNullException();
            Driver = d;
            Driver.AddHandler( this );
        }

        /// <summary>
        /// Gets the driver to which this handler is associated.
        /// </summary>
        protected SetupItemDriver Driver { get; private set; }

        /// <summary>
        /// Helper function for specialized handlers that throws an ArgumentException if the driver's item type is not the 
        /// one expected.
        /// </summary>
        /// <typeparam name="T">Expected item type.</typeparam>
        protected void CheckItemType<T>()
        {
            if( !(Driver.Item is T) ) throw new ArgumentException( "Driver '{0}' has an item of type '{1}'. this handler work with items of type '{2}'.", String.Format( "", Driver.FullName, Driver.Item.GetType().Name, typeof( T ).Name ) );
        }


        void CheckCall( SetupItemDriver d )
        {
            if( d != Driver ) throw new InvalidOperationException( String.Format( "Call mismatch: handler is bound to '{0}' but called from '{1}'.", Driver.FullName, d.FullName ) );
        }

        bool ISetupHandler.Init( SetupItemDriver d )
        {
            CheckCall( d );
            return Init();
        }

        bool ISetupHandler.InitContent( SetupItemDriver d )
        {
            CheckCall( d );
            return InitContent();
        }

        bool ISetupHandler.Install( SetupItemDriver d )
        {
            CheckCall( d );
            return Install();
        }

        bool ISetupHandler.InstallContent( SetupItemDriver d )
        {
            CheckCall( d );
            return InstallContent();
        }

        bool ISetupHandler.Settle( SetupItemDriver d )
        {
            CheckCall( d );
            return Settle();
        }

        bool ISetupHandler.SettleContent( SetupItemDriver d )
        {
            CheckCall( d );
            return SettleContent();
        }

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool Init()
        {
            return true;
        }

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool InitContent()
        {
            return true;
        }

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool Install()
        {
            return true;
        }

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool InstallContent()
        {
            return true;
        }

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool Settle()
        {
            return true;
        }

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool SettleContent()
        {
            return true;
        }

    }
}
