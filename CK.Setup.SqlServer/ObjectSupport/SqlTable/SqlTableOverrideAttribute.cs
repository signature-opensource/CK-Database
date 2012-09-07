//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace CK.Setup.SqlServer
//{
//    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
//    public class SqlTableOverrideAttribute : Attribute
//    {
//        public SqlTableOverrideAttribute( string versions )
//        {
//            Versions = versions;
//        }

//        /// <summary>
//        /// Gets the version list.
//        /// </summary>
//        public string Versions { get; private set; }

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
//    }
//}
