#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\GroupsAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.Core;

/// <summary>
/// Simple attributes to define groups to which a class belong by names.
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public class GroupsAttribute : Setup.BaseItemNamesAttribute
{
    /// <summary>
    /// Defines groups by their names.
    /// </summary>
    /// <param name="groups">Comma separated list of group names.</param>
    public GroupsAttribute( string groups )
        : base( groups )
    {
    }
}
