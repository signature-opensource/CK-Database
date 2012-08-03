using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class StobjAttribute : Attribute, IStObjAttribute
    {
        /// <summary>
        /// Gets or sets the container of the object.
        /// </summary>
        public Type Container { get; set; }

        /// <summary>
        /// Gets or sets an array of direct dependencies.
        /// </summary>
        public Type[] Requires { get; set; }

        /// <summary>
        /// Gets or sets an array of types that depends on the object.
        /// </summary>
        public Type[] RequiredBy { get; set; }


        /// <summary>
        /// Retrieves a <see cref="IStObjAttribute"/> from attributes on a type.
        /// If multiple attributes are defined, <see cref="IStObjAttribute.Requires"/> and <see cref="IStObjAttribute.RequiredBy"/>
        /// are merged, but if their <see cref="IStObjAttribute.Container"/> are not null and differ, the first one is kept and a log is emitted 
        /// in the <paramref name="logger">.
        /// </summary>
        /// <param name="objectType">The type for which the attribute must be found.</param>
        /// <param name="logger">Logger that will receive the warning.</param>
        /// <returns>
        /// Null if no <see cref="IStObjAttribute"/> is set.
        /// </returns>
        static public IStObjAttribute GetStObjAttribute( Type objectType, IActivityLogger logger, LogLevel multipleContainerLogLevel = LogLevel.Warn )
        {
            if( objectType == null ) throw new ArgumentNullException( "objectType" );
            if( logger == null ) throw new ArgumentNullException( "logger" );

            object[] a = objectType.GetCustomAttributes( typeof( IStObjAttribute ), false );
            if( a.Length == 0 ) return null;
            if( a.Length == 1 ) return (IStObjAttribute)a[0];
            List<Type> requires = null;
            List<Type> requiredBy = null;
            Type container = null;
            IStObjAttribute containerDefiner = null;
            foreach( IStObjAttribute attr in a )
            {
                if( attr.Container != null )
                {
                    if( container == null ) 
                    {
                        containerDefiner = attr;
                        container = attr.Container;
                    }
                    else
                    {
                        if( (int)multipleContainerLogLevel >= (int)logger.Filter )
                        {
                            string msg = String.Format( "Attribute {0} for type {1} specifies Container type '{2}' but attribute {3} specifies Container type '{4}'. Container is '{4}'.",
                                                                        attr.GetType().Name, objectType.FullName, containerDefiner.GetType().Name, attr.Container.FullName );
                            logger.UnfilteredLog( multipleContainerLogLevel, msg );
                        }
                    }
                }
                Type[] req = attr.Requires;
                if( req != null && req.Length > 0 )
                {
                    if( requires == null ) requires = new List<Type>();
                    requires.AddRangeArray( req );
                }
                Type[] reqBy = attr.RequiredBy;
                if( reqBy != null && reqBy.Length > 0 )
                {
                    if( requiredBy == null ) requiredBy = new List<Type>();
                    requiredBy.AddRangeArray( reqBy );
                }
            }
            var r = new StobjAttribute();
            r.Container = container;
            if( requires != null ) r.Requires = requires.ToArray();
            if( requiredBy != null ) r.RequiredBy = requiredBy.ToArray();
            return r;
        }

    }
}
