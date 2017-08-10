using CK.Core;
using CK.Text;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CKSetup.Tests
{
    public class StoreContent
    {
        public readonly XElement Db;
        public readonly ComponentDB ComponentDB;

        public struct FileEntry
        {
            public FileEntry( ComponentDB db, string fullName )
            {
                string sha = RemoveCompressionPrefix( fullName );
                SHA1Value v = SHA1Value.Parse( sha );
                var allComps = db.Components.SelectMany( c => c.Files ).Where( f => f.SHA1 == v );
                allComps.Should().NotBeNullOrEmpty();
                ComponentFile = allComps.First();
                allComps.ShouldAllBeEquivalentTo( ComponentFile );
            }

            public readonly ComponentFile ComponentFile;

            public override string ToString() => ComponentFile.ToString();
        }

        public readonly Dictionary<SHA1Value,FileEntry> FileEntries;

        public void AllComponentFilesShouldBeStored( Component c )
        {
            c.Files.All( f => FileEntries.ContainsKey( f.SHA1 ) ).Should().BeTrue();
        }

        public void NoComponentFilesShouldBeStored( Component c )
        {
            c.Files.Any( f => FileEntries.ContainsKey( f.SHA1 ) ).Should().BeFalse();
        }

        public void ComponentShouldContainFiles( Component c, params string[] file )
        {
            c.Files.Select( f => f.Name ).Should().Contain( file );
        }

        public void ComponentShouldNotContainFiles( Component c, params string[] file )
        {
            c.Files.Select( f => f.Name ).Should().NotContain( file );
        }

        public StoreContent( string path )
        {
            if( path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                using( var z = ZipFile.Open( path, ZipArchiveMode.Read ) )
                {
                    var e = z.GetEntry( "None/" + RuntimeArchive.DbXmlFileName );
                    if( e != null )
                    {
                        using( var content = e.Open() )
                        {
                            Db = NormalizeWithoutAnyOrder( XDocument.Load( content ).Root );
                        }
                        ComponentDB = new ComponentDB( Db );
                    }

                    FileEntries = z.Entries
                                .Where( x => RemoveCompressionPrefix( x.FullName ) != RuntimeArchive.DbXmlFileName )
                                .Select( x => new FileEntry( ComponentDB, x.FullName ) )
                                .ToDictionary( x => x.ComponentFile.SHA1 );
                }
            }
            else
            {
                path = Path.GetFullPath( path ) + FileUtil.DirectorySeparatorString;
                string pathNone = FileUtil.NormalizePathSeparator( Path.Combine( path, "None" ), true );
                string pathGZiped = FileUtil.NormalizePathSeparator( Path.Combine( path, "GZiped" ), true );
                if( File.Exists( pathNone + RuntimeArchive.DbXmlFileName ) )
                {
                    Db = NormalizeWithoutAnyOrder( XDocument.Load( pathNone + RuntimeArchive.DbXmlFileName ).Root );
                    ComponentDB = new ComponentDB( Db );
                }
                FileEntries = Directory.EnumerateFiles( pathNone, "*", SearchOption.AllDirectories )
                                .Concat( Directory.EnumerateFiles( pathGZiped, "*", SearchOption.AllDirectories ) )
                                .Select( p => p.Substring( path.Length ) )
                                .Where( x => RemoveCompressionPrefix( x ) != RuntimeArchive.DbXmlFileName )
                                .Select( x => new FileEntry( ComponentDB, x ) )
                                .ToDictionary( x => x.ComponentFile.SHA1 );
            }
        }

        static string RemoveCompressionPrefix( string name )
        {
            if( name.StartsWith( "None/" ) || name.StartsWith( @"None\" ) ) return name.Substring( 5 );
            if( name.StartsWith( "GZiped/" ) || name.StartsWith( @"GZiped\" ) ) return name.Substring( 7 );
            throw new Exception( $@"File name {name} should start with 'None/', 'None\', 'GZiped/' or 'GZiped\'." );
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
                                        .Concatenate( "\u0001" ) )
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
