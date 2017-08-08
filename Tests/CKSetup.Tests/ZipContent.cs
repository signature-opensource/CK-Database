using CK.Text;
using FluentAssertions;
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
        public readonly ComponentDB ComponentDB;

        public struct FileEntry
        {
            public FileEntry( ComponentDB db, ZipArchiveEntry e )
            {
                FullPath = e.FullName;
                SHA1Value v = SHA1Value.Parse( FullPath );
                var allComps = db.Components.SelectMany( c => c.Files ).Where( f => f.SHA1.Equals( v ) );
                allComps.Should().NotBeNullOrEmpty();
                ComponentFile = allComps.First();
                allComps.ShouldAllBeEquivalentTo( ComponentFile );
            }

            public readonly string FullPath;
            public readonly ComponentFile ComponentFile;

            public override string ToString() => FullPath;
        }

        public readonly HashSet<SHA1Value> Files;

        public void AllComponentFilesShouldBeStored( Component c )
        {
            c.Files.All( f => Files.Contains( f.SHA1 ) ).Should().BeTrue();
        }

        public void ComponentShouldContainFiles( Component c, params string[] file )
        {
            c.Files.Select( f => f.Name ).Should().Contain( file );
        }

        public void ComponentShouldNotContainFiles( Component c, params string[] file )
        {
            c.Files.Select( f => f.Name ).Should().NotContain( file );
        }

        public ZipContent( string path )
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
                Files = new HashSet<SHA1Value>( z.Entries
                            .Select( ef => RemoveCompressionPrefix( ef.FullName ) )
                            .Where( n => n != RuntimeArchive.DbXmlFileName )
                            .Select( n => SHA1Value.Parse( n ) ) );
            }
        }

        static string RemoveCompressionPrefix( string name )
        {
            if( name.StartsWith( "None/" ) ) return name.Substring( 5 );
            name.Should().StartWith( "GZiped/" );
            return name.Substring( 7 );
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
