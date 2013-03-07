using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class GroupsAttribute : Attribute
    {
        readonly string _groups;

        /// <summary>
        /// Defines groups by their names.
        /// </summary>
        /// <param name="groups">Comma separated list of group names.</param>
        public GroupsAttribute( string groups )
        {
            _groups = groups;
        }

        static internal DependentItemGroupList GetGroups( IActivityLogger logger, Type t )
        {
            Debug.Assert( logger != null );
            Debug.Assert( t != null );
            DependentItemGroupList result = new DependentItemGroupList();
            var all = (GroupsAttribute[])t.GetCustomAttributes( typeof(GroupsAttribute), false );
            foreach( var a in all )
            {
                result.AddCommaSeparatedString( a._groups );
            }
            return result;
        }

    }
}
