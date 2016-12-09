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
    /// <summary>
    /// Base class for handlers bound to a <see cref="SetupItemDriver"/>.
    /// </summary>
    public class SetupHandler : ISetupHandler
    {
        /// <summary>
        /// Initializes a new <see cref="SetupHandler"/> bound to a <see cref="SetupItemDriver"/>.
        /// This new handler is automatically added to the handlers of the item driver.
        /// </summary>
        /// <param name="d">The item driver. Can not be null.</param>
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

        bool ISetupHandler.Init( SetupItemDriver d ) => Init();

        bool ISetupHandler.InitContent( SetupItemDriver d ) => InitContent();

        bool ISetupHandler.Install( SetupItemDriver d ) => Install();

        bool ISetupHandler.InstallContent( SetupItemDriver d ) => InstallContent();

        bool ISetupHandler.Settle( SetupItemDriver d ) => Settle();

        bool ISetupHandler.SettleContent( SetupItemDriver d ) => SettleContent();

        bool ISetupHandler.OnStep( SetupItemDriver d, SetupCallGroupStep step ) => OnStep( step );

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool Init() => true;

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool InitContent() => true;

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool Install() => true;

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool InstallContent() => true;

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool Settle() => true;

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool SettleContent() => true;

        /// <summary>
        /// This method is called right after its corresponding dedicated method.
        /// This centralized step based method is easier to use then the different
        /// available overrides when the step actions are structurally the same and
        /// only their actual contents/data is step dependent.
        /// Does nothing (always returns true).
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool OnStep( SetupCallGroupStep step ) => true;

    }
}
