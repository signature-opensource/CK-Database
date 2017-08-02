using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CK.Core
{
    /// <summary>
    /// Defines which assemblies and types must be discovered.
    /// </summary>
    public class BuildAndRegisterConfiguration
    {
        readonly AssemblyRegistererConfiguration _assemblyRegister;
        readonly List<string> _explicitClasses;

        /// <summary>
        /// Initialize a new <see cref="BuildAndRegisterConfiguration"/>.
        /// </summary>
        public BuildAndRegisterConfiguration()
        {
            _assemblyRegister = new AssemblyRegistererConfiguration();
            _explicitClasses = new List<string>();
        }

        static readonly XName xAssemblyRegistererConfiguration = XNamespace.None + "AssemblyRegistererConfiguration";
        static readonly XName xExplicitClass = XNamespace.None + "ExplicitClass";

        /// <summary>
        /// Initializes a new <see cref="BuildAndRegisterConfiguration"/> from a <see cref="XElement"/>.
        /// </summary>
        /// <param name="e">The xml element.</param>
        /// <param name="version">The element format version.</param>
        public BuildAndRegisterConfiguration( XElement e, int version )
        {
            _assemblyRegister = new AssemblyRegistererConfiguration( e.Element( xAssemblyRegistererConfiguration ) );
            _explicitClasses = e.Elements( xExplicitClass ).Select( c => c.Value ).ToList();
        }

        /// <summary>
        /// Serializes its content in the provided <see cref="XElement"/> and returns it.
        /// The <see cref="BuildAndRegisterConfiguration(XElement,int)"/> constructor will be able to read this element back.
        /// </summary>
        /// <param name="e">The element to populate.</param>
        /// <returns>The <paramref name="e"/> element.</returns>
        public XElement SerializeXml( XElement e )
        {
            e.Add( _assemblyRegister.SerializeXml( new XElement( xAssemblyRegistererConfiguration ) ),
                   _explicitClasses.Select( c => new XElement( xExplicitClass, c ) ) );
            return e;
        }

        /// <summary>
        /// Gets the <see cref="AssemblyRegistererConfiguration"/> that describes assemblies that must 
        /// participate (or not) to setup.
        /// </summary>
        public AssemblyRegistererConfiguration Assemblies => _assemblyRegister;

        /// <summary>
        /// List of assembly qualified type names that must be explicitely registered 
        /// regardless of <see cref="Assemblies"/>.
        /// </summary>
        public List<string> ExplicitClasses => _explicitClasses;

    }
}
