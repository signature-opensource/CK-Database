#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\Scripts\SetupScriptExtension.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion


namespace CK.Setup;

/// <summary>
/// Contains extension methods for <see cref="ISetupScript"/>.
/// </summary>
public static class SetupScriptExtensions
{
    /// <summary>
    /// Creates a new <see cref="ISetupScript"/> with a new script and extension.
    /// </summary>
    /// <param name="this">This setup script.</param>
    /// <param name="newScript">The new script text.</param>
    /// <param name="newExtension">The new extension.</param>
    /// <returns>A new setup script object.</returns>
    public static ISetupScript SetScriptAndExtension( this ISetupScript @this, string newScript, string newExtension )
    {
        ParsedFileName newName = @this.Name.SetExtension( newExtension );
        return new SetupScript( newName, newScript );
    }

}
