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
        public ComponentMissingDescription( TargetRuntime t, IReadOnlyCollection<ComponentDependency> d, IReadOnlyCollection<ComponentRef> c )
        {
            TargetRuntime = t;
            Dependencies = d ?? Array.Empty<ComponentDependency>();
            Components = c ?? Array.Empty<ComponentRef>();
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
