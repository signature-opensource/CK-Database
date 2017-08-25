using System.Xml.Linq;

namespace CK.Core
{
    /// <summary>
    /// All configuration of a Engine Aspect must implement this interface.
    /// Aspect configuration must have a deserialization constructor that take a XElement.
    /// </summary>
    /// <remarks>
    /// Any <see cref="Type"/> or delegates of any kind should be avoided (ie. any kind 
    /// of stuff that can not be serialized).
    /// </remarks>
    public interface IStObjEngineAspectConfiguration
    {
        /// <summary>
        /// Gets the fully qualified name of the class that implements this aspect.
        /// </summary>
        string AspectType { get; }

        /// <summary>
        /// Serializes its content in the provided <see cref="XElement"/> and returns it.
        /// The dedicated constructor will be able to read this element back.
        /// </summary>
        /// <param name="e">The element to populate.</param>
        /// <returns>The <paramref name="e"/> element.</returns>
        XElement SerializeXml( XElement e );

    }
}
