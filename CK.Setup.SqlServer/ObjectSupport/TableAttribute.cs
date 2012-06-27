//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace CK.Setup.Database
//{
//    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
//    public class TableAttribute : PackageAttribute
//    {
//        public TableAttribute( string versions )
//            : base( versions )
//        {
//            AutomaticModelRequirement = true;
//        }

//        /// <summary>
//        /// Gets or sets whether any package requirements (that is not itself a Model) is automatically projected as a requirement on 
//        /// a Model (the package name prefixed with "Model.").
//        /// Defaults to true and applies to <see cref="DynamicPackage.Requires"/> and <see cref="DynamicPackage.RequiredBy"/>.
//        /// </summary>
//        /// <remarks>
//        /// <para>
//        /// States whether the Models of the packages that our <see cref="Package"/> requires are automatically required by this Model
//        /// and whether Models of the packages that our package states to be required by automatically require this Model.
//        /// </para>
//        /// <para>
//        /// Said differently: 
//        /// "If I require a package "A", then my own Model requires "Model.A" (if A has a model).".
//        /// Or, for the "required by": 
//        /// "If I want to be required by "B" (ie. I must be before "B"), then if "B" has a model, my Model must be before "Model.B".".
//        /// </para>
//        /// </remarks>
//        public bool AutomaticModelRequirement { get; set; }

//        public string ModelRequires { get; private set; }

//        public string ModelRequiredBy { get; private set; }

//        public string ModelPackageFullName { get; set; }

//        public Type ModelPackage { get; set; }

//    }

//}
