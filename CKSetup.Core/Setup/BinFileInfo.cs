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
        SHA1Value _sha1;

        protected BinFileInfo( string fullPath, string localName, int len )
        {
            FullPath = fullPath;
            FileLength = len;
            _sha1 = SHA1Value.ZeroSHA1;
            LocalFileName = localName;
            var info = FileVersionInfo.GetVersionInfo( fullPath );
            FileVersion = new Version( info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart );
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
        public string LocalFileName { get; }

        /// <summary>
        /// Gets the file length in bytes.
        /// There is no need/interest to handle files bigger than 2GB here.
        /// </summary>
        public int FileLength { get; }

        /// <summary>
        /// Gets the file version from the <see cref="FileVersionInfo"/> if the file has a PE header with a VERSIONINFO.
        /// Null otherwise.
        /// </summary>
        public Version FileVersion { get; }

        /// <summary>
        /// Gets the .Net assembly version if this file is a <see cref="BinFileAssemblyInfo"/> and the version exists.
        /// Null otherwise.
        /// </summary>
        public virtual Version AssemblyVersion => null;

        /// <summary>
        /// Get the SHA1 of the file (file is loaded the first time and only once).
        /// </summary>
        public SHA1Value ContentSHA1 => _sha1.IsZero ? (_sha1 = SHA1Value.ComputeFileSHA1( FullPath )) : _sha1;

        internal virtual HashSet<BinFileAssemblyInfo> SetBinFolderAndUpdateLocalDependencies( BinFolder binFolder )
        {
            Debug.Assert( _binFolder == null || _binFolder == binFolder );
            if( _binFolder == null ) _binFolder = binFolder;
            return null;
        }

        public override string ToString() => LocalFileName;

        static internal IReadOnlyList<BinFileInfo> ReadFiles( IActivityMonitor m, string binPath )
        {
            var result = new List<BinFileInfo>();
            ReaderParameters r = new ReaderParameters();
            foreach( var f in Directory.EnumerateFiles( binPath, "*", SearchOption.AllDirectories )
                                .Where( p => p.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase )
                                             || p.EndsWith( ".exe", StringComparison.OrdinalIgnoreCase )
                                             || p.EndsWith( ".so", StringComparison.OrdinalIgnoreCase )
                                             || p.EndsWith( ".deps.json", StringComparison.OrdinalIgnoreCase ) ) )
            {
                BinFileInfo info = TryRead( m, r, f, f.Substring( binPath.Length ) );
                if( info != null ) result.Add( info );
            }
            return result;
        }

        static BinFileInfo TryRead( IActivityMonitor m, ReaderParameters r, string fullPath, string localFileName )
        {
            BinFileInfo info = null;

            var fi = new FileInfo( fullPath );
            long len = fi.Length;
            if( len > Int32.MaxValue )
            {
                m.Warn( $"'{localFileName}' is bigger than 2 GiB. It will be ignored." );
            }
            else if( len == 0 )
            {
                m.Warn( $"'{localFileName}' is an empty file. It will be ignored." );
            }
            else if( localFileName.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase )
                    || localFileName.EndsWith( ".exe", StringComparison.OrdinalIgnoreCase ) )
            {
                try
                {
                    // Mono.Cecil requires the stream to be seekable. Pity :)
                    //   using( var file = new FileStream( fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan ) )
                    //   using( var shaCompute = new SHA1Stream( file, true, true ) )
                    //   using( AssemblyDefinition a = AssemblyDefinition.ReadAssembly( shaCompute, r ) )
                    using( AssemblyDefinition a = AssemblyDefinition.ReadAssembly( fullPath, r ) )
                    {
                        var excludeAttribute = a.CustomAttributes.FirstOrDefault( attr => attr.AttributeType.FullName == "CK.Setup.ExcludeFromSetupAttribute" );
                        if( excludeAttribute != null )
                        {
                            m.Warn( $"'{localFileName}' is marked with ExcludeFromSetup attribute. It will be ignored." );
                        }
                        else info = new BinFileAssemblyInfo( fullPath, localFileName, (int)len, a, m );
                    }
                }
                catch( BadImageFormatException ex )
                {
                    m.Warn( $"While analysing '{localFileName}'.", ex );
                    info = new BinFileInfo( fullPath, localFileName, (int)len );
                }
            }
            else
            {
                info = new BinFileInfo( fullPath, localFileName, (int)len );
            }
            return info;
        }
    }
}
