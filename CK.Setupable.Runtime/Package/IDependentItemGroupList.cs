#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Package\IDependentItemGroupList.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Collections.Generic;

namespace CK.Setup;

/// <summary>
/// Mutable list of <see cref="IDependentItemGroupRef"/> that can handle named items (ie. <see cref="NamedDependentItemGroupRef"/>) transparently.
/// Instances can be created by <see cref="DependentItemListFactory"/>.
/// </summary>
public interface IDependentItemGroupList : IList<IDependentItemGroupRef>
{
    /// <summary>
    /// Adds a full name as a <see cref="NamedDependentItemGroupRef"/>.
    /// </summary>
    /// <param name="fullName">Full name of the dependency. When null or empty, nothing is added.</param>
    void Add( string fullName );

    /// <summary>
    /// Removes a full name.
    /// </summary>
    /// <param name="fullName">Full name of the dependency. When null or empty, nothing is removed.</param>
    void Remove( string fullName );

    /// <summary>
    /// <see cref="Add(string)">Adds</see> multiple full names.
    /// </summary>
    /// <param name="fullNames">The list of full names to add. When null or empty, nothing is added.</param>
    void Add( IEnumerable<string> fullNames );

    /// <summary>
    /// Splits the parameter on the comma and <see cref="Add(string)">adds</see> the multiple full names.
    /// </summary>
    /// <param name="commaSeparatedReferences">Comma separated full names. When null or empty, nothing is added.</param>
    void AddCommaSeparatedString( string commaSeparatedReferences );
}
