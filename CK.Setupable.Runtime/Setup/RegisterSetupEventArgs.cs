#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\RegisterSetupEventArgs.cs) is part of CK-Database. 
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
    /// Event argument for registration step.
    /// Adds registering capacity to <see cref="ISetupEngine.SetupEvent"/>.
    /// </summary>
    public class RegisterSetupEventArgs : SetupEventArgs
    {
        internal List<ISetupItem> RegisteredItems;
        internal List<IDependentItemDiscoverer<ISetupItem>> RegisteredDiscoverers;

        internal RegisterSetupEventArgs()
            : base( SetupStep.PreInit )
        {
        }

        /// <summary>
        /// Registers an <see cref="ISetupItem"/> object.
        /// </summary>
        /// <param name="item">Object to register.</param>
        public void Register( ISetupItem item )
        {
            if( item == null ) throw new ArgumentNullException( "item" );
            if( RegisteredItems == null ) RegisteredItems = new List<ISetupItem>();
            RegisteredItems.Add( item );
        }

        class Trick : IDependentItemDiscoverer<ISetupItem>
        {
            public IEnumerable<ISetupItem> Registered;
  
            public IEnumerable<ISetupItem> GetOtherItemsToRegister()
            {
                return Registered;
            }
        }

        /// <summary>
        /// Registers multiple <see cref="IDependentItem"/> objects.
        /// </summary>
        /// <param name="items">Objects to register.</param>
        public void Register( IEnumerable<ISetupItem> items )
        {
            if( items == null ) throw new ArgumentNullException( "items" );
            Register( new Trick() { Registered = items } );
        }

        /// <summary>
        /// Registers an <see cref="IDependentItemDiscoverer"/> object.
        /// </summary>
        /// <param name="discoverer">Discoverer to register.</param>
        public void Register( IDependentItemDiscoverer<ISetupItem> discoverer )
        {
            if( discoverer == null ) throw new ArgumentNullException( "discoverer" );
            if( RegisteredDiscoverers == null ) RegisteredDiscoverers = new List<IDependentItemDiscoverer<ISetupItem>>();
            RegisteredDiscoverers.Add( discoverer );
        }

    }
}
