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
    internal class MutableParameter : MutableResolvableReference, IMutableParameter, IParameter
    {
        ParameterInfo _param;

        internal MutableParameter( MutableItem owner, ParameterInfo param, bool isContainer )
            : base( owner, isContainer ? MutableReferenceKind.ConstructParameter|MutableReferenceKind.Container : MutableReferenceKind.ConstructParameter )
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
            get { return _param.ParameterType == typeof( IActivityLogger ) && _param.Name == "logger"; }
        }

        public override string ToString()
        {
            string s = String.Format( "Construct parameter '{0}' (n°{1}) for '{2}'", Name, Index+1, Owner.ToString() );
            if( (Kind & MutableReferenceKind.Container) != 0 ) s += " (Container)";
            return s;
        }

        IStObj IResolvableReference.Owner
        {
            get { return (IStObj)Owner; }
        }

    }
}
