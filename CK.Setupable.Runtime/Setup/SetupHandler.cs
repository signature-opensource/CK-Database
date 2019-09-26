#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\SetupHandler.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using System;

namespace CK.Setup
{
    /// <summary>
    /// Template base class for handlers bound to a specific <see cref="SetupItemDriver"/>.
    /// Nothing prevent such handlers to be explicitly <see cref="SetupItemDriver.AddHandler(ISetupHandler)">added</see>
    /// to another driver (but this should probably be avoided).
    /// </summary>
    public class SetupHandler : ISetupHandler
    {
        /// <summary>
        /// Initializes a new <see cref="SetupHandler"/> bound to a <see cref="SetupItemDriver"/>.
        /// A newly created handler is automatically added to the handlers of the item driver.
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
            if( !(Driver.Item is T) ) throw new ArgumentException( $"Driver '{Driver.FullName}' has an item of type '{Driver.Item.GetType().Name}'. this handler work with items of type '{typeof( T ).Name}'." );
        }

        bool ISetupHandler.Init( IActivityMonitor monitor, SetupItemDriver d ) => Init( monitor );

        bool ISetupHandler.InitContent( IActivityMonitor monitor, SetupItemDriver d ) => InitContent( monitor );

        bool ISetupHandler.Install( IActivityMonitor monitor, SetupItemDriver d ) => Install( monitor );

        bool ISetupHandler.InstallContent( IActivityMonitor monitor, SetupItemDriver d ) => InstallContent( monitor );

        bool ISetupHandler.Settle( IActivityMonitor monitor, SetupItemDriver d ) => Settle( monitor );

        bool ISetupHandler.SettleContent( IActivityMonitor monitor, SetupItemDriver d ) => SettleContent( monitor );

        bool ISetupHandler.OnStep( IActivityMonitor monitor, SetupItemDriver d, SetupCallGroupStep step ) => OnStep( monitor, step );

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <returns>Always true.</returns>
        protected virtual bool Init( IActivityMonitor monitor ) => true;

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <returns>Always true.</returns>
        protected virtual bool InitContent( IActivityMonitor monitor ) => true;

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <returns>Always true.</returns>
        protected virtual bool Install( IActivityMonitor monitor ) => true;

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <returns>Always true.</returns>
        protected virtual bool InstallContent( IActivityMonitor monitor ) => true;

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <returns>Always true.</returns>
        protected virtual bool Settle( IActivityMonitor monitor ) => true;

        /// <summary>
        /// This default implementation does nothing and returns true.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <returns>Always true.</returns>
        protected virtual bool SettleContent( IActivityMonitor monitor ) => true;

        /// <summary>
        /// This method is called right after its corresponding dedicated method.
        /// This centralized step based method is easier to use than the different
        /// available overrides when the step actions are structurally the same and
        /// only their actual contents/data is step dependent.
        /// Does nothing (always returns true).
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="step">Current process step.</param>
        /// <returns>Always true.</returns>
        protected virtual bool OnStep( IActivityMonitor monitor, SetupCallGroupStep step ) => true;

    }
}
