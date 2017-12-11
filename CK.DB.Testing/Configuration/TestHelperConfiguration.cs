using CK.Core;
using CK.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CK.Testing
{
    public class TestHelperConfiguration : ITestHelperConfiguration
    {
        readonly Dictionary<string, string> _config;
        readonly SimpleServiceContainer _container;

        public TestHelperConfiguration()
        {
            _config = new Dictionary<string, string>();
            _container = new SimpleServiceContainer();
            _container.Add<ITestHelperConfiguration>( this );
            var root = new NormalizedPath( AppContext.BaseDirectory );
            SimpleReadFromAppSetting( root.FindClosestFile( "Test.config", "App.config" ) );
        }

        /// <summary>
        /// Gets the configuration value associated to a key with a lookup up to the root of the configuration.
        /// </summary>
        /// <param name="key">The path of the key to find.</param>
        /// <param name="defaultValue">The default value when not found.</param>
        /// <returns>The configured value or the default value.</returns>
        public string Get( NormalizedPath key, string defaultValue = null )
        {
            while( key )
            {
                if( _config.TryGetValue( key, out string result ) ) return result;
                if( key.Parts.Count == 1 ) break;
                key = key.RemovePart( key.Parts.Count - 2 );
            }
            return defaultValue;
        }

        void SimpleReadFromAppSetting( NormalizedPath appConfigFile )
        {
            if( !appConfigFile.IsEmpty )
            {
                XDocument doc = XDocument.Load( appConfigFile );
                foreach( var e in doc.Root.Descendants( "appSettings" ).Elements( "add" ) )
                {
                    _config[(string)e.AttributeRequired( "key" )] = (string)e.AttributeRequired( "value" );
                }
            }
        }

        public static ITestHelperConfiguration Default { get; } = new TestHelperConfiguration();
    }
}
