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
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
    public class RemoveContextAttribute : Attribute, IAddOrRemoveContextAttribute
    {
        public RemoveContextAttribute( string context )
        {
            if( context == null ) throw new ArgumentNullException( "context" );
            Context = context;
        }

        public string Context { get; private set; }
    }
}
