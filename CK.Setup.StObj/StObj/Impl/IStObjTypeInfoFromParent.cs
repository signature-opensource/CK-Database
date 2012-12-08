using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    internal interface IStObjTypeInfoFromParent
    {
        int SpecializationDepth { get; }
        Type Container { get; }
        IReadOnlyList<AmbientPropertyInfo> AmbientProperties { get; }
        IReadOnlyList<AmbientContractInfo> AmbientContracts { get; }
        IReadOnlyList<StObjPropertyInfo> StObjProperties { get; }
        DependentItemKind ItemKind { get; }
        TrackAmbientPropertiesMode TrackAmbientProperties { get; }
    }
}
