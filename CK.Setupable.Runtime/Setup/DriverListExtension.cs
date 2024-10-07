using System;
using CK.Core;

namespace CK.Setup;

/// <summary>
/// Supports extension methods on <see cref="IDriverList"/>.
/// </summary>
public static class DriverListExtension
{
    /// <summary>
    /// Finds the typed driver with a given name.
    /// </summary>
    /// <typeparam name="T">Type of the driver to find.</typeparam>
    /// <param name="this">This driver list.</param>
    /// <param name="fullName">The full name of the driver/item to find.</param>
    /// <param name="throwIfNotFound">
    /// False to return null instead of throwing an exception if the driver can not be found
    /// or is not of the expected type.
    /// </param>
    /// <returns>The typed driver or null if not found and <paramref name="throwIfNotFound"/> is false.</returns>
    public static T? Find<T>( this IDriverList @this, string fullName, bool throwIfNotFound = true )
        where T : SetupItemDriver
    {
        if( fullName == null )
        {
            if( throwIfNotFound ) throw new ArgumentNullException( nameof( fullName ) );
            return null;
        }
        T? result = @this[fullName] as T;
        if( result == null && throwIfNotFound )
        {
            var existing = @this[fullName];
            if( existing == null )
            {
                throw new CKException( $"Unable to find object '{fullName}' (while looking for a driver of type '{typeof( T ).Name}')." );
            }
            else
            {
                throw new CKException( $"Object named '{fullName}' should be a driver of type '{typeof( T ).Name}' but its type is '{existing.GetType().Name}'." );
            }
        }
        return result;
    }

    /// <summary>
    /// Finds the typed driver associated to an item.
    /// </summary>
    /// <typeparam name="T">Type of the driver to find.</typeparam>
    /// <param name="this">This driver list.</param>
    /// <param name="item">The full name of the driver/item to find.</param>
    /// <param name="throwIfNotFound">
    /// False to return null instead of throwing an exception if the driver can not be found
    /// or is not of the expected type.
    /// </param>
    /// <returns>The typed driver or null if not found and <paramref name="throwIfNotFound"/> is false.</returns>
    public static T Find<T>( this IDriverList @this, IDependentItem item, bool throwIfNotFound = true )
        where T : SetupItemDriver
    {
        if( item == null )
        {
            if( throwIfNotFound ) throw new ArgumentNullException( nameof( item ) );
            return null;
        }
        T result = @this[item] as T;
        if( result == null && throwIfNotFound )
        {
            var existing = @this[item];
            if( existing == null )
            {
                throw new CKException( $"Unable to find object '{item.FullName}' (while looking for a driver of type '{typeof( T ).Name}')." );
            }
            else
            {
                throw new CKException( $"Object named '{item.FullName}' should be a driver of type '{typeof( T ).Name}' but its type is '{existing.GetType().Name}'." );
            }
        }
        return result;
    }
}
