using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    internal class AttributesReader
    {
        /// <summary>
        /// Retrieves a <see cref="IStObjAttribute"/> from (potentially multiple) attributes on a type.
        /// If multiple attributes are defined, <see cref="IStObjAttribute.Requires"/>, <see cref="IStObjAttribute.Children"/>, and <see cref="IStObjAttribute.RequiredBy"/>
        /// are merged, but if their <see cref="IStObjAttribute.Container"/> are not null or if <see cref="ItemKind"/> is not <see cref="DependentItemKind.Unknown"/> and differ, the 
        /// first one is kept and a log is emitted in the <paramref name="logger"/>.
        /// </summary>
        /// <param name="objectType">The type for which the attribute must be found.</param>
        /// <param name="logger">Logger that will receive the warning.</param>
        /// <param name="multipleContainerLogLevel"><see cref="LogLevel"/> when different containers are detected. By default a warning is emitted.</param>
        /// <returns>
        /// Null if no <see cref="IStObjAttribute"/> is set.
        /// </returns>
        static internal IStObjAttribute GetStObjAttributeForExactType( Type objectType, IActivityLogger logger, LogLevel multipleContainerLogLevel = LogLevel.Warn )
        {
            if( objectType == null ) throw new ArgumentNullException( "objectType" );
            if( logger == null ) throw new ArgumentNullException( "logger" );

            var a = (IStObjAttribute[])objectType.GetCustomAttributes( typeof( IStObjAttribute ), false );
            if( a.Length == 0 ) return null;
            if( a.Length == 1 ) return (IStObjAttribute)a[0];
            IList<Type> requires = null;
            IList<Type> requiredBy = null;
            IList<Type> children = null;
            IList<Type> group = null;
            DependentItemKindSpec itemKind = DependentItemKindSpec.Unknown;
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
                            logger.UnfilteredLog( ActivityLogger.RegisteredTags.EmptyTrait, multipleContainerLogLevel, msg, DateTime.UtcNow );
                        }
                    }
                }
                if( attr.ItemKind != DependentItemKindSpec.Unknown )
                {
                    if( itemKind != DependentItemKindSpec.Unknown ) logger.Warn( "ItemKind is already set to '{0}'. Value '{1}' set by {2} is ignored.", itemKind, attr.ItemKind, attr.GetType().Name );
                    else itemKind = attr.ItemKind;
                }
                CombineTypes( ref requires, attr.Requires );
                CombineTypes( ref requiredBy, attr.RequiredBy );
                CombineTypes( ref children, attr.Children );
                CombineTypes( ref group, attr.Groups );
            }
            var r = new StObjAttribute();
            r.Container = container;
            r.ItemKind = itemKind;
            if( requires != null ) r.Requires = requires.ToArray();
            if( requiredBy != null ) r.RequiredBy = requiredBy.ToArray();
            if( children != null ) r.Children = children.ToArray();
            if( group != null ) r.Groups = group.ToArray();
            return r;
        }

        static void CombineTypes( ref IList<Type> list, Type[] types )
        {
            if( types != null && types.Length > 0 )
            {
                if( list == null ) list = new List<Type>();
                list.AddRangeArray( types );
            }
        }

    }
}
