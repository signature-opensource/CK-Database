using CK.Text;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup.Tests
{
    public class ZipContent
    {
        public readonly XElement Db;

        public struct FileEntry
        {
            public FileEntry( ZipArchiveEntry e )
            {
                FullPath = e.FullName;
                var p = FullPath.Split( '/' );
                ComponentName = p[0];
                Version = p[1];
                Framework = p[2];
                FilePath = e.FullName.Substring( p[0].Length + p[1].Length + p[2].Length + 3 );
            }

            public readonly string FullPath;
            public readonly string ComponentName;
            public readonly string Version;
            public readonly string Framework;
            public readonly string FilePath;

            public override string ToString() => FullPath;
        }

        public readonly IReadOnlyList<FileEntry> Files;

        public ZipContent( string path )
        {
            using( var z = ZipFile.Open( path, ZipArchiveMode.Read ) )
            {
                var e = z.GetEntry( ZipRuntimeArchive.DbXmlFileName );
                if( e != null )
                {
                    using( var content = e.Open() )
                    {
                        Db = NormalizeWithoutAnyOrder( XDocument.Load( content ).Root );
                    }
                }
                Files = z.Entries.Where( x => x.FullName != ZipRuntimeArchive.DbXmlFileName ).Select( x => new FileEntry( x ) ).ToArray();
            }
        }
    static XElement NormalizeWithoutAnyOrder( XElement element )
    {
        if( element.HasElements )
        {
            return new XElement(
                element.Name,
                element.Attributes().OrderBy( a => a.Name.ToString() ),
                element.Elements()
                    .OrderBy( a => a.Name.ToString() )
                    .Select( e => NormalizeWithoutAnyOrder( e ) )
                    .OrderBy( e => e.Attributes().Count() )
                    .OrderBy( e => e.Attributes()
                                    .Select( a => a.Value )
                                    .Concatenate("\u0001") )
                    .ThenBy( e => e.Value ) );
        }
        if( element.IsEmpty || string.IsNullOrEmpty( element.Value ) )
        {
            return new XElement( element.Name,
                                 element.Attributes()
                                        .OrderBy( a => a.Name.ToString() ) );
        }
        return new XElement( element.Name, 
                             element.Attributes()
                                    .OrderBy( a => a.Name.ToString() ), 
                             element.Value );
    }
    }
}
