//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace CK.Setup.SqlServer
//{
//    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
//    public class SqlTableAttribute : Attribute
//    {
//        public SqlTableAttribute( string versions )
//        {
//            Versions = versions;
//        }

//        /// <summary>
//        /// Gets the version list.
//        /// </summary>
//        public string Versions { get; private set; }

//        /// <summary>
//        /// Gets or sets the table name (without schema). 
//        /// When null, the package object's <see cref="Type.Name"/> is used.
//        /// </summary>
//        public string TableName { get; set; }

//        /// <summary>
//        /// Gets or sets the setup container.
//        /// When null, the container defined by the <see cref="SqlDefaultDatabase"/> is used.
//        /// </summary>
//        public Type Container { get; set; }

//        /// <summary>
//        /// Gets or sets a comma separated list of full names dependencies.
//        /// </summary>
//        public string Requires { get; set; }
//        public string RequiredBy { get; set; }

//        /// <summary>
//        /// Gets or sets the default database object to use.
//        /// When null, the <see cref="Container"/>'s <see cref="Container.DefaultDatabase"/> is used.
//        /// </summary>
//        public Type Database { get; set; }

//        /// <summary>
//        /// Gets or sets the schema to use.
//        /// When null, the <see cref="Container"/>'s <see cref="Container.DefaultSchema"/> is used.
//        /// </summary>
//        public string Schema { get; set; }
//    }
//}
