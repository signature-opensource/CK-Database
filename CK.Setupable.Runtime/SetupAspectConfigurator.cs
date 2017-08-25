using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Setup
{

    /// <summary>
    /// Holds the head of a Chain of Responsibility composed of <see cref="StObjBuildConfigurator"/>.
    /// </summary>
    public sealed class SetupAspectConfigurator
    {
        StObjBuildConfigurator _first;

        /// <summary>
        /// Adds a configurator as the first configurator.
        /// </summary>
        /// <param name="configurator">Configurator to add. Must have a null <see cref="StObjBuildConfigurator.Host"/>.</param>
        public void AddConfigurator( StObjBuildConfigurator configurator )
        {
            if( configurator == null ) throw new ArgumentNullException( nameof( configurator ) );
            if( configurator.Host != null ) throw new ArgumentException( "StObjBuildConfigurator is already hosted.", nameof( configurator ) );
            configurator.Next = _first;
            _first = configurator;
            configurator.Host = this;
        }

        /// <summary>
        /// Removes a previously added configurator.
        /// </summary>
        /// <param name="configurator">Configurator to remove.</param>
        public void RemoveConfigurator( StObjBuildConfigurator configurator )
        {
            if( configurator == null ) throw new ArgumentNullException( nameof( configurator ) );
            if( configurator.Host != this ) throw new ArgumentException( "StObjBuildConfigurator is not hosted by this StObjEngineConfigurator.", nameof( configurator ) );
            StObjBuildConfigurator prev = null;
            StObjBuildConfigurator x = _first;
            while( x != configurator ) x = x.Next;
            if( prev != null ) prev.Next = configurator.Next;
            else _first = configurator.Next;
            configurator.Host = null;
        }

        /// <summary>
        /// Gets the first <see cref="StObjBuildConfigurator"/>.
        /// Null if no configurator has been added.
        /// </summary>
        public StObjBuildConfigurator BuildConfigurator => _first;
    }

}
