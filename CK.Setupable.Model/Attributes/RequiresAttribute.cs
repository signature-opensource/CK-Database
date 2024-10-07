#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\RequiresAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.Core;

/// <summary>
/// Simple attributes to define requirements of a class by names.
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public class RequiresAttribute : Setup.BaseItemNamesAttribute
{
    /// <summary>
    /// Defines requirements by their names.
    /// </summary>
    /// <param name="requires">Comma separated list of requirement item names.</param>
    public RequiresAttribute( string requires )
        : base( requires )
    {
    }

}
