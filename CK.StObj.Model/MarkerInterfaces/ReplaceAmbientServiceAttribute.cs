using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Optional attribute for <see cref="IAmbientService"/> implementation that
    /// declares that this implementation replaces another one, avoiding the replaced implementation
    /// to appear in the single constructor parameters.
    /// <para>
    /// Note that this attribute is useless if this implementation specializes the replaced service since
    /// discovering the most precise implementation is one of the key goal of Ambient services handling.
    /// </para>
    /// <para>
    /// It is also useless if the replaced service is used by this implementation: as long as a parameter with the
    /// same type appears in its constructor, this service "covers" (and possibly reuses) the replaced one.
    /// </para>
    /// <para>
    /// This attribute, just like <see cref="IAmbientService"/>, <see cref="IScopedAmbientService"/>
    /// and <see cref="ISingletonAmbientService"/> can be created anywhere: the name must be ReplaceAmbientServiceAttribute
    /// and a constructor with a Type and/or a constructor with a string must be defined.
    /// </para>
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class ReplaceAmbientServiceAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new <see cref="ReplaceAmbientServiceAttribute"/> that specifies the type of the
        /// replaced service.
        /// </summary>
        /// <param name="replaced">The type of the service that this service replaces. Must not be null.</param>
        public ReplaceAmbientServiceAttribute( Type replaced )
        {
            if( replaced == null ) throw new ArgumentNullException( nameof( replaced ) );
            ReplacedType = replaced;
        }

        /// <summary>
        /// Initializes a new <see cref="ReplaceAmbientServiceAttribute"/> that specifies the assembly
        /// qualified name of the replaced service type.
        /// </summary>
        /// <param name="replaced">The type of the service that this service replaces. Must not be null or white space.</param>
        public ReplaceAmbientServiceAttribute( string replacedAssemblyQualifiedName )
        {
            if( String.IsNullOrWhiteSpace( replacedAssemblyQualifiedName ) ) throw new ArgumentNullException( nameof( replacedAssemblyQualifiedName ) );
            ReplacedAssemblyQualifiedName = replacedAssemblyQualifiedName;
        }

        /// <summary>
        /// Gets the type of the service that this service replaces.
        /// </summary>
        public Type ReplacedType { get; private set; }

        /// <summary>
        /// Gets the assembly qualified name of the replaced service type.
        /// </summary>
        public string ReplacedAssemblyQualifiedName { get; private set; }
    }
}
