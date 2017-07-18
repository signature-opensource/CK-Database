using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CKSetup
{
    public interface IComponentDBRemote
    {
        ComponentDB Download( IActivityMonitor monitor, TargetRuntime targetRuntime, IReadOnlyCollection<ComponentDependency> missingDependencies, IReadOnlyCollection<ComponentRef> missingEmbedded, ComponentDB db );
    }
}
