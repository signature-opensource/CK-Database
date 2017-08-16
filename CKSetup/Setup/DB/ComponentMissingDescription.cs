using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using System.IO;
using System.Threading;
using System.Xml.Linq;

namespace CKSetup
{
    public class ComponentMissingDescription
    {
        /// <summary>
        /// Initializes a <see cref="ComponentMissingDescription"/> with dependencies and explicit components.
        /// </summary>
        /// <param name="t">Target runtime for dependencies.</param>
        /// <param name="d">Dependencies. Can be null.</param>
        /// <param name="c">Explicit missing components. Can be null.</param>
        public ComponentMissingDescription( TargetRuntime t, IReadOnlyCollection<ComponentDependency> d, IReadOnlyCollection<ComponentRef> c )
        {
            TargetRuntime = t;
            Dependencies = d ?? Array.Empty<ComponentDependency>();
            Components = c ?? Array.Empty<ComponentRef>();
        }

        /// <summary>
        /// Initializes a <see cref="ComponentMissingDescription"/> only with explicit missing components.
        /// </summary>
        /// <param name="c">Explicit missing components. Can not be null.</param>
        public ComponentMissingDescription( IReadOnlyCollection<ComponentRef> components )
        {
            if( components == null ) throw new ArgumentNullException( nameof( components ) );
            TargetRuntime = TargetRuntime.None;
            Dependencies = Array.Empty<ComponentDependency>();
            Components = components;
        }

        /// <summary>
        /// Initializes a <see cref="ComponentMissingDescription"/> from a XElement.
        /// </summary>
        /// <param name="e">Xml element. Can not be null.</param>
        public ComponentMissingDescription( XElement e )
        {
            TargetRuntime = e.AttributeEnum<TargetRuntime>( DBXmlNames.Runtime, TargetRuntime.None );
            Dependencies = e.Elements( DBXmlNames.Dependency ).Select( d => new ComponentDependency( d ) ).ToList();
            Components = e.Elements( DBXmlNames.Ref ).Select( d => new ComponentRef( d ) ).ToList();
        }

        /// <summary>
        /// Creates a xml representation of this <see cref="ComponentMissingDescription"/>.
        /// </summary>
        /// <returns>The XElement.</returns>
        public XElement ToXml()
        {
            return new XElement( DBXmlNames.Missing, 
                                    new XAttribute( DBXmlNames.Runtime, TargetRuntime.ToString() ),
                                    Dependencies.Select( d => d.ToXml() ),
                                    Components.Select( c => c.ToXml() ) );
        }

        /// <summary>
        /// Target runtime for <see cref="Dependencies"/>.
        /// </summary>
        public TargetRuntime TargetRuntime { get; }

        /// <summary>
        /// Dependencies to resolve according to the <see cref="TargetRuntime"/>.
        /// Never null.
        /// </summary>
        public IReadOnlyCollection<ComponentDependency> Dependencies { get; }

        /// <summary>
        /// Explicit components missing.
        /// Never null.
        /// </summary>
        public IReadOnlyCollection<ComponentRef> Components { get; }

    }

}
