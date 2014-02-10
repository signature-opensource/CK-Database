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

        public int Index { get { return _param.Position; } }

        public override string Name { get { return _param.Name; } }

        public bool IsRealParameterOptional { get { return _param.IsOptional; } }

        internal override string KindName { get { return "Parameter"; } }

        internal override Type UnderlyingType { get { return _param.ParameterType; } }

        internal bool IsSetupLogger
        {
            get { return _param.ParameterType == typeof( IActivityMonitor ) && _param.Name == "monitor"; }
        }

        /// <summary>
        /// Stores the index of the runtime value to use. 0 for null, Positive for objects collected in BuildValueCollector, the negative IndexOrdered+1 for StObj
        /// and Int32.MaxValue for the setup Logger.
        /// </summary>
        internal int BuilderValueIndex;

        public override string ToString()
        {
            string s = String.Format( "Construct parameter '{0}' (n°{1}) for '{2}'", Name, Index+1, Owner.ToString() );
            if( (Kind & StObjMutableReferenceKind.Container) != 0 ) s += " (Container)";
            return s;
        }

        public void SetParameterValue( object value )
        {
            Value = value;
        }

        IStObjResult IStObjFinalParameter.Owner
        {
            get { return Owner; }
        }
    }
}
