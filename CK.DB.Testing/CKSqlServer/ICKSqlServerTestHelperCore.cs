using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Testing
{
    /// <summary>
    /// Support sql database related helpers.
    /// </summary>
    public interface ICKSqlServerTestHelperCore 
    {
        /// <summary>
        /// Gets the schema names used from "SqlServer/UsedSchemas" comma separated names configuration.
        /// If this list is not empty, the schemas "CK" and "CKCore" are added.
        /// </summary>
        IReadOnlyList<string> UsedSchemas { get; }
    }
}
