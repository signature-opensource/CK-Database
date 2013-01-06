using System;
using System.Collections.Generic;
using CK.Core;
using CK.Setup;

namespace CK.Setup
{
    public class SetupCenterConfiguration
    {
        readonly AssemblyRegistererConfiguration _regConf;
        readonly List<Type> _regTypeList;
        readonly StObjFinalAssemblyConfiguration _finalAssemblyConf;

        /// <summary>
        /// Initializes a new <see cref="SetupCenterConfiguration"/>.
        /// </summary>
        public SetupCenterConfiguration()
        {
            _regConf = new AssemblyRegistererConfiguration();
            _regTypeList = new List<Type>();
            _finalAssemblyConf = new StObjFinalAssemblyConfiguration();
        }

        /// <summary>
        /// Gets the <see cref="AssemblyRegistererConfiguration"/> that describes assemblies that must participate (or not) to setup.
        /// </summary>
        public AssemblyRegistererConfiguration AssemblyRegistererConfiguration
        {
            get { return _regConf; }
        }

        /// <summary>
        /// Gets the <see cref="StObjFinalAssemblyConfiguration"/> that describes final assembly generation options.
        /// </summary>
        public StObjFinalAssemblyConfiguration StObjFinalAssemblyConfiguration
        {
            get { return _finalAssemblyConf; }
        }

        /// <summary>
        /// Gets a list of class types that will be explicitely registered (even if they belong to
        /// a assembly that is not discovered or appears in <see cref="AssemblyRegistererConfiguration.IgnoredAssemblyNames"/>).
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
