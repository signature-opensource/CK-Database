#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\AmbientContract\AmbientContractInterfaceKey.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Wrapper for keys in Type mapping dictionaries: when wrapped in this class,
    /// the Type is the key of its highest implementation instead of its final concrete class.
    /// This enables the use of one and only one dictionnary for Mappings (Type => Final Type) as well as 
    /// highest implementation association (Ambient contract interface => its highest implementation).
    /// </summary>
    internal class AmbientContractInterfaceKey
    {
        public readonly Type InterfaceType;

        public AmbientContractInterfaceKey( Type ambientContractInterface )
        {
            Debug.Assert( ambientContractInterface.IsInterface );
            InterfaceType = ambientContractInterface;
        }

        public override bool Equals( object obj )
        {
            AmbientContractInterfaceKey k = obj as AmbientContractInterfaceKey;
            return k != null && k.InterfaceType == InterfaceType;
        }

        public override int GetHashCode()
        {
            return -InterfaceType.GetHashCode();
        }
    }
}
