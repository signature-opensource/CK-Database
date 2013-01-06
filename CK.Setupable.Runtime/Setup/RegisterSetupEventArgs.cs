using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Event argument for registration step.
    /// Adds registering capacity to <see cref="SetupEngine.SetupEvent"/>.
    /// </summary>
    public class RegisterSetupEventArgs : SetupEventArgs
    {
        internal List<IDependentItem> RegisteredItems;
        internal List<IDependentItemDiscoverer> RegisteredDiscoverers;

        internal RegisterSetupEventArgs()
            : base( SetupStep.None )
        {
        }

        /// <summary>
        /// Registers an <see cref="IDependentItem"/> object.
        /// </summary>
        /// <param name="item">Object to register.</param>
        public void Register( IDependentItem item )
        {
            if( item == null ) throw new ArgumentNullException( "item" );
            if( RegisteredItems == null ) RegisteredItems = new List<IDependentItem>();
            RegisteredItems.Add( item );
        }

        class Trick : IDependentItemDiscoverer
        {
            public IEnumerable<IDependentItem> Registered;
  
            public IEnumerable<IDependentItem> GetOtherItemsToRegister()
            {
                return Registered;
            }
        }

        /// <summary>
        /// Registers multiple <see cref="IDependentItem"/> objects.
        /// </summary>
        /// <param name="item">Object to register.</param>
        public void Register( IEnumerable<IDependentItem> items )
        {
            if( items == null ) throw new ArgumentNullException( "items" );
            Register( new Trick() { Registered = items } );
        }

        /// <summary>
        /// Registers an <see cref="IDependentItemDiscoverer"/> object.
        /// </summary>
        /// <param name="discoverer">Discoverer to register.</param>
        public void Register( IDependentItemDiscoverer discoverer )
        {
            if( discoverer == null ) throw new ArgumentNullException( "discoverer" );
            if( RegisteredDiscoverers == null ) RegisteredDiscoverers = new List<IDependentItemDiscoverer>();
            RegisteredDiscoverers.Add( discoverer );
        }

    }
}
