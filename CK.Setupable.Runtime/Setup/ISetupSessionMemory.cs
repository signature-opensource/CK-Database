#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\ISetupSessionMemory.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion


namespace CK.Setup;

/// <summary>
/// Memory for the setup.
/// </summary>
public interface ISetupSessionMemory
{
    /// <summary>
    /// Registers a key that must not exceed 255 characters long otherwise an exception is thrown.
    /// </summary>
    /// <param name="itemKey">The key to set.</param>
    /// <param name="itemValue">Value of the item. Must be not null and have any length.</param>
    void RegisterItem( string itemKey, string itemValue = "" );

    /// <summary>
    /// Gets the value of a registered key. 
    /// </summary>
    /// <param name="itemKey">Key to get.</param>
    /// <returns>Null if not registered.</returns>
    string FindRegisteredItem( string itemKey );

    /// <summary>
    /// Gets whether the key has already been registered. 
    /// </summary>
    /// <param name="itemKey">Key to get.</param>
    /// <returns>True if already registered.</returns>
    bool IsItemRegistered( string itemKey );

}
