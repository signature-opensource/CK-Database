using CK.Core;
using CSemVer;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{
    /// <summary>
    /// Captures assembly related information when a <see cref="BinFileInfo"/> is an assembly.
    /// </summary>
    public class BinFileAssemblyInfo : BinFileInfo
    {
        HashSet<BinFileAssemblyInfo> _localDependencies;
        ComponentRef _cRef;

        internal BinFileAssemblyInfo( string fullPath, string localFileName, int len, AssemblyDefinition a, IActivityMonitor m )
            : base( fullPath, localFileName, len )
        {
            InfoVersion = a.GetInformationalVersion();
            Name = a.Name;
            RawTargetFramework = a.CustomAttributes
                                        .Where( x => x.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute" && x.HasConstructorArguments )
                                        .Select( x => x.ConstructorArguments[0].Value as string )
                                        .FirstOrDefault();

            AssemblyReferences = a.MainModule.AssemblyReferences.ToArray();
            SetupDependencies = a.CustomAttributes
                                    .Select( x => (x.AttributeType.FullName == "CK.Setup.RequiredSetupDependencyAttribute"
                                                        ? new SetupDependency( x.ConstructorArguments, this )
                                                        : null) )
                                    .Where( x => x != null )
                                    .ToArray();
            bool isSetupDependency = false, isModel = false;
            foreach( var attr in a.CustomAttributes )
            {
                if( attr.AttributeType.FullName == "CK.Setup.IsSetupDependencyAttribute" )
                {
                    isSetupDependency = true;
                }
                else if( attr.AttributeType.FullName == "CK.Setup.IsModelAttribute" )
                {
                    isModel = true;
                }
            }
            if( isModel && isSetupDependency )
            {
                throw new CKException( $"Component '{localFileName}' is marked with both IsModel and IsSetupDependency attribute." );
            }
            if( isSetupDependency )
            {
                ComponentKind = ComponentKind.SetupDependency;
            }
            else if( isModel )
            {
                ComponentKind = ComponentKind.Model;
            }
            else if( SetupDependencies.Count > 0 )
            {
                throw new CKException( $"Component '{localFileName}' has at least one RequiredSetupDepency attribute. It must also be marked with IsModel or IsSetupDependency attribute." );
            }
            if( ComponentKind != ComponentKind.None )
            {
                TargetFramework t = TargetRuntimeOrFrameworkExtension.TryParse( RawTargetFramework );
                if( t == TargetFramework.None )
                {
                    if( RawTargetFramework == null )
                    {
                        throw new CKException( $"Component '{localFileName}' must be marked with a TargetFrameworkAttribute." );
                    }
                    throw new CKException( $"Component '{localFileName}' has TargetFrameworkAttribute {RawTargetFramework} that is invalid or not currently handled." );
                }
                if( InfoVersion.OriginalInformationalVersion == null )
                {
                    InfoVersion = InformationalVersion.Zero;
                    m.Warn( $"Component '{localFileName}' does not have a standard CSemVer version in its InformationalVersion. Using the ZeroVersion." );
                }
                else if( !InfoVersion.IsValidSyntax )
                {
                    throw new CKException( $"Component '{localFileName}' standard CSemVer version error: {InfoVersion.ParseErrorMessage}." );
                }
                foreach( var d in SetupDependencies ) d.OnSourceVersionKnown( InfoVersion.NuGetVersion );
                _cRef = new ComponentRef( Name.Name, t, InfoVersion.NuGetVersion );
            }
        }

        /// <summary>
        /// Gets the <see cref="ComponentKind"/>. Can be <see cref="ComponentKind.None"/>.
        /// </summary>
        public ComponentKind ComponentKind { get; }

        /// <summary>
        /// Gets whether files should be stored: only Models in .Net framework don't need to be stored.
        /// </summary>
        public bool StoreFiles => ComponentKind == ComponentKind.SetupDependency
                                    || (ComponentKind == ComponentKind.Model || !_cRef.TargetFramework.IsNetFramework());

        /// <summary>
        /// Gets the assembly <see cref="AssemblyNameDefinition"/>.
        /// Null if this file is not an assembly.
        /// </summary>
        public AssemblyNameDefinition Name { get; }

        /// <summary>
        /// Gets the CKVersion info if found.
        /// </summary>
        public InformationalVersion InfoVersion { get; }

        /// <summary>
        /// Gets the TargetFramework if <see cref="System.Runtime.Versioning.TargetFrameworkAttribute"/>
        /// exists on the assembly.
        /// </summary>
        public string RawTargetFramework { get; }

        /// <summary>
        /// Gets the corresponding <see cref="ComponentRef"/> if this is a Component.
        /// </summary>
        public ComponentRef ComponentRef => _cRef;

        /// <summary>
        /// Gets the setup dependencies (from attributes named CK.Setup.RequiredSetupDependencyAttribute).
        /// </summary>
        public IReadOnlyList<SetupDependency> SetupDependencies { get; }

        /// <summary>
        /// Gets the list of <see cref="AssemblyNameReference"/>.
        /// </summary>
        public IReadOnlyCollection<AssemblyNameReference> AssemblyReferences { get; }

        /// <summary>
        /// Gets the recursive local dependencies.
        /// </summary>
        public IReadOnlyCollection<BinFileAssemblyInfo> LocalDependencies => _localDependencies;

        internal override HashSet<BinFileAssemblyInfo> SetBinFolderAndUpdateLocalDependencies( BinFolder binFolder )
        {
            base.SetBinFolderAndUpdateLocalDependencies( binFolder );
            if( _localDependencies == null )
            {
                _localDependencies = new HashSet<BinFileAssemblyInfo>();
                foreach( var dep in AssemblyReferences
                                        .Select( n => binFolder.Assemblies.FirstOrDefault( b => b.Name.Name == n.Name ) )
                                        .Where( b => b != null ) )
                {
                    if( _localDependencies.Add( dep ) )
                    {
                        _localDependencies.UnionWith( dep.SetBinFolderAndUpdateLocalDependencies( binFolder ) );
                    }
                }
            }
            return _localDependencies;
        }

        public override string ToString()
        {
            string s = Name.FullName;
            if(ComponentKind != ComponentKind.None ) s += $" ({ComponentKind})";
            return s;
        }


}
}
