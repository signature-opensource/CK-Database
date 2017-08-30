using CK.Core;
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

    public class BinFolder
    {
        BinFolder( string p, IReadOnlyList<BinFileInfo> files )
        {
            BinPath = p;
            Files = files;
            Assemblies = files.OfType<BinFileAssemblyInfo>().ToArray();
            foreach( var b in files )
            {
                b.SetBinFolderAndUpdateLocalDependencies( this );
            }
            var heads = Assemblies.Where( f => f.ComponentKind != ComponentKind.None ).ToList();
            Components = heads.ToArray();
            foreach( var c in Components )
            {
                foreach( var dep in c.LocalDependencies ) heads.Remove( dep );
            }
            Heads = heads.ToArray();
        }

        bool Initialize( IActivityMonitor m )
        {
            if( !Components.Any() )
            {
                m.Error( "Unable to find a Model or Setup Dependency assembly in this folder." );
                return false;
            }
            if( !Heads.Any() )
            {
                m.Error( "Components are co-dependent. How did you get there?" );
                return false;
            }
            return true;
        }

        /// <summary>
        /// Reads the content of a folder.
        /// Returns null on error.
        /// </summary>
        /// <param name="m">The monitor to use. Can not be null.</param>
        /// <param name="binPath"></param>
        /// <returns>The bin folder or null on error.</returns>
        static public BinFolder ReadBinFolder( IActivityMonitor m, string binPath )
        {
            if( m == null ) throw new ArgumentNullException( nameof( m ) );
            if( binPath == null ) throw new ArgumentNullException( nameof( binPath ) );
            using( m.OpenInfo( $"Reading files from '{binPath}'." ) )
            {
                try
                {
                    binPath = FileUtil.NormalizePathSeparator( Path.GetFullPath( binPath ), true );
                    if( !Directory.Exists( binPath ) )
                    {
                        m.Error( "Directory not found: " + binPath );
                        return null;
                    }
                    var b = new BinFolder( binPath, BinFileInfo.ReadFiles( m, binPath ) );
                    return b.Initialize( m ) ? b : null;

                }
                catch( Exception ex )
                {
                    m.Error( ex );
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the normalized full path of this BinFolder (<see cref="FileUtil.NormalizePathSeparator(string, bool)"/>).
        /// It ends with a directory separator.
        /// </summary>
        public string BinPath { get; }

        /// <summary>
        /// Gets all the discovered files.
        /// </summary>
        public IReadOnlyList<BinFileInfo> Files { get; }

        /// <summary>
        /// Gets all the discovered files.
        /// </summary>
        public IReadOnlyList<BinFileAssemblyInfo> Assemblies { get; }

        /// <summary>
        /// Gets the components (Model or Setup Dependency).
        /// </summary>
        public IReadOnlyList<BinFileAssemblyInfo> Components { get; }

        /// <summary>
        /// Gets the head components.
        /// </summary>
        public IReadOnlyList<BinFileAssemblyInfo> Heads { get; }

        public override string ToString()
        {
            return $"{BinPath} ({Files.Count} files)";
        }

    }
}
