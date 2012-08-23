using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Describes a parameter of a Construct method.
    /// </summary>
    public interface IMutableParameterType : IMutableReferenceType
    {
        /// <summary>
        /// Gets the parameter position in the list.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets whether this parameter is optional (ie. can be null).
        /// Defaults to false: a parameter instance should be found unless explicitely said as optionnal.
        /// </summary>
        bool IsOptional { get; set; }

    }
}
