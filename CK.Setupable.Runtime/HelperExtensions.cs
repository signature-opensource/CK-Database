using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Extends <see cref="Attribute"/> type.
    /// </summary>
    public static class HelperExtensions
    {
        /// <summary>
        /// Gets the name of this attribute's type without Attribute suffix.
        /// </summary>
        /// <param name="this">This </param>
        /// <returns>The attribute's type name.</returns>
        public static string GetShortTypeName( this Attribute @this )
        {
            return @this != null ? @this.GetType().Name.Replace( "Attribute", "" ) : String.Empty;
        }

    }
}
