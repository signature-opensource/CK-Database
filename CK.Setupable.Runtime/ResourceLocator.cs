#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\Core\ResourceLocator.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
    /// and the <see cref="Type"/> is used only for its assembly.
    /// </remarks>
    public class ResourceLocator : IResourceLocator
    {
        Type	_type;
        Type	_fallbackType;
        string	_path;

        /// <summary>
        /// Initializes an empty <see cref="ResourceLocator"/>: <see cref="PrimaryType"/>, <see cref="Path"/> and <see cref="FallbackType"/> are null.
        /// </summary>
        public ResourceLocator()
        {
        }

        /// <summary>
        /// Initializes a <see cref="ResourceLocator"/>.
        /// </summary>
        /// <param name="primaryType">
        /// The assembly of this type must hold the resources. The <see cref="T:Type.Namespace"/>
        /// is the path prefix of the resources. Can be null.
        /// </param>
        /// <param name="path">
        /// An optional sub path (can be null) from the namespace of the type to the resource 
        /// itself. Can be null or <see cref="String.Empty"/> if the resources are 
        /// directly associated to the type.
        /// </param>
        /// <param name="fallbackType">Will be used whenever <see cref="PrimaryType"/> is null.</param>
        public ResourceLocator( Type primaryType, string path, Type fallbackType )
        {
            _type = primaryType;
            _path = path;
            _fallbackType = fallbackType;
        }

        /// <summary>
        /// Gets or sets the type that will be used to locate the resource: its <see cref="T:PrimaryType.Namespace"/> is the path prefix of the resources.
        /// The resources must belong to its <see cref="System.Reflection.Assembly"/>.
        /// </summary>
        public Type PrimaryType
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Gets or sets the fallback type that will be used if <see cref="PrimaryType"/> is null.
        /// </summary>
        public Type FallbackType
        {
            get { return _fallbackType; }
            set { _fallbackType = value; }
        }

        /// <summary>
        /// Gets the type that will be used to locate the resource (either <see cref="PrimaryType"/> or <see cref="FallbackType"/>).
        /// </summary>
        public Type Type
        {
            get { return _type ?? _fallbackType; }
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
        /// Compute the resource full name from the namespace of the <see cref="P:Type"/> 
        /// and the <see cref="P:Path"/> properties and combines it with the given name.
        /// </summary>
        /// <param name="name">Name of the resource. Usually a file name ('sProc.sql')</param>
        /// <returns>The full resource name.</returns>
        public string ResourceName( string name )
        {
            return ResourceName( Type, _path, name );
        }

        /// <summary>
        /// Gets an ordered list of resource names that starts with the <paramref name="namePrefix"/>.
        /// </summary>
        /// <param name="namePrefix">Prefix for any strings.</param>
        /// <returns>
        /// Ordered lists of available resource names (with the <paramref name="namePrefix"/>). 
        /// Resource content can then be obtained by <see cref="OpenStream"/> or <see cref="G:GetString"/>.
        /// </returns>
        public IEnumerable<string> GetNames( string namePrefix )
        {
            if( Type == null ) return Util.Array.Empty<string>();
            IReadOnlyList<string> a = Type.GetTypeInfo().Assembly.GetSortedResourceNames();
            
            string p = ResourceName( "." );
            namePrefix = p + namePrefix;

            // TODO: Use the fact that the list is sorted to 
            // select the sub range instead of that Where linear filter.           
            return a.Where( n => n.Length > namePrefix.Length && n.StartsWith( namePrefix, StringComparison.Ordinal ) )
                    .Select( n => n.Substring( p.Length ) );
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
            return LoadStream( Type, _path, name, throwError );
        }

        /// <summary>
        /// Obtains the content of a resource as a string.
        /// </summary>
        /// <param name="name">
        /// Name of the resource (can be null or empty). Usually a file name ('sProc.sql') or a type ('Model.User.1.0.0.sql') but can be any suffix.
        /// </param>
        /// <param name="throwError">
        /// Set to false, no exception will be thrown if the resource does not exist.
        /// </param>
        /// <param name="allowedNamePrefix">
        /// Allowed prefixes like "[Replace]" or "[Override]".
        /// </param>
        /// <returns>
        /// A string (possibly empty) if the resource is found.
        /// Null if the resource is not found and <paramref name="throwError"/> is false.
        /// </returns>
        public string GetString( string name, bool throwError, params string[] allowedNamePrefix )
        {
            foreach( var p in allowedNamePrefix )
            {
                string s = LoadString( Type, _path, p + name, false );
                if( s != null ) return s;
            }
            return LoadString( Type, _path, name, throwError );
        }

        /// <summary>
        /// Obtains the content of a resource as a string.
        /// </summary>
        /// <param name="name">
        /// Name of the resource (can be null or empty). Usually a file name ('sProc.sql') or a type ('Model.User.1.0.0.sql') but can be any suffix.
        /// </param>
        /// <param name="throwError">
        /// Set to false, no exception will be thrown if the resource does not exist.
        /// </param>
        /// <param name="namePrefix">
        /// The prefix found (prefix are looked up first). String.Empty if there were no match with prefix.
        /// </param>
        /// <param name="allowedNamePrefix">
        /// Allowed prefixes like "[Replace]" or "[Override]".
        /// </param>
        /// <returns>
        /// A string (possibly empty) if the resource is found.
        /// Null if the resource is not found and <paramref name="throwError"/> is false.
        /// </returns>
        public string GetString( string name, bool throwError, out string namePrefix, params string[] allowedNamePrefix )
        {
            foreach( var p in allowedNamePrefix )
            {
                string s = LoadString( Type, _path, p + name, false );
                if( s != null )
                {
                    namePrefix = p;
                    return s;
                }
            }
            namePrefix = String.Empty;
            return LoadString( Type, _path, name, throwError );
        }

        /// <summary>
        /// Overridden to return the assembly:path for this ResourceLocator.
        /// </summary>
        /// <returns>The assembly:path string.</returns>
        public override string ToString()
        {
            return String.Format( "{0}:{1}", Type != null ? Type.GetTypeInfo().Assembly.GetName().Name : "(no assembly)", ResourceName( "*" ) );
        }

        /// <summary>
        /// Computes the resource full name from the namespace of the <paramref name="resourceHolder"/> 
        /// and the <paramref name="path"/> (that can be null or empty).
        /// </summary>
        /// <param name="resourceHolder">
        /// The assembly of this type must hold the resources and its <see cref="T:Type.Namespace"/>
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
                else
                {
                    if( path[0] == '.' )
                        path = ns + path;
                    else path = ns + '.' + path;
                }
            }
            else path = ns;
            if( name == null || name.Length == 0 ) return path;
            if( name[0] == '.' )
            {
                if( path.Length > 0 && path[path.Length - 1] == '.' ) return path + name.Substring( 1 );
                return path + name;
            }
            if( path.Length > 0 && path[path.Length - 1] == '.' ) return path + name;
            return path + '.' + name;
        }

        /// <summary>
        /// Obtains the content of a resource.
        /// </summary>
        /// <exception cref="ApplicationException">If <paramref name="throwError"/> is true and the resource can not be found.</exception>
        /// <param name="resourceHolder">
        /// The assembly of this type must hold the resources and its <see cref="T:Type.Namespace"/>
        /// is the path prefix of the resources.
        /// </param>
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
            return LoadStream( resourceHolder.GetTypeInfo().Assembly, fullName, name, throwError );
        }

        /// <summary>
        /// Obtains the content of a resource as a string from the <paramref name="resourceHolder"/>'s assembly.
        /// </summary>
        /// <param name="resourceHolder">
        /// The assembly of this type must hold the resources its <see cref="T:Type.Namespace"/>
        /// is the path prefix of the resources.
        /// </param>
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
            using( Stream stream = LoadStream( resourceHolder.GetTypeInfo().Assembly, fullName, name, throwError ) )
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
                    if( s.EndsWith( sEnd, StringComparison.OrdinalIgnoreCase ) )
                    {
                        shouldBe = s;
                        break;
                    }
                }
                throw new CKException( "Resource not found: '{0}'.{1}", fullResName, shouldBe == null ? string.Empty : $" It seems to be '{shouldBe}'." );
            }
            return stream;
        }

        /// <summary>
        /// Merges information from another <see cref="IResourceLocator"/>: whenever this <see cref="P:Type"/> or <see cref="Path"/>
        /// are null, they are set.
        /// When this Path starts with a dot, it is appended to the path of the merged object.
        /// </summary>
        /// <param name="source">ResourceLocator to combine with this one.</param>
        /// <param name="services">Optional services (not used by this implementation).</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool Merge( object source, IServiceProvider services = null )
        {
            IResourceLocator r = source as IResourceLocator;
            if( r != null )
            {
                if( _type == null ) _type = r.Type;
                if( _path == null ) _path = r.Path;
                else if( r.Path != null && _path.Length > 0 && _path[0] == '.' )
                {
                    _path = r.Path + _path;
                }
                return true;
            }
            return false;
        }
    }
}
