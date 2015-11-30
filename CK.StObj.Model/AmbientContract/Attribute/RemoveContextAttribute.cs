#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\AmbientContract\Attribute\RemoveContextAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Attribute that removes an object from an existing context.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class RemoveContextAttribute : Attribute, IAddOrRemoveContextAttribute
    {
        /// <summary>
        /// Initializes a new <see cref="RemoveContextAttribute"/>.
        /// </summary>
        /// <param name="context">>Context from which the object must be removed.</param>
        public RemoveContextAttribute( string context )
        {
            if( context == null ) throw new ArgumentNullException( "context" );
            Context = context;
        }

        /// <summary>
        /// Gets the context from which the object must be removed.
        /// </summary>
        public string Context { get; private set; }
    }
}
