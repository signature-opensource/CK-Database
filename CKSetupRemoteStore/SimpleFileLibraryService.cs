using CK.Core;
using CSemVer;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CKSetupRemoteStore
{
    public class SimpleFileLibraryService
    {
        readonly string _root;

        public SimpleFileLibraryService( IHostingEnvironment env )
        {
            _root = FileUtil.NormalizePathSeparator( Path.Combine( env.ContentRootPath, "Files" ), true );
            Directory.CreateDirectory( _root );
        }

        public IEnumerable<KeyValuePair<string,SVersion>> Content
        {
            get
            {
                foreach( var f in Directory.EnumerateFiles( _root, "*.*", SearchOption.AllDirectories ) )
                {
                    if( f.EndsWith( ".bak" ) ) continue;
                    string localName = f.Substring( _root.Length );
                    int idx = localName.LastIndexOf( Path.DirectorySeparatorChar );
                    string version = localName.Substring( idx + 1 );
                    localName = localName.Substring( 0, idx );
                    yield return new KeyValuePair<string, SVersion>( localName, SVersion.Parse( version ) );
                }
            }
        }

        public string HtmlContent
        {
            get
            {
                var b = new StringBuilder();
                var files = Content.ToList();
                b.Append( $"<h2>{files.Count} files.</h2>" );
                b.Append( "<ul>" );
                foreach( var f in files.GroupBy( f => f.Key ) )
                {
                    b.Append( $"<li>{f.Key} files.<ul>" );
                    foreach( var v in f.OrderByDescending( version => version.Value ) )
                    {
                        string verText = v.Value.ToString();
                        b.Append( $"<li><a href=\"/Files/{f.Key}/{verText}{Path.GetExtension( f.Key )}\">{verText}</a></li>" );
                    }
                    b.Append( $"</ul></li>" );
                }
                b.Append( "</ul>" );
                return b.ToString();
            }
        }

        public async Task<bool> AddOrUpdate( IActivityMonitor monitor, string name, SVersion v, Stream file, bool allowOverwrite )
        {
            name = FileUtil.NormalizePathSeparator( name, false );
            string target = Path.Combine( _root, name, v.ToString() ) + Path.GetExtension( name );
            bool targetExists = File.Exists( target );
            if( targetExists )
            {
                if( allowOverwrite )
                {
                    monitor.Info( $"Updating existing file {name}/{v}." );
                }
                else
                {
                    monitor.Error( $"Upload file {name}/{v} already exists." );
                    return false;
                }
            }
            else
            {
                Directory.CreateDirectory( Path.GetDirectoryName( target ) );
                monitor.Info( $"Uploading new file {name}/{v}." );
            }
            using( var tmp = new TemporaryFile() )
            {
                using( var tmpStream = File.OpenWrite( tmp.Path ) )
                {
                    await file.CopyToAsync( tmpStream );
                }
                if( targetExists )
                {
                    File.Replace( tmp.Path, target, target + ".bak", true );
                }
                else
                {
                    File.Copy( tmp.Path, target );
                }
            }
            return true;
        }

    }
}
