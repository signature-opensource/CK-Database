using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Extends <see cref="IStObjServiceClassFactoryInfo"/>.
    /// </summary>
    public static class StObjServiceClassFactoryInfoExtension
    {
        /// <summary>
        /// Gets the single constructor information of the type.
        /// Currently, there must be one and only one public constructor.
        /// If it happens that other public constructors are needed, this function
        /// will filter the constructor to use with an attribute, however for the moment,
        /// it retruns the linq Single() one.
        /// </summary>
        /// <param name="this">This service class factory info.</param>
        /// <returns>The single constructor to consider.</returns>
        public static ConstructorInfo GetSingleConstructor( this IStObjServiceClassFactoryInfo @this )
        {
            return @this.ClassType.GetConstructors().Single();
        }
    }
}
