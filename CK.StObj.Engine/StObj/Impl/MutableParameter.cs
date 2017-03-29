#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\Impl\MutableParameter.cs) is part of CK-Database. 
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
    /// Describes a parameter of a Construct method.
    /// </summary>
    internal class MutableParameter : MutableReferenceWithValue, IStObjMutableParameter, IStObjFinalParameter
    {
        ParameterInfo _param;

        internal MutableParameter( MutableItem owner, ParameterInfo param, bool isContainer )
            : base( owner, isContainer ? StObjMutableReferenceKind.ConstructParameter|StObjMutableReferenceKind.Container : StObjMutableReferenceKind.ConstructParameter )
        {
            _param = param;
            Type = param.ParameterType;
            IsOptional = param.IsOptional;
            if( IsSetupLogger ) StObjRequirementBehavior = Setup.StObjRequirementBehavior.None;
        }

        public int Index => _param.Position;

        public override string Name => _param.Name;

        public bool IsRealParameterOptional => _param.IsOptional;

        internal override string KindName => "Parameter";

        internal override Type UnderlyingType => _param.ParameterType;

        internal bool IsSetupLogger => _param.ParameterType == typeof( IActivityMonitor ) && _param.Name == "monitor";

        /// <summary>
        /// Stores the index of the runtime value to use. 0 for null, Positive for objects collected in BuildValueCollector, the negative IndexOrdered+1 for StObj
        /// and Int32.MaxValue for the setup Logger.
        /// </summary>
        internal int BuilderValueIndex;

        public override string ToString()
        {
            string s = $"Construct parameter '{Name}' (n°{Index+1}) for '{Owner}'";
            if( (Kind & StObjMutableReferenceKind.Container) != 0 ) s += " (Container)";
            return s;
        }

        public void SetParameterValue( object value )
        {
            Value = value;
        }

        IStObjResult IStObjFinalParameter.Owner => Owner; 
    }
}
