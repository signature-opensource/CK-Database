using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup.SqlServer
{
    /// <summary>
    /// Declares a resource that contains a Sql procedure, function or view associated to a type.
    /// Multiples object names like "sUserCreate, sUserDestroy, sUserUpgrade" can be defined.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class SqlObjectItemAttribute : AmbientContextBoundDelegationAttribute
    {
        /// <summary>
        /// Initializes a new <see cref="SqlObjectItemAttribute"/> with (potentially) multiple object names.
        /// </summary>
        /// <param name="commaSeparatedObjectNames">Name or multiple comma separated names.</param>
        public SqlObjectItemAttribute( string commaSeparatedObjectNames )
            : base( "CK.Setup.SqlServer.SqlObjectItemAttributeImpl, CK.Setup.SqlServer" )
        {
            CommaSeparatedObjectNames = commaSeparatedObjectNames;
        }

        /// <summary>
        /// Gets a Sql object name or multiple comma separated names.
        /// </summary>
        public string CommaSeparatedObjectNames { get; private set; }
    }
}
