#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\AmbientContract\Attribute\AddContextAttribute.cs) is part of CK-Database. 
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
    /// Adds the decorated class to the given context.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class AddContextAttribute : Attribute, IAddOrRemoveContextAttribute
    {
        /// <summary>
        /// Initializes a new <see cref="AddContextAttribute"/> that adds the decorated class to the given context.
        /// </summary>
        /// <param name="context">Name of the context.</param>
        public AddContextAttribute( string context )
        {
            if( context == null ) throw new ArgumentNullException( "context" );
            Context = context;
        }

        /// <summary>
        /// Gets the name of the context into which the decorated class must be injected.
        /// </summary>
        public string Context { get; private set; }
    }
}
