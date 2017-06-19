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
            foreach( var b in files )
            {
                b.SetBinFolderAndUpdateLocalDependencies( this );
            }
            var heads = Files.Where( f => f.ComponentKind != ComponentKind.None ).ToList();
            Components = heads.ToArray();
            foreach( var c in Components )
            {
                foreach( var dep in c.LocalDependencies ) heads.Remove( dep );
            }
        }

        bool Initialize( IActivityMonitor m )
        {
            if( !Components.Any() )
            {
                m.Error().Send( "Unable to find a Model, Runtime or Engine assembly in this folder." );
                return false;
            }
            if( !Heads.Any() )
            {
                m.Error().Send( "Components are co-dependent. How did you get there?" );
                return false;
            }
            return true;
        }

        static public BinFolder ReadBinFolder( IActivityMonitor m, string binPath )
        {
            try
            {
                binPath = FileUtil.NormalizePathSeparator( Path.GetFullPath( binPath ), true );
                if( !Directory.Exists( binPath ) )
                {
                    m.Error().Send( "Directory not found: " + binPath );
                    return null;
                }
                var b = new BinFolder( binPath, BinFileInfo.ReadFiles( m, binPath ) );
                return b.Initialize( m ) ? b : null;

            }
            catch( Exception ex )
            {
                m.Error().Send( ex );
                return null;
            }
        }

        /// <summary>
        /// Gets the normalized full path of this BinFolder (<see cref="FileUtil.NormalizePathSeparator(string, bool)"/>).
        /// It ends with a directory separator.
        /// </summary>
        public string BinPath { get; }

        /// <summary>
        /// Gets all the .dll files.
        /// </summary>
        public IReadOnlyList<BinFileInfo> Files { get; }

        /// <summary>
        /// Gets the components (Model, Runtime or Engine).
        /// </summary>
        public IReadOnlyList<BinFileInfo> Components { get; }

        /// <summary>
        /// Gets the head components.
        /// </summary>
        public IReadOnlyList<BinFileInfo> Heads { get; }

        public override string ToString()
        {
            return $"{BinPath} ({Files.Count} files)";
        }

    }
}
