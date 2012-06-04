using System;
using System.Reflection;
using System.IO;

namespace CK.Core
{
    /// <summary>
    /// Holds a <see cref="Type"/> and a path to a resource.
    /// </summary>
    /// <remarks>
    /// The path may begin with a ~ and in such case, the resource path is "assembly based"
    /// and the <see cref="Type"/> is used only for its <see cref="Type.Assembly"/>.
    /// </remarks>
    public class ResourceLocator
    {
        Type	_type;
        string	_path;

        /// <summary>
        /// Initializes a <see cref="ResourceLocator"/>.
        /// </summary>
        /// <param name="resourceHolder">
        /// The assembly of this type must hold the resources. The <see cref="Type.Namespace"/>
        /// is the path prefix of the resources.
        /// </param>
        /// <param name="path">
        /// A sub path from the namespace of the type to the resource 
        /// itself. Can be null or <see cref="String.Empty"/> if the resources are 
        /// directly associated to the type.
        /// </param>
        public ResourceLocator( Type resourceHolder, string path )
        {
            if( _type == null ) throw new ArgumentNullException( "resourceHolder" );
            _type = resourceHolder;
            _path = path;
        }

        /// <summary>
        /// Gets the type that will be used to locate the resource.<br/>
        /// Its <see cref="System.Reflection.Assembly"/> must hold the resources and 
        /// its <see cref="Type.Namespace"/> is the path prefix of the resources.
        /// </summary>
        public Type Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Holds a sub path from the namespace of the <see cref="Type"/> to the resources.<br/>
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
        /// Obtains the content of a resource.
        /// </summary>
        /// <param name="name">Name of the resource to load.</param>
        /// <param name="throwError">Set to false, no exception will be thrown if the resource 
        /// does not exist.</param>
        /// <returns>An opened <see cref="Stream"/> if the resource is found.<br/>
        /// Null if the resource is not found and <paramref name="throwError"/> is false.</returns>
        public Stream OpenStream( string name, bool throwError )
        {
            return LoadStream( _type, _path, name, throwError );
        }

        /// <summary>
        /// Obtains the content of a resource as a string
        /// </summary>
        /// <param name="name">Name of the resource to load.</param>
        /// <param name="throwError">Set to false, no exception will be thrown if the resource 
        /// does not exist.</param>
        /// <returns>A string (possibly empty) if the resource is found.<br/>
        /// Null if the resource is not found and <paramref name="throwError"/> is false.</returns>
        public string GetString( string name, bool throwError )
        {
            return LoadString( _type, _path, name, throwError );
        }

        /// <summary>
        /// Compute the resource full name from the namespace of the <paramref name="resourceHolder"/> 
        /// and the <paramref name="path"/> (that can be null or empty).
        /// </summary>
        /// <param name="resourceHolder">
        /// The assembly of this type must hold the resources and its <see cref="Type.Namespace"/>
        /// is the path prefix of the resources.
        /// </param>
        /// <param name="path">
        /// A sub path from the namespace of the type to the resource 
        /// itself. Can be null or <see cref="String.Empty"/> if the resources are 
        /// directly associated to the type.
        /// </param>
        /// <param name="name">
        /// Name of the resource. Usually a file name ('sProc.sql')
        /// </param>
        /// <returns>The full resource name.</returns>
        static public string ResourceName( Type resourceHolder, string path, string name )
        {
            if( resourceHolder == null ) throw new ArgumentNullException( "resourceHolder" );
            string ns = resourceHolder.Namespace;
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
            return path + '.' + name;
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
            string fullName = ResourceName( resourceHolder, path, name );
            Assembly a = resourceHolder != null ? resourceHolder.Assembly : Assembly.GetCallingAssembly();
            return LoadStream( a, fullName, name, throwError );
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
            string fullName = ResourceName( resourceHolder, path, name );
            Assembly a = resourceHolder != null ? resourceHolder.Assembly : Assembly.GetCallingAssembly();
            using( Stream stream = LoadStream( a, fullName, name, throwError ) )
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
                string[] resNames = a.GetManifestResourceNames();
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

    }
}
