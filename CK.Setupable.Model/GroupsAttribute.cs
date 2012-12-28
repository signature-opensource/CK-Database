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

        /// <summary>
        /// Gets a comma separated list of group names.
        /// </summary>
        public string Groups { get { return _groups; } }

    }
}
