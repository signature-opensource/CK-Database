using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CK.Core;

namespace CK.Setup.SqlServer
{
    //public class SqlObject
    //{
    //}

    //public class SqlObjectCache<T>
    //{
    //    Dictionary<string,SqlObject> _objects;

    //    public SqlObjectCache()
    //    {
    //        _objects = new Dictionary<string, SqlObject>();
    //    }

    //    public SqlProcedure LoadFromResource( string resourcePath, string name )
    //    {
    //        string s = ResourceLocator.LoadString( typeof( T ), resourcePath, name, true );

    //    }

    //    SqlProcedure LoadFromResource( Stream s )
    //    {
    //        typeof( T ).Assembly.GetManifestResourceStream( typeof( T ), fullName );
    //    }
    //}

    public class Package : PackageBase
    {
        protected Package()
        {
        }

        public string Schema { get; set; }

        public string FullName { get; set; }

        protected override string GetFullName()
        {
            return FullName;
        }

        protected override object StartDependencySort()
        {
            return typeof( PackageSetupDriver ).AssemblyQualifiedName;
        }

    }
}

