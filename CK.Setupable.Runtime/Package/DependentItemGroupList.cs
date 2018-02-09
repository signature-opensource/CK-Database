#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Package\DependentItemGroupList.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Mutable list of <see cref="IDependentItemGroupRef"/>.
    /// </summary>
    public class DependentItemGroupList : List<IDependentItemGroupRef>, IDependentItemGroupList
    {
        /// <summary>
        /// Intializes a new dependent item with existing groups. 
        /// </summary>
        /// <param name="existing">Exisitng groups.</param>
        public DependentItemGroupList( IEnumerable<IDependentItemGroupRef> existing ) 
            : base( existing )
        {
        }

        /// <summary>
        /// Intializes a new dependent item with existing groups. 
        /// </summary>
        /// <param name="existing">Exisitng groups.</param>
        public DependentItemGroupList()
        {
        }

        /// <summary>
        /// Adds a full name as a <see cref="NamedDependentItemGroupRef"/>.
        /// </summary>
        /// <param name="fullName">Full name of the dependency. When null or empty, nothing is added.</param>
        public void Add( string fullName )
        {
            if( !string.IsNullOrWhiteSpace( fullName ) )
            {
                Add( new NamedDependentItemGroupRef( fullName ) );
            }
        }

        /// <summary>
        /// Removes a full name (that may starts with '?').
        /// </summary>
        /// <param name="fullName">Full name of the dependency.  When null or empty, nothing is removed.</param>
        public void Remove( string fullName )
        {
            if( !String.IsNullOrWhiteSpace( fullName ) )
            {
                bool opt = fullName[0] == '?';
                if( opt ) fullName = fullName.Substring( 1 );

                int i = 0;
                while( (i = FindIndex( i, d => d.Optional == opt && d.FullName == fullName )) >= 0 )
                {
                    RemoveAt( i );
                }
            }
        }

        /// <summary>
        /// <see cref="Add(string)">Adds</see> multiple full names.
        /// </summary>
        /// <param name="fullNames">The list of full names to add. When null or empty, nothing is added.</param>
        public void Add( IEnumerable<string> fullNames )
        {
            if( fullNames != null )
            {
                foreach( var s in fullNames )
                {
                    if( !String.IsNullOrWhiteSpace(s) ) Add( new NamedDependentItemGroupRef( s ) );
                }
            }
        }

        /// <summary>
        /// Splits the parameter on the comma and <see cref="Add(string)">adds</see> the multiple full names.
        /// </summary>
        /// <param name="commaSeparatedRequires">Comma separated full names. When null or empty, nothing is added.</param>
        public void AddCommaSeparatedString( string commaSeparatedRequires )
        {
            if( !String.IsNullOrWhiteSpace( commaSeparatedRequires ) )
            {
                Add( commaSeparatedRequires.Split( new[] { ',', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries ) );
            }
        }

    }
}
