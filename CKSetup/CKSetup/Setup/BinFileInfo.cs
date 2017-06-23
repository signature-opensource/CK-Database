using CK.Core;
using CSemVer;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{

    public class BinFileInfo
    {
        BinFolder _binFolder;
        HashSet<BinFileInfo> _localDependencies;
        ComponentRef _cRef;

        BinFileInfo( string p, AssemblyDefinition a, IActivityMonitor m )
        {
            FullPath = p;
            InfoVersion = a.GetInformationalVersion();
            Name = a.Name;
            RawTargetFramework = a.CustomAttributes
                                        .Where( x => x.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute" && x.HasConstructorArguments )
                                        .Select( x => x.ConstructorArguments[0].Value as string )
                                        .FirstOrDefault();

            AssemblyReferences = a.MainModule.AssemblyReferences.ToArray();
            // SetupDependencies may need VersionName or RawTargetFramework.
            SetupDependencies = a.CustomAttributes
                                    .Select( x => (x.AttributeType.FullName == "CK.Setup.IsEngineAttribute"
                                                    ? new SetupDependency( this )
                                                    : ( x.AttributeType.FullName == "CK.Setup.IsModelThatUsesRuntimeAttribute"
                                                        ? new SetupDependency( true, x.ConstructorArguments, this )
                                                        : (x.AttributeType.FullName == "CK.Setup.IsRuntimeThatUsesEngineAttribute")
                                                            ? new SetupDependency( false, x.ConstructorArguments, this )
                                                            : null)) )
                                    .Where( x => x != null )
                                    .ToArray();
            bool multiple = false;
            if( SetupDependencies.Any( d => d.IsModel ) ) ComponentKind = ComponentKind.Model;
            if( SetupDependencies.Any( d => d.IsRuntime ) )
            {
                if( ComponentKind != ComponentKind.None ) multiple = true;
                ComponentKind = ComponentKind.Runtime;
            }
            if( SetupDependencies.Any( d => d.IsEngine ))
            {
                if( ComponentKind != ComponentKind.None ) multiple = true;
                ComponentKind = ComponentKind.Engine;
            }
            if( multiple )
            {
                throw new CKException( $"File '{p}' cannot be marked with more that one kind ot attributes (Engine, Runtime or Model)." );
            }
            if( ComponentKind != ComponentKind.None )
            {
               TargetFramework t;
               switch( RawTargetFramework )
                {
                    case null: throw new CKException( $"Component '{p}' must be marked with a TargetFrameworkAttribute." );
                    case ".NETFramework,Version=v4.6.1": t = TargetFramework.Net461; break;
                    case ".NETStandard,Version=v1.3": t = TargetFramework.NetStandard13; break;
                    case ".NETStandard,Version=v1.6": t = TargetFramework.NetStandard16; break;
                    default: throw new CKException( $"Component '{p}' has TargetFrameworkAttribute {RawTargetFramework} that is not currently handled." );
                }
                if( InfoVersion.OriginalInformationalVersion == null )
                {
                    InfoVersion = InformationalVersion.Invalid;
                    m.Warn().Send( $"Component '{p}' does not have a standard CSemVer version in its InformationalVersion. Using the ZeroVersion." );
                }
                else if( !InfoVersion.NuGetVersion.IsValidSyntax /*Should be !InfoVersion.IsValidSyntax */)
                {
                    throw new CKException( $"Component '{p}' standard CSemVer version error: {InfoVersion.NuGetVersion.ParseErrorMessage}." );
                }
                _cRef = new ComponentRef( t, Name.Name, InfoVersion.NuGetVersion );
            }
        }

        /// <summary>
        /// Gets the folder with all its binaries.
        /// The <see cref="LocalDependencies"/> is a subset of the <see cref="BinFolder.Files"/> set.
        /// </summary>
        public BinFolder BinFolder => _binFolder;

        /// <summary>
        /// Gets the full path of this BinFileInfo.
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// Gets the local fileName of this BinFileInfo.
        /// </summary>
        public string LocalFileName { get; private set; }

        /// <summary>
        /// Gets the assembly <see cref="AssemblyNameDefinition"/>.
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
        /// Gets the <see cref="ComponentKind"/>.
        /// </summary>
        public ComponentKind ComponentKind { get; }

        /// <summary>
        /// Gets the corresponding <see cref="ComponentRef"/> if this is a Component.
        /// </summary>
        public ComponentRef ComponentRef => _cRef;

        /// <summary>
        /// Gets the setup dependencies (from attributes named CK.Setup.SetupDependencyAttribute).
        /// </summary>
        public IReadOnlyList<SetupDependency> SetupDependencies { get; }

        /// <summary>
        /// Gets the list of <see cref="AssemblyNameReference"/>.
        /// </summary>
        public IReadOnlyCollection<AssemblyNameReference> AssemblyReferences { get; }

        /// <summary>
        /// Gets the recursive local dependencies.
        /// </summary>
        public IReadOnlyCollection<BinFileInfo> LocalDependencies => _localDependencies;

        /// <summary>
        /// Gets the best version name: "P&lt;CSemVer NuGetV2&gt;" first else fallbacks to "V&lt;Assembly version&gt;"
        /// </summary>
        public string GenericVersionName
        {
            get
            {
                return InfoVersion?.NuGetVersion?.IsValidSyntax == true
                        ? "P" + InfoVersion.NuGetVersion.Text
                        : "V" + Name.Version;
            }
        }

        internal HashSet<BinFileInfo> SetBinFolderAndUpdateLocalDependencies( BinFolder binFolder )
        {
            if( _binFolder == null )
            {
                _binFolder = binFolder;
                LocalFileName = FullPath.Substring( _binFolder.BinPath.Length );
                _localDependencies = new HashSet<BinFileInfo>();
                foreach( var dep in AssemblyReferences
                                        .Select( n => binFolder.Files.FirstOrDefault( b => b.Name.Name == n.Name ) )
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
            if( ComponentKind != ComponentKind.None ) s += $" ({ComponentKind})";
            return s;
        }

        static internal IReadOnlyList<BinFileInfo> ReadFiles( IActivityMonitor m, string binPath )
        {
            var result = new List<BinFileInfo>();
            ReaderParameters r = new ReaderParameters();
            foreach( var f in Directory.EnumerateFiles( binPath )
                                .Where( p => p.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase ) ) )
            {
                BinFileInfo info = TryRead( m, r, f );
                if( info != null ) result.Add( info );
            }
            return result;
        }

        static BinFileInfo TryRead( IActivityMonitor m, ReaderParameters r, string fullPath )
        {
            BinFileInfo info = null;
            try
            {
                using( AssemblyDefinition a = AssemblyDefinition.ReadAssembly( fullPath, r ) )
                {
                    info = new BinFileInfo( fullPath, a, m );
                }
            }
            catch( BadImageFormatException ex )
            {
                m.Warn().Send( ex, $"While analysing '{fullPath}'." );
            }
            return info;
        }
    }
}
