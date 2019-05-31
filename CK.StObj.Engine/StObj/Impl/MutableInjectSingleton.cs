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
        internal readonly InjectSingletonInfo InjecttInfo;

        internal MutableInjectSingleton( MutableItem owner, InjectSingletonInfo info )
            : base( owner, StObjMutableReferenceKind.SingletonReference )
        {
            InjecttInfo = info;
            Type = InjecttInfo.PropertyType;
            IsOptional = InjecttInfo.IsOptional;
        }

        public override string Name => InjecttInfo.Name;

        internal override string KindName => "InjectSingleton";

        internal override Type UnderlyingType => InjecttInfo.PropertyType;

        public override string ToString() => $"Inject Singleton '{Name}' of '{Owner}'";

    }
}
