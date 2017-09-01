using CK.Core;
using CKSetup.StreamStore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup.StreamStore
{
    /// <summary>
    /// Extends <see cref="IStreamStore"/> with <see cref="ComponentDB"/> related method extensions.
    /// </summary>
    public static class StreamStoreComponentDBExtension
    {
        /// <summary>
        /// Reads or creates the <see cref="RuntimeArchive.DbXmlFileName"/> manifest file in this store.
        /// </summary>
        /// <param name="this">This store.</param>
        /// <param name="db">Database to update.</param>
        /// <returns>The component database or null on error.</returns>
        static public void Save( this IStreamStore @this, ComponentDB db )
        {
            @this.UpdateText( RuntimeArchive.DbXmlFileName, db.ToXml().ToString(), CompressionKind.None );
        }

        /// <summary>
        /// Reads or creates the <see cref="RuntimeArchive.DbXmlFileName"/> manifest file in this store.
        /// </summary>
        /// <param name="this">This store.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <returns>The component database or null on error.</returns>
        static public ComponentDB Initialize( this IStreamStore @this, IActivityMonitor monitor )
        {
            ComponentDB db = null;
            if( @this.IsEmptyStore )
            {
                monitor.Info( $"Initializing new empty store." );
                db = new ComponentDB();
                @this.CreateText( RuntimeArchive.DbXmlFileName, db.ToXml().ToString( SaveOptions.DisableFormatting ), CompressionKind.None );
            }
            else
            {
                try
                {
                    string text = @this.ReadText( RuntimeArchive.DbXmlFileName );
                    if( text != null )
                    {
                        db = new ComponentDB( XDocument.Parse( text ).Root );
                        monitor.Trace( $"Opened store with {db.Components.Count} components." );
                    }
                    else
                    {
                        monitor.Error( $"File is not a valid runtime zip ({RuntimeArchive.DbXmlFileName} manifest not found)." );
                        db = null;
                    }
                }
                catch( Exception ex )
                {
                    monitor.Fatal( $"Invalid {RuntimeArchive.DbXmlFileName} manifest.", ex );
                    db = null;
                }
            }
            return db;
        }

        /// <summary>
        /// Tries to download a missing file to this store.
        /// </summary>
        /// <param name="this">This store.</param>
        /// <param name="monitor">Monitor to use. Can not be null.</param>
        /// <param name="downloader">Downloader. Can not be null.</param>
        /// <param name="f">File to download. Can not be null.</param>
        /// <param name="storageKind">Compression kind to use in store.</param>
        /// <returns>True on success, false otherwise.</returns>
        static public bool Download( this IStreamStore @this, IActivityMonitor monitor, IComponentFileDownloader downloader, ComponentFile f, CompressionKind storageKind )
        {
            Debug.Assert( monitor != null && downloader != null && f != null );
            using( monitor.OpenInfo( $"Downloading {f}." ) )
            {
                var storedStream = downloader.GetDownloadStream( monitor, f.SHA1, storageKind );
                if( storedStream.Stream == null )
                {
                    monitor.Error( $"Unable to obtain file by its SHA1 from downloader." );
                    return false;
                }
                try
                {
                    @this.Create( f.SHA1.ToString(), storedStream.Stream, storedStream.Kind, storageKind );
                    monitor.CloseGroup( "Successfully downloaded." );
                    return true;
                }
                catch( Exception ex )
                {
                    monitor.Error( ex );
                    return false;
                }
            }
        }

        /// <summary>
        /// Tries to download any missing files from the new component's files 
        /// of a <see cref="ComponentDB.ImportResult"/> to this store.
        /// </summary>
        /// <param name="this">This store.</param>
        /// <param name="monitor">Monitor to use. Can not be null.</param>
        /// <param name="downloader">Downloader. Can not be null.</param>
        /// <param name="r">Import result. <see cref="ComponentDB.ImportResult.Error"/> must be false.</param>
        /// <param name="storageKind">Compression kind to use in store.</param>
        /// <returns>A tuple with the numer of successfully imported files and number of failures.</returns>
        static public Tuple<int,int> DownloadImportResult( 
            this IStreamStore @this, 
            IActivityMonitor monitor, 
            IComponentFileDownloader downloader, 
            ComponentDB.ImportResult r, 
            CompressionKind storageKind )
        {
            Debug.Assert( !r.Error );
            Debug.Assert( monitor != null && downloader != null );
            int successCount = 0;
            int failedCount = 0;
            if( r.Components != null && r.Components.Count > 0 )
            {
                using( monitor.OpenInfo( $"Downloading missing files." ) )
                {
                    var newFiles = r.Components
                                    .Where( c => c.StoreFiles )
                                    .SelectMany( c => c.Files )
                                    .ToLookup( f => f.SHA1 )
                                    .Where( g => !@this.Exists( g.Key.ToString() ) )
                                    .ToList();
                    if( newFiles.Count == 0 )
                    {
                        monitor.CloseGroup( "All files are already in the store." );
                    }
                    else
                    {
                        using( monitor.OpenInfo( $"{newFiles.Count} files missing." ) )
                        {
                            foreach( var f in newFiles.Select( g => g.First() ) )
                            {
                                if( !@this.Download( monitor, downloader, f, storageKind ) ) ++failedCount;
                                else ++successCount;
                            }
                        }
                    }
                }
            }
            return Tuple.Create( successCount, failedCount );
        }
    }
}
