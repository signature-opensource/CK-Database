#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Version\VersionedNameExtension.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Offers extension for related objects like <see cref="VersionedName"/>.
    /// </summary>
    public static class VersionRelatedExtensions
    {
        /// <summary>
        /// Checks <see cref="IVersionedItem"/> contracts and raises <see cref="ArgumentException"/> as required.
        /// This also checks <see cref="IDependentItem"/> contracts.
        /// </summary>
        /// <param name="this">This version item.</param>
        /// <param name="parameterName">Name of the parameter to check.</param>
        /// <param name="allowNull">True to allow null.</param>
        public static void CheckVersionItemArgument( this IVersionedItem @this, string parameterName, bool allowNull = false )
        {
            if( @this == null )
            {
                if( allowNull ) return;
                throw new ArgumentNullException( parameterName );
            }
            string fullName = @this.FullName;
            if( string.IsNullOrWhiteSpace( fullName ) ) throw new ArgumentException( "IDependentItem.FullName must be not be empty.", parameterName );
            if( fullName.Length > 400 ) throw new ArgumentOutOfRangeException( parameterName, fullName, "IDependentItem.FullName not be longer than 400 characters." );
            string type = @this.ItemType;
            if( type == null ) throw new ArgumentException( "IVersionedItem.ItemType must be not null", parameterName );
            if( type.Length == 0 || type.Length > 16 ) throw new ArgumentOutOfRangeException( parameterName, type, "IVersionedItem.ItemType must be between 1 and 16 characters long." );
            IEnumerable<VersionedName> prev = @this.PreviousNames;
            if( prev != null && !prev.IsSortedStrict( ( v1, v2 ) => v1.Version.CompareTo( v2.Version ) ) )
            {
                throw new ArgumentException( $"PreviousNames must be ordered by their Version for FullName='{@this.FullName}'", parameterName );
            }
        }

        /// <summary>
        /// Returns a collection of <see cref="VersionedName"/> with potentially different <see cref="VersionedName.FullName"/>.
        /// Can safely be called on null source reference.
        /// </summary>
        /// <param name="source">This enumerable.</param>
        /// <param name="fullNameProvider">Name provider. By returning null, the VersionedName does not appear in the resulting enumerable.</param>
        /// <returns>A collection of <see cref="VersionedName"/> whose FullName may have changed.</returns>
        public static IEnumerable<VersionedName> SetRefFullName( this IEnumerable<VersionedName> source, Func<VersionedName, string> fullNameProvider )
        {
            if( source != null )
            {
                foreach( var v in source )
                {
                    string newName = fullNameProvider( v );
                    if( newName != null )
                    {
                        if( newName == v.FullName ) yield return v;
                        else yield return new VersionedName( newName, v.Version );
                    }
                }
            }
        }
    }
}
