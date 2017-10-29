#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\AutoImplementor\DynamicAssembly.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Collections;

namespace CK.Core
{
    public static class DynamicAssemblyExtension
    {
        /// <summary>
        /// Gets a type name in <see cref="SourceBuilder"/>'s namespace and a <see cref="NextUniqueNumber"/> suffix
        /// or a guid when the <paramref name="name"/> is null.
        /// </summary>
        /// <param name="this">This Dynamic assembly.</param>
        /// <param name="name">Base type name.</param>
        /// <returns>A unique type name.</returns>
        public static string AutoNextTypeName(this IDynamicAssembly @this, string name = null)
        {
            return @this.DefaultGenerationNamespace.FullName + '.' + (name != null ? name + @this.NextUniqueNumber() : Guid.NewGuid().ToString());
        }

/*
        public struct Marker<T>
        {
            public readonly T Value;
            public readonly bool AlreadyMarked;

            public Marker(T v, bool e)
            {
                Value = v;
                AlreadyMarked = e;
            }
        }

        public static Marker<T> MemoryMark<T>(this IDynamicAssembly @this, object key)
        {
            object r = @this.Memory[key];
            if (r == null)
            {
                @this.Memory[key] = new Marker<T>(default(T),true);
                return new Marker<T>();
            }
            if (r is Marker<T>) return (Marker<T>)r;
            if (r is T) return new Marker<T>((T)r, true);
            throw new InvalidOperationException($"DynamicAssembly.Memory type mismatch for key '{key}'. Expected '{typeof(T).Name}', got '{r.GetType().Name}'.");
        }

        public static T MemoryMark<T>(this IDynamicAssembly @this, object key, T value)
        {
            object r = @this.Memory[key];
            if (r == null || r is Marker<T>)
            {
                @this.Memory[key] = new Marker<T>(value, true);
                return value;
            }
            if (r is T) return (T)r;
            throw new InvalidOperationException($"DynamicAssembly.Memory type mismatch for key '{key}'. Expected '{typeof(T).Name}', got '{r.GetType().Name}'.");
        }
*/
        /// <summary>
        /// Gets all information related to Poco.
        /// </summary>
        /// <param name="this">This Dynamic assembly.</param>
        /// <returns>The Poco information.</returns>
        public static IPocoSupportResult GetPocoInfo( this IDynamicAssembly @this ) => (IPocoSupportResult)@this.Memory[typeof( IPocoSupportResult )];

    }
}
