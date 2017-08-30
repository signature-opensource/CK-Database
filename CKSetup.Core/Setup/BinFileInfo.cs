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

        protected BinFileInfo( string p, int len )
        {
            FullPath = p;
            FileLength = len;
            _sha1 = SHA1Value.ZeroSHA1;
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
        /// Gets the file length in bytes.
        /// There is no need/interest to handle files bigger than 2GB here.
        /// </summary>
        public int FileLength { get; }

        /// <summary>
        /// Get the SHA1 of the file (file is loaded the first time and only once).
        /// </summary>
        public SHA1Value ContentSHA1 => _sha1.IsZero ? (_sha1 = SHA1Value.ComputeFileSHA1( FullPath )) : _sha1;

        internal virtual HashSet<BinFileAssemblyInfo> SetBinFolderAndUpdateLocalDependencies( BinFolder binFolder )
        {
            if( _binFolder == null )
            {
                _binFolder = binFolder;
                LocalFileName = FullPath.Substring( _binFolder.BinPath.Length );
            }
            return null;
        }

        public override string ToString() => LocalFileName;

        static internal IReadOnlyList<BinFileInfo> ReadFiles( IActivityMonitor m, string binPath )
        {
            var result = new List<BinFileInfo>();
            ReaderParameters r = new ReaderParameters();
            foreach( var f in Directory.EnumerateFiles( binPath, "*.*", SearchOption.AllDirectories )
                                .Where( p => p.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase )
                                             || p.EndsWith( ".exe", StringComparison.OrdinalIgnoreCase )
                                             || p.EndsWith( ".so", StringComparison.OrdinalIgnoreCase ) ) )
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
                        info = new BinFileAssemblyInfo( fullPath, (int)len, a, m );
                    }
                }
                catch( BadImageFormatException ex )
                {
                    m.Warn( $"While analysing '{fullPath}'.", ex );
                    info = new BinFileInfo( fullPath, (int)len );
                }
            }
            return info;
        }
    }
}
