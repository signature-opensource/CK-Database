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
    public partial class StObjCollectorResult : MultiContextualResult<StObjCollectorContextualResult>
    {
        readonly AmbientContractCollectorResult<StObjContextualMapper,StObjTypeInfo,MutableItem> _contractResult;
        readonly int _totalSpecializationCount;
        readonly BuildValueCollector _buildValueCollector;
        readonly DynamicAssembly _tempAssembly;
        readonly Func<string,object> _secondaryRunAccessor;
        IReadOnlyList<MutableItem> _orderedStObjs;
        bool _fatal;

        internal StObjCollectorResult( 
            StObjMapper owner, 
            AmbientContractCollectorResult<StObjContextualMapper,StObjTypeInfo, MutableItem> contractResult,
            DynamicAssembly tempAssembly,
            Dictionary<string,object> primaryRunCache )
        {
            Debug.Assert( contractResult != null );
            _contractResult = contractResult;
            _tempAssembly = tempAssembly;
            if( primaryRunCache != null ) _secondaryRunAccessor = key => primaryRunCache[key];
            foreach( var r in contractResult.Contexts )
            {
                var c = Add( new StObjCollectorContextualResult( r ) );
                _totalSpecializationCount += c._specializations.Length;
            }
            _buildValueCollector = new BuildValueCollector();
        }

        /// <summary>
        /// Gets an accessor for the primary run cache only if this result comes
        /// from a primary run, null otherwise.
        /// </summary>
        public Func<string, object> SecondaryRunAccessor => _secondaryRunAccessor;

        /// <summary>
        /// True if a fatal error occured. Result should be discarded.
        /// </summary>
        public override bool HasFatalError
        {
            get { return _fatal || _contractResult.HasFatalError || base.HasFatalError; }
        }

        /// <summary>
        /// Gets the total number of of specializations.
        /// </summary>
        public int TotalSpecializationCount => _totalSpecializationCount; 

        /// <summary>
        /// Gets all the <see cref="IStObjResult"/> ordered by their dependencies.
        /// Empty if <see cref="HasFatalError"/> is true.
        /// </summary>
        public IReadOnlyList<IStObjResult> OrderedStObjs => _orderedStObjs; 

        internal BuildValueCollector BuildValueCollector => _buildValueCollector; 

        internal IEnumerable<MutableItem> AllMutableItems => Contexts.SelectMany( r => r.InternalMapper.RawMappings.Values ); 

        internal IEnumerable<MutableItem> FindHighestImplFor( Type t ) => Contexts.Select( r => r.InternalMapper.ToHighestImpl( t ) ).Where( m => m != null );

        internal void SetFatal()
        {
            _fatal = true;
            _orderedStObjs = Util.Array.Empty<MutableItem>();
        }

        internal void SetSuccess( IReadOnlyList<MutableItem> ordered )
        {
            Debug.Assert( !HasFatalError && _orderedStObjs == null );
            _orderedStObjs = ordered;
        }
    }
}
