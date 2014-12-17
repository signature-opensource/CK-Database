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
    public class SetupCenterConfiguration
    {
        readonly List<Type> _regTypeList;
        readonly BuilderFinalAssemblyConfiguration _finalAssemblyConf;
        readonly BuilderAppDomainConfiguration _builderAppdomainConf;

        /// <summary>
        /// Initializes a new <see cref="SetupCenterConfiguration"/>.
        /// </summary>
        public SetupCenterConfiguration()
        {
            _regTypeList = new List<Type>();
            _finalAssemblyConf = new BuilderFinalAssemblyConfiguration();
            _builderAppdomainConf = new BuilderAppDomainConfiguration();
        }

        /// <summary>
        /// Gets the <see cref="BuilderFinalAssemblyConfiguration"/> that describes final assembly generation options.
        /// </summary>
        public BuilderFinalAssemblyConfiguration FinalAssemblyConfiguration
        {
            get { return _finalAssemblyConf; }
        }

        /// <summary>
        /// Gets the <see cref="BuilderAppDomainConfiguration"/> that describes the Application Domain related 
        /// options to use during setup phasis.
        /// </summary>
        public BuilderAppDomainConfiguration AppDomainConfiguration
        {
            get { return _builderAppdomainConf; }
        }

        /// <summary>
        /// Gets a list of class types that will be explicitely registered (even if they belong to
        /// an assembly that is not discovered or appears in <see cref="AssemblyRegistererConfiguration.IgnoredAssemblyNames"/>).
        /// </summary>
        public IList<Type> ExplicitRegisteredClasses
        {
            get { return _regTypeList; }
        }

        /// <summary>
        /// Gets ors sets whether the ordering for setupable items that share the same rank in the pure dependency graph must be inverted.
        /// Defaults to false. (See <see cref="DependencySorter"/> for more information.)
        /// </summary>
        public bool RevertOrderingNames { get; set; }

        /// <summary>
        /// Optional filter for types.
        /// When null (the default), all types of assemblies resulting of <see cref="AssemblyRegistererConfiguration"/> are kept.
        /// </summary>
        public Predicate<Type> TypeFilter { get; set; }

    }
}
