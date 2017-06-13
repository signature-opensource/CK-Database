using CK.Core;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{

    class BinFileInfo
    {
        HashSet<BinFileInfo> _localDependencies;

        BinFileInfo( string p, AssemblyDefinition a )
        {
            FullPath = p;
            CKVersion = a.GetCKVersion();
            Name = a.Name;
            RawTargetFramework = a.CustomAttributes
                                        .Where( x => x.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute" && x.HasConstructorArguments )
                                        .Select( x => x.ConstructorArguments[0].Value as string )
                                        .FirstOrDefault();

            AssemblyReferences = a.MainModule.AssemblyReferences.ToArray();
            // SetupDependencies may need VersionName or RawTargetFramework.
            SetupDependencies = a.CustomAttributes
                                    .Where( x => x.AttributeType.FullName == "CK.Setup.SetupDependencyAttribute" && x.HasConstructorArguments )
                                    .Select( x => new SetupDependency( x.ConstructorArguments, this ) )
                                    .Where( x => x.IsValid )
                                    .ToArray();

        }

        /// <summary>
        /// Gets the full path of this BinFileInfo.
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// Gets the filaName of this BinFileInfo.
        /// </summary>
        public string FileName => Path.GetFileName( FullPath );

        /// <summary>
        /// Gets the assembly name.
        /// </summary>
        public AssemblyNameDefinition Name { get; }

        /// <summary>
        /// Gets the CKVersion info if found.
        /// </summary>
        public CKVersionInfo CKVersion { get; }

        /// <summary>
        /// Gets the TargetFramework if <see cref="System.Runtime.Versioning.TargetFrameworkAttribute"/>
        /// exists on the assembly.
        /// </summary>
        public string RawTargetFramework { get; }

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
        /// Gets whether at least one of the <see cref="LocalDependencies"/> depends on an assembly
        /// that has one or more associated <see cref="SetupDependency"/>: this is a "Model".
        /// </summary>
        public bool IsRuntimeDependencyDependent { get; private set; }

        /// <summary>
        /// Gets the best version name: "P&lt;CSemVer NuGetV2&gt;" first else fallbacks to "V&lt;Assembly version&gt;"
        /// </summary>
        public string VersionName
        {
            get
            {
                return CKVersion?.Version?.IsValid == true
                        ? "P" + CKVersion.Version.ToString( CSemVer.CSVersionFormat.NugetPackageV2 )
                        : "V" + Name.Version;
            }
        }

        /// <summary>
        /// Gets the zip entry path: the assembly name\target framework\<see cref="VersionName"/>.
        /// </summary>
        public string ZipEntryPath => Name.Name + '\\' + RawTargetFramework + "\\" + VersionName;

        /// <summary>
        /// Gets the zip entry name: the <see cref="ZipEntryPath"/>\<see cref="FileName"/>.
        /// </summary>
        public string ZipEntryName => ZipEntryPath + "\\" + FileName;

        HashSet<BinFileInfo> UpdateLocalDependencies( IEnumerable<BinFileInfo> binFolder )
        {
            if( _localDependencies == null )
            {
                _localDependencies = new HashSet<BinFileInfo>();
                foreach( var dep in AssemblyReferences
                                     .Select( n => binFolder.FirstOrDefault( b => b.Name.Name == n.Name ) )
                                     .Where( b => b != null ) )
                {
                    if( _localDependencies.Add( dep ) )
                    {
                        _localDependencies.UnionWith( dep.UpdateLocalDependencies( binFolder ) );
                    }
                    IsRuntimeDependencyDependent |= dep.IsRuntimeDependencyDependent;
                    if( !IsRuntimeDependencyDependent )
                    {
                        IsRuntimeDependencyDependent = dep.SetupDependencies.Count > 0;
                    }
                }
            }
            return _localDependencies;
        }

        static public IReadOnlyList<BinFileInfo> ReadBinFolder( IActivityMonitor m, string binPath )
        {
            var result = new List<BinFileInfo>();
            ReaderParameters r = new ReaderParameters();
            foreach( var f in Directory.EnumerateFiles( binPath )
                                .Where( p => p.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase ) ) )
            {
                BinFileInfo info = TryRead( m, r, f );
                if( info != null ) result.Add( info );
            }
            foreach( var b in result )
            {
                b.UpdateLocalDependencies( result );
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
                    info = new BinFileInfo( fullPath, a );
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
