#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\Impl\MutableInjectContract.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Reflection;

namespace CK.Setup
{
    /// <summary>
    /// Describes an Ambient singleton property.
    /// </summary>
    internal class MutableInjectSingleton : MutableReferenceOptional, IStObjMutableInjectSingleton
    {
        internal readonly InjectSingletonInfo AmbientContractInfo;

        internal MutableInjectSingleton( MutableItem owner, InjectSingletonInfo info )
            : base( owner, StObjMutableReferenceKind.AmbientContract )
        {
            AmbientContractInfo = info;
            Type = AmbientContractInfo.PropertyType;
            IsOptional = AmbientContractInfo.IsOptional;
        }

        public override string Name => AmbientContractInfo.Name;

        internal override string KindName => "AmbientContract";

        internal override Type UnderlyingType => AmbientContractInfo.PropertyType;

        public override string ToString() => $"Ambient Singleton '{Name}' of '{Owner}'";

    }
}
