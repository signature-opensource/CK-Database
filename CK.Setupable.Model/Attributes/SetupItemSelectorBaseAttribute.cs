using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Declares a dynamic handler associated to the object.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public abstract class SetupItemSelectorBaseAttribute : AmbientContextBoundDelegationAttribute
    {
        /// <summary>
        /// Initializes a new <see cref="SetupItemSelectorBaseAttribute"/> with (potentially) multiple item names.
        /// </summary>
        /// <param name="commaSeparatedTypeNames">Name or multiple comma separated names.</param>
        protected SetupItemSelectorBaseAttribute( string actualAttributeTypeAssemblyQualifiedName, string commaSeparatedTypeNames, SetupItemSelectorScope scope )
            : base( actualAttributeTypeAssemblyQualifiedName )
        {
            if( scope == Setup.SetupItemSelectorScope.None ) throw new ArgumentException( "scope" ); 
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
}
