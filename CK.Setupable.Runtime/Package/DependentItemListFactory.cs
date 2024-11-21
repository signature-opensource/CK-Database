#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Package\DependentItemList.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;

namespace CK.Setup;

/// <summary>
/// Factory for <see cref="IDependentItemList"/> and <see cref="IDependentItemGroupList"/>.
/// </summary>
public static class DependentItemListFactory
{
    abstract class L<T> : List<T> where T : IDependentItemRef
    {
        public void Add( string fullName )
        {
            if( !String.IsNullOrWhiteSpace( fullName ) )
            {
                Add( CreateNamed( fullName ) );
            }
        }

        protected abstract T CreateNamed( string fullName );

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

        public void Add( IEnumerable<string> fullNames )
        {
            if( fullNames != null )
            {
                foreach( var s in fullNames )
                {
                    if( !String.IsNullOrWhiteSpace( s ) ) Add( CreateNamed( s ) );
                }
            }
        }

        public void AddCommaSeparatedString( string commaSeparatedRequires )
        {
            if( !String.IsNullOrWhiteSpace( commaSeparatedRequires ) )
            {
                Add( commaSeparatedRequires.Split( new[] { ',', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries ) );
            }
        }

    }

    class ItemList : L<IDependentItemRef>, IDependentItemList
    {
        protected override IDependentItemRef CreateNamed( string fullName ) => new NamedDependentItemRef( fullName );
    }

    class GroupList : L<IDependentItemGroupRef>, IDependentItemGroupList
    {
        protected override IDependentItemGroupRef CreateNamed( string fullName ) => new NamedDependentItemGroupRef( fullName );
    }

    /// <summary>
    /// Creates a new, empty, instance of the mutable <see cref="IDependentItemList"/>.
    /// </summary>
    /// <returns>A new depdendent item list.</returns>
    public static IDependentItemList CreateItemList() => new ItemList();

    /// <summary>
    /// Creates a new, empty, instance of the mutable <see cref="IDependentItemGroupList"/>.
    /// </summary>
    /// <returns>A new depdendent item group list.</returns>
    public static IDependentItemGroupList CreateItemGroupList() => new GroupList();

}
