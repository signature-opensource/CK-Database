#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\Configuration\SetupCenterConfiguration.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using CK.Core;
using CK.Setup;

namespace CK.Setup
{
    [Serializable]
    public class SetupEngineConfiguration : IStObjBuilderConfiguration
    {
        readonly StObjEngineConfiguration _stObjConfig;
        readonly List<ISetupEngineAspectConfiguration> _aspects;

        /// <summary>
        /// Initializes a new <see cref="SetupEngineConfiguration"/>.
        /// </summary>
        public SetupEngineConfiguration()
        {
            _stObjConfig = new StObjEngineConfiguration();
            _aspects = new List<ISetupEngineAspectConfiguration>();
        }

        /// <summary>
        /// Gets ors sets whether the ordering for setupable items that share the same rank in the pure dependency graph must be inverted.
        /// Defaults to false. (See <see cref="DependencySorter"/> for more information.)
        /// </summary>
        public bool RevertOrderingNames { get; set; }

        /// <summary>
        /// Gets or sets whether the dependency graph (the set of IDependentItem) must be send to the monitor before sorting.
        /// Defaults to false.
        /// </summary>
        public bool TraceDependencySorterInput { get; set; }

        /// <summary>
        /// Gets whether the dependency graph (the set of ISortedItem) must be send to the monitor once the graph is sorted.
        /// Defaults to false.
        /// </summary>
        public bool TraceDependencySorterOutput { get; set; }

        /// <summary>
        /// Gets the <see cref="StObjEngineConfiguration"/> object.
        /// </summary>
        public StObjEngineConfiguration StObjEngineConfiguration
        {
            get { return _stObjConfig; }
        }

        /// <summary>
        /// Gets the list of all configuration aspects that must participate to setup.
        /// </summary>
        public List<ISetupEngineAspectConfiguration> Aspects 
        {
            get { return _aspects; } 
        }

        string IStObjBuilderConfiguration.BuilderAssemblyQualifiedName
        {
            get { return "CK.Setup.SetupEngine, CK.Setupable.Engine"; }
        }
    }
}
