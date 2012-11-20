using System;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CK.Core
{
    /// <summary>
    /// Mutable locator that combines a <see cref="Type"/> and a path to a resource. 
    /// A ResourceLocator can be <see cref="Merge"/>d with another one.
    /// </summary>
    /// <remarks>
    /// The path may begin with a ~ and in such case, the resource path is "assembly based"
    /// and the <see cref="Type"/> is used only for its <see cref="Type.Assembly"/>.
    /// </remarks>
    public class ResourceLocator : IMergeable
    {
        Type	_type;
        string	_path;

        /// <summary>
        /// Initializes an empty <see cref="ResourceLocator"/>: <see cref="P:Type"/> and <see cref="Path"/> are null.
        /// </summary>
        public ResourceLocator()
        {
        }

        /// <summary>
        /// Initializes a <see cref="ResourceLocator"/>.
        /// </summary>
        /// <param name="resourceHolder">
        /// The assembly of this type must hold the resources. The <see cref="Type.Namespace"/>
        /// is the path prefix of the resources. Can be null.
        /// </param>
        /// <param name="path">
        /// An optional sub path from the namespace of the type to the resource 
        /// itself. Can be null or <see cref="String.Empty"/> if the resources are 
        /// directly associated to the type.
        /// </param>
        public ResourceLocator( Type resourceHolder, string path )
        {
            _type = resourceHolder;
            _path = path;
        }

        /// <summary>
        /// Gets or sets the type that will be used to locate the resource: its <see cref="Type.Namespace"/> is the path prefix of the resources.
        /// The resources must belong to its <see cref="System.Reflection.Assembly"/>.
        /// </summary>
        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Gets or sets a sub path from the namespace of the <see cref="Type"/> to the resources.
        /// Can be null or <see cref="String.Empty"/> if the resources are directly 
        /// associated to the type.
        /// </summary>
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        /// <summary>
        /// Compute the resource full name from the namespace of the <see cref="Type"/> 
        /// and the <see cref="Path"/>.
        /// </summary>
        /// <param name="name">Name of the resource. Usually a file name ('sProc.sql')</param>
        /// <returns>The full resource name.</returns>
        public string ResourceName( string name )
        {
            return ResourceName( _type, _path, name );
        }

        /// <summary>
        /// Gets an ordered list of resource names that starts with the <paramref name="namePrefix"/>.
        /// </summary>
        /// <param name="namePrefix">Prefix for any strings.</param>
        /// <returns>Ordered lists of available resource names (without the prefix). Resource content can then be obtained by <see cref="OpenStream"/> or <see cref="GetString"/>.</returns>
        public IEnumerable<string> GetNames( string namePrefix )
        {
            if( _type == null ) return Util.EmptyStringArray;
            IReadOnlyList<string> a = _type.Assembly.GetSortedResourceNames();
            // TODO: Use the fact that the list is sorted to 
            // select the sub range instead of that Where linear filter.
            string prefix = ResourceName( null );
            string prefixName = namePrefix != null && namePrefix.Length > 0 ? prefix + '.' + namePrefix : prefix;
            return a.Where( n => n.Length > prefixName.Length && n.StartsWith( prefixName, StringComparison.Ordinal ) )
                    .Select( n => n.Substring( prefix.Length+1 ) );
        }

        /// <summary>
        /// Obtains the content of a resource.
        /// </summary>
        /// <param name="name">Name of the resource to load.</param>
        /// <param name="throwError">
        /// When set to false, no exception will be thrown if the resource 
        /// does not exist and null is returned.
        /// </param>
        /// <returns>
        /// An opened <see cref="Stream"/> if the resource is found.
        /// Null if the resource is not found and <paramref name="throwError"/> is false.
        /// </returns>
        public Stream OpenStream( string name, bool throwError )
        {
            return LoadStream( _type, _path, name, throwError );
        }

        /// <summary>
        /// Obtains the content of a resource as a string
        /// </summary>
        /// <param name="name">
        /// Name of the resource (can be null or empty). Usually a file name ('sProc.sql') or a type ('Model.User.1.0.0.sql') but can be any suffix.
        /// When not null nor empty a '.' will automatically be inserted between <paramref name="path"/> and name.
        /// </param>
        /// <param name="throwError">Set to false, no exception will be thrown if the resource 
        /// does not exist.</param>
        /// <returns>A string (possibly empty) if the resource is found.<br/>
        /// Null if the resource is not found and <paramref name="throwError"/> is false.</returns>
        public string GetString( string name, bool throwError )
        {
            return LoadString( _type, _path, name, throwError );
        }

        public override string ToString()
        {
            return String.Format( "ResourceLocator:{0}/{1}", _type != null ? _type.Assembly.GetName().Name : "(no assembly)", ResourceName( "*" ) );
        }

        /// <summary>
        /// Compute the resource full name from the namespace of the <paramref name="resourceHolder"/> 
        /// and the <paramref name="path"/> (that can be null or empty).
        /// </summary>
        /// <param name="resourceHolder">
        /// The assembly of this type must hold the resources and its <see cref="Type.Namespace"/>
        /// is the path prefix of the resources. Can be null.
        /// </param>
        /// <param name="path">
        /// An optional sub path from the namespace of the type to the resource 
        /// itself. Can be null or <see cref="String.Empty"/> if the resources are 
        /// directly associated to the namespace of the type.
        /// </param>
        /// <param name="name">
        /// Name of the resource (can be null or empty). Usually a file name ('sProc.sql') or a type ('Model.User.1.0.0.sql') but can be any suffix.
        /// When not null nor empty a '.' will automatically be inserted between <paramref name="path"/> and name.
        /// </param>
        /// <returns>The full resource name.</returns>
        static public string ResourceName( Type resourceHolder, string path, string name )
        {
            string ns = resourceHolder != null ? resourceHolder.Namespace : String.Empty;
            if( path != null && path.Length != 0 )
            {
                if( path[0] == '~' )
                {
                    if( path.Length > 1 && path[1] == '.' )
                        path = path.Substring( 2 );
                    else path = path.Substring( 1 );
                }
                else path = ns + '.' + path;
            }
            else path = ns;
            return (name == null || name.Length == 0) ? path : path + '.' + name;
        }

        /// <summary>
        /// Obtains the content of a resource.
        /// </summary>
        /// <exception cref="ApplicationException">If <paramref name="throwError"/> is true and the resource can not be found.</exception>
        /// <param name="resourceHolder">
        /// The assembly of this type must hold the resources and its <see cref="Type.Namespace"/>
        /// is the path prefix of the resources.
        /// <param name="path">
        /// A sub path from the namespace of the type to the resource 
        /// itself. Can be null or <see cref="String.Empty"/> if the resources are 
        /// directly associated to the type.
        /// </param>
        /// <param name="name">Name of the resource to load.</param>
        /// <param name="throwError">
        /// When set to false, no exception will be thrown if the resource 
        /// does not exist.
        /// </param>
        /// <returns>
        /// An opened <see cref="Stream"/> if the resource is found.
        /// Null if the resource is not found and <paramref name="throwError"/> is false.
        /// </returns>
        static public Stream LoadStream( Type resourceHolder, string path, string name, bool throwError )
        {
            if( resourceHolder == null )
            {
                if( throwError ) throw new ArgumentNullException( "resourceHolder" );
                return null;
            }
            string fullName = ResourceName( resourceHolder, path, name );
            return LoadStream( resourceHolder.Assembly, fullName, name, throwError );
        }

        /// <summary>
        /// Obtains the content of a resource as a string from the <paramref name="resourceHolder"/>'s assembly.
        /// </summary>
        /// <param name="resourceHolder">
        /// The assembly of this type must hold the resources its <see cref="Type.Namespace"/>
        /// is the path prefix of the resources.
        /// <param name="path">
        /// A sub path from the namespace of the type to the resource 
        /// itself. Can be null or <see cref="String.Empty"/> if the resources are 
        /// directly associated to the type.
        /// </param>
        /// <param name="name">Name of the resource to load.</param>
        /// <param name="throwError">
        /// When set to false, no exception will be thrown if the resource 
        /// does not exist.
        /// </param>
        /// <returns>A string (possibly empty) if the resource is found.
        /// Null if the resource is not found and <paramref name="throwError"/> is false.
        /// </returns>
        static public string LoadString( Type resourceHolder, string path, string name, bool throwError )
        {
            if( resourceHolder == null )
            {
                if( throwError ) throw new ArgumentNullException( "resourceHolder" );
                return null;
            }
            string fullName = ResourceName( resourceHolder, path, name );
            using( Stream stream = LoadStream( resourceHolder.Assembly, fullName, name, throwError ) )
            {
                if( stream == null ) return null;
                using( StreamReader reader = new StreamReader( stream, true ) )
                {
                    return reader.ReadToEnd();
                }
            }
        }

        static private Stream LoadStream( Assembly a, string fullResName, string name, bool throwError )
        {
            Stream stream = a.GetManifestResourceStream( fullResName );
            if( stream == null && throwError )
            {
                var resNames = a.GetSortedResourceNames();
                string shouldBe = null;
                string sEnd = '.' + name;
                foreach( string s in resNames )
                {
                    if( s.EndsWith( sEnd, StringComparison.InvariantCultureIgnoreCase ) )
                    {
                        shouldBe = s;
                        break;
                    }
                }
                throw new CKException( "Resource not found: '{0}'.{1}", fullResName, shouldBe == null ? String.Empty : String.Format( " It seems to be '{0}'.", shouldBe ) );
            }
            return stream;
        }

        /// <summary>
        /// Merges information from another locator: whenever this <see cref="P:Type"/> or <see cref="Path"/>
        /// are null, they are set.
        /// </summary>
        /// <param name="source">ResourceLocator to combine with this one.</param>
        /// <param name="services">Optional services (not used by this implementation).</param>
        /// <returns>True on success, false oterwise.</returns>
        public bool Merge( object source, IServiceProvider services = null )
        {
            ResourceLocator r = source as ResourceLocator;
            if( r != null )
            {
                if( _type == null ) _type = r._type;
                if( _path == null ) _path = r._path;
                return true;
            }
            return false;
        }
    }
}
