using NUnit.Framework;
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
    [TestFixture]
    public class ComponentDBTests
    {
        [TestCase( "component-db-v0.zip" )]
        public void component_load_stores( string name )
        {
            var db = LoadFromCapturedStore( name );
            var info = new ComponentDBInfo( db );
        }

        static ComponentDB LoadFromCapturedStore( string name )
        {
            var path = Path.Combine( TestHelper.CapturedStoredFolder, name );
            using( var a = ZipFile.OpenRead( path ) )
            using( var content = a.Entries[0].Open() )
            {
                return new ComponentDB( XDocument.Load( content ).Root );
            }
        }
    }
}
