using System;
using CK.Core;

namespace CK.Setup;

/// <summary>
/// Base class to declare a dynamic handler associated to the object.
/// </summary>
public abstract class SetupItemSelectorBaseAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new <see cref="SetupItemSelectorBaseAttribute"/> with (potentially) multiple item names.
    /// </summary>
    /// <param name="actualAttributeTypeAssemblyQualifiedName">Concrete type name (provided by the specialized class).</param>
    /// <param name="commaSeparatedTypeNames">Name or multiple comma separated names.</param>
    /// <param name="scope">Defines the scope to which this attribute applies.</param>
    protected SetupItemSelectorBaseAttribute( string actualAttributeTypeAssemblyQualifiedName, string commaSeparatedTypeNames, SetupItemSelectorScope scope )
        : base( actualAttributeTypeAssemblyQualifiedName )
    {
        if( scope == SetupItemSelectorScope.None ) throw new ArgumentException( nameof( scope ) );
        CommaSeparatedTypeNames = commaSeparatedTypeNames;
        SetupItemSelectorScope = scope;
    }

    /// <summary>
    /// Gets the multiple comma separated names.
    /// </summary>
    public string CommaSeparatedTypeNames { get; private set; }

    /// <summary>
    /// Gets the scope where items are selected.
    /// </summary>
    public SetupItemSelectorScope SetupItemSelectorScope { get; private set; }


}
