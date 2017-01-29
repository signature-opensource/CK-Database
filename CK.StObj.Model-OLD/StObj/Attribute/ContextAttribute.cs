#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObj\Attribute\ContextAttribute.cs) is part of CK-Database. 
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
    /// Offers a way to statically bind a parameter or a property to a typed context.
    /// </summary>
    /// <remarks>
    /// The <see cref="AttributeTargets.ReturnValue"/> is defined here for consistency (it is perfectly applicable to a return value) but is 
    /// currently not used by the framework since we do not handle methods other than void Construct.
    /// </remarks>
    [AttributeUsage( AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property, Inherited = false, AllowMultiple = false )]
    public class ContextAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new <see cref="ContextAttribute"/>.
        /// </summary>
        /// <param name="context">Context name.</param>
        public ContextAttribute( string context )
        {
            Context = context;
        }

        /// <summary>
        /// Gets the context name.
        /// </summary>
        public string Context { get; private set; }
    }
}
