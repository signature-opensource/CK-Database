#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\SetupNameAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class SetupNameAttribute : Attribute, IAttributeSetupName
    {
        readonly string _name;

        public SetupNameAttribute( string fullName )
        {
            _name = fullName;
        }

        public string FullName
        {
            get { return _name; }
        }

    }
}
