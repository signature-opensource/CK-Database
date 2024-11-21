using System;

namespace CK.Setup;


/// <summary>
/// Holds the head of a Chain of Responsibility composed of <see cref="SetupConfigurationLayer"/>.
/// </summary>
public sealed class SetupAspectConfigurator
{
    SetupConfigurationLayer _first;

    /// <summary>
    /// Adds a configuration layer as the first one.
    /// </summary>
    /// <param name="layer">Configuration layer to add. Must have a null <see cref="SetupConfigurationLayer.Host"/>.</param>
    public void AddLayer( SetupConfigurationLayer layer )
    {
        if( layer == null ) throw new ArgumentNullException( nameof( layer ) );
        if( layer.Host != null ) throw new ArgumentException( $"{nameof( SetupConfigurationLayer )} is already hosted.", nameof( layer ) );
        layer.Next = _first;
        _first = layer;
        layer.Host = this;
    }

    /// <summary>
    /// Removes a previously added configuration layer.
    /// </summary>
    /// <param name="configurator">Configuration layer to remove.</param>
    public void RemoveLayer( SetupConfigurationLayer configurator )
    {
        if( configurator == null ) throw new ArgumentNullException( nameof( configurator ) );
        if( configurator.Host != this ) throw new ArgumentException( $"{nameof( SetupConfigurationLayer )} is not hosted by this {nameof( SetupAspectConfigurator )}.", nameof( configurator ) );
        SetupConfigurationLayer prev = null;
        SetupConfigurationLayer x = _first;
        while( x != configurator ) x = x.Next;
        if( prev != null ) prev.Next = configurator.Next;
        else _first = configurator.Next;
        configurator.Host = null;
    }

    /// <summary>
    /// Gets the first <see cref="SetupConfigurationLayer"/>.
    /// Null if no configurator has been added.
    /// </summary>
    public SetupConfigurationLayer FirstLayer => _first;
}
