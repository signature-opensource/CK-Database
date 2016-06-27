using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// Defines a transformer.
    /// This is an interface rather than a base class to let actual transformers specialize
    /// existing base class.
    /// </summary>
    public interface ISetupObjectTransformerItem : IMutableSetupObjectItem
    {
        /// <summary>
        /// Gets the source object for this transformer.
        /// This should be set only by <see cref="SetupObjectItem.AddTransformer(ISetupObjectTransformerItem)"/>.
        /// </summary>
        SetupObjectItem Source { get; set; }

        /// <summary>
        /// Gets the target transformed object.
        /// This should be set only by <see cref="SetupObjectItem.AddTransformer(ISetupObjectTransformerItem)"/>.
        /// </summary>
        SetupObjectItem Target { get; set; }

    }
}
