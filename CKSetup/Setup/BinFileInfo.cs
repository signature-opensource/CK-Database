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
        SHA1Value _sha1;

        BinFileInfo( string p, int len, AssemblyDefinition a, IActivityMonitor m )
        {
            FullPath = p;
            FileLength = len;
            _sha1 = SHA1Value.ZeroSHA1;
            InfoVersion = a.GetInformationalVersion();
            Name = a.Name;
            RawTargetFramework = a.CustomAttributes
                                        .Where( x => x.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute" && x.HasConstructorArguments )
                                        .Select( x => x.ConstructorArguments[0].Value as string )
                                        .FirstOrDefault();

            AssemblyReferences = a.MainModule.AssemblyReferences.ToArray();
            // SetupDependencies may need VersionName or RawTargetFramework.
            IsExcludedFromSetup = a.CustomAttributes.Any( x => x.AttributeType.FullName == "CK.Setup.ExcludeFromSetupAttribute" );
            if( IsExcludedFromSetup )
            {
                SetupDependencies = Array.Empty<SetupDependency>();
                m.Debug( $"'{Name.Name}' has ExcludeFromSetup attribute." );
            }
            else 
            {
                bool tooMuchKind = false;
                bool isEngine = a.CustomAttributes.Any( x => x.AttributeType.FullName == "CK.Setup.IsEngineAttribute" );
                if( isEngine )
                {
                    ComponentKind = ComponentKind.Engine;
                }
                bool isRuntime = a.CustomAttributes.Any( x => x.AttributeType.FullName == "CK.Setup.IsRuntimeAttribute" );
                if( isRuntime )
                {
                    if( ComponentKind != ComponentKind.None ) tooMuchKind = true;
                    ComponentKind = ComponentKind.Runtime;
                }
                SetupDependencies = a.CustomAttributes
                                        .Select( x => (x.AttributeType.FullName == "CK.Setup.IsModelThatUsesRuntimeAttribute"
                                                            ? new SetupDependency( true, x.ConstructorArguments, this )
                                                            : (x.AttributeType.FullName == "CK.Setup.IsRuntimeThatUsesEngineAttribute")
                                                                  ? new SetupDependency( false, x.ConstructorArguments, this )
                                                                  : null) )
                                        .Where( x => x != null )
                                        .ToArray();
                if( !tooMuchKind && SetupDependencies.Any( d => d.IsModel ) )
                {
                    if( ComponentKind != ComponentKind.None ) tooMuchKind = true;
                    ComponentKind = ComponentKind.Model;
                }
                if( !tooMuchKind && SetupDependencies.Any( d => d.IsRuntime ) )
                {
                    if( ComponentKind != ComponentKind.None && ComponentKind != ComponentKind.Runtime ) tooMuchKind = true;
                    ComponentKind = ComponentKind.Runtime;
                }
                if( tooMuchKind )
                {
                    throw new CKException( $"File '{p}' cannot be marked with more that one kind ot attributes (Engine, Runtime or Model)." );
                }
            }
            if( ComponentKind != ComponentKind.None )
            {
                TargetFramework t = TargetRuntimeOrFrameworkExtension.TryParse( RawTargetFramework );
                if( t == TargetFramework.None )
                {
                    if( RawTargetFramework == null )
                    {
                        throw new CKException( $"Component '{p}' must be marked with a TargetFrameworkAttribute." );
                    }
                    throw new CKException( $"Component '{p}' has TargetFrameworkAttribute {RawTargetFramework} that is invalid or not currently handled." );
                }
                if( InfoVersion.OriginalInformationalVersion == null )
                {
                    InfoVersion = InformationalVersion.Zero;
                    m.Warn( $"Component '{p}' does not have a standard CSemVer version in its InformationalVersion. Using the ZeroVersion." );
                }
                else if( !InfoVersion.IsValidSyntax )
                {
                    throw new CKException( $"Component '{p}' standard CSemVer version error: {InfoVersion.ParseErrorMessage}." );
                }
                foreach( var d in SetupDependencies ) d.OnSourceVersionKnown( InfoVersion.NuGetVersion );
                _cRef = new ComponentRef(Name.Name, t, InfoVersion.NuGetVersion);
            }
        }

        /// <summary>
        /// Gets whether this file is marked with CK.Setup.ExcludeFromSetupAttribute.
        /// </summary>
        public bool IsExcludedFromSetup { get; }

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
        /// Gets the file length in bytes.
        /// There is no need/interest to handle files bigger than 2GB here.
        /// </summary>
        public int FileLength { get; }

        /// <summary>
        /// Get the SHA1 of the file (file is loaded the first time and only once).
        /// </summary>
        public SHA1Value ContentSHA1 => _sha1.IsZero ? (_sha1 = SHA1Value.ComputeFileSHA1( FullPath )) : _sha1;

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
                                .Where( p => p.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase ) 
                                             || p.EndsWith( ".exe", StringComparison.OrdinalIgnoreCase ) ) )
            {
                BinFileInfo info = TryRead( m, r, f );
                if( info != null ) result.Add( info );
            }
            return result;
        }

        static BinFileInfo TryRead( IActivityMonitor m, ReaderParameters r, string fullPath )
        {
            BinFileInfo info = null;

            var fi = new FileInfo( fullPath );
            long len = fi.Length;
            if( len > Int32.MaxValue )
            {
                m.Warn( $"'{fullPath}' is bigger than 2 GiB. It will be ignored." );
            }
            else if( len == 0 )
            {
                m.Warn( $"'{fullPath}' is an empty file. It will be ignored." );
            }
            else
            {
                try
                {
                    // Mono.Cecil requires the stream to be seekable. Pity :)
                    //   using( var file = new FileStream( fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan ) )
                    //   using( var shaCompute = new SHA1Stream( file, true, true ) )
                    //   using( AssemblyDefinition a = AssemblyDefinition.ReadAssembly( shaCompute, r ) )
                    using( AssemblyDefinition a = AssemblyDefinition.ReadAssembly( fullPath, r ) )
                    {
                        info = new BinFileInfo( fullPath, (int)len, a, m );
                    }
                }
                catch( BadImageFormatException ex )
                {
                    m.Warn( $"While analysing '{fullPath}'.", ex );
                }
            }
            return info;
        }
    }
}
