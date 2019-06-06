#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\StObjCollectorResult.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using CK.CodeGen;

namespace CK.Setup
{
    /// <summary>
    /// Encapsulates the result of the <see cref="StObjCollector"/> work.
    /// </summary>
    public partial class StObjCollectorResult
    {
        readonly DynamicAssembly _tempAssembly;
        readonly StObjObjectEngineMap _liftedMap;

        internal StObjCollectorResult(
            AmbientTypeCollectorResult typeResult,
            DynamicAssembly tempAssembly,
            Dictionary<string,object> primaryRunCache,
            IReadOnlyList<MutableItem> orderedStObjs )
        {
            AmbientTypeResult = typeResult;
            _liftedMap = AmbientTypeResult?.AmbientObjects?.EngineMap;
            _tempAssembly = tempAssembly;
            if( primaryRunCache != null ) SecondaryRunAccessor = key => primaryRunCache[key];
            OrderedStObjs = orderedStObjs;
        }

        /// <summary>
        /// Gets an accessor for the primary run cache only if this result comes
        /// from a primary run, null otherwise.
        /// </summary>
        public Func<string, object> SecondaryRunAccessor { get; }

        /// <summary>
        /// True if a fatal error occured. Result should be discarded.
        /// </summary>
        public bool HasFatalError => OrderedStObjs == null || (AmbientTypeResult?.HasFatalError ?? false); 

        /// <summary>
        /// Gets the result of the types discovery and analysis.
        /// </summary>
        public AmbientTypeCollectorResult AmbientTypeResult { get; }

        /// <summary>
        /// Gets the <see cref="IStObjObjectEngineMap"/> that extends runtime <see cref="IStObjObjectMap"/>.
        /// </summary>
        public IStObjObjectEngineMap StObjs => _liftedMap;

        /// <summary>
        /// Gets the <see cref="IStObjServiceMap"/>.
        /// </summary>
        public IStObjServiceMap Services => _liftedMap;

        /// <summary>
        /// Gets the name of this StObj map.
        /// Never null, defaults to the empty string.
        /// </summary>
        string MapName => _liftedMap?.MapName ?? String.Empty;

        /// <summary>
        /// Gets all the <see cref="IStObjResult"/> ordered by their dependencies.
        /// Null if <see cref="HasFatalError"/> is true.
        /// </summary>
        public IReadOnlyList<IStObjResult> OrderedStObjs { get; } 

    }
}
