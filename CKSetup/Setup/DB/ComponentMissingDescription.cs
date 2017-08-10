using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using System.IO;
using System.Threading;

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
