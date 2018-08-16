using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Describes the parameters assignments that are required to call
    /// the constructor of a Service class Type.
    /// This is a recursive model that acts as a blueprint: it must be
    /// adapted to the DI container capabilities.
    /// </summary>
    public interface IStObjServiceClassFactoryInfo
    {
        /// <summary>
        /// Gets the actual Type that must be instanciated.
        /// This Type has, by design, one and only one public constructor.
        /// </summary>
        Type ClassType { get; }

        /// <summary>
        /// Gets the set of parameters assignments of the single <see cref="ClassType"/>'s
        /// public constructor that must be explicitly provided in order to successfully
        /// call the constructor.
        /// Only parameters that require a <see cref="IStObjServiceParameterInfo"/> appear
        /// in this list.
        /// </summary>
        IReadOnlyList<IStObjServiceParameterInfo> Assignments { get; }
    }

}
