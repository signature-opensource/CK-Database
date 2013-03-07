using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class DependentItemList : List<IDependentItemRef>, IDependentItemList
    {
        /// <summary>
        /// Adds a full name (that may starts with '?') as a <see cref="NamedDependentItemRef"/>.
        /// </summary>
        /// <param name="fullName">Full name of the dependency. When null or empty, nothing is added.</param>
        public void Add( string fullName )
        {
            if( !String.IsNullOrWhiteSpace( fullName ) )
            {
                Add( new NamedDependentItemRef( fullName ) );
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
                    if( !String.IsNullOrWhiteSpace(s) ) Add( new NamedDependentItemRef( s ) );
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
