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
    public interface ISetupObjectTransformerItem : IMutableSetupBaseItem
    {
        /// <summary>
        /// Gets the source object for this transformer.
        /// This should be set only by dedicated methods like <see cref="SetupObjectItem.AddTransformer"/>.
        /// </summary>
        IMutableSetupBaseItem Source { get; set; }

        /// <summary>
        /// Gets the target transformed object.
        /// This should be set only by dedicated methods like <see cref="SetupObjectItem.AddTransformer"/>.
        /// </summary>
        IMutableSetupBaseItem Target { get; set; }

    }
}
