namespace CK.Setup
{

    /// <summary>
    /// Base class for attributes declared on a class that define dynamically created setup objects.
    /// Multiples object names like "sUserCreate, sUserDestroy, AnotherSchema.sUserUpgrade, CK.sUserRun" may be defined:
    /// this is up to the specialized attribute and its implementation to actually set the maximum number of allowed names.
    /// </summary>
    public abstract class SetupObjectItemAttributeBase : Setup.ContextBoundDelegationAttribute
    {
        /// <summary>
        /// Initializes a new <see cref="SetupObjectItemAttributeBase"/> with (potentially) multiple object names.
        /// </summary>
        /// <param name="commaSeparatedObjectNames">Name or multiple comma separated names.</param>
        /// <param name="actualAttributeTypeAssemblyQualifiedName">Assembly Qualified Name of the object that will replace this attribute during setup.</param>
        public SetupObjectItemAttributeBase( string commaSeparatedObjectNames, string actualAttributeTypeAssemblyQualifiedName )
            : base( actualAttributeTypeAssemblyQualifiedName )
        {
            NameOrCommaSeparatedObjectNames = commaSeparatedObjectNames;
        }

        /// <summary>
        /// Gets a object name (or multiple comma separated names if the implementation allows it).
        /// Implementor should use the name of the required constructor parameter (and comments of course) to 
        /// specify this i.e.: 'name' or 'commaSeparatedObjectNames'.
        /// </summary>
        public string NameOrCommaSeparatedObjectNames { get; }
    }
}
