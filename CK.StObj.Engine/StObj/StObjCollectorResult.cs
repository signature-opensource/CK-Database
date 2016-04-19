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
        IReadOnlyList<MutableItem> _orderedStObjs;
        bool _fatal;

        internal StObjCollectorResult( StObjMapper owner, AmbientContractCollectorResult<StObjContextualMapper,StObjTypeInfo, MutableItem> contractResult )
        {
            Debug.Assert( contractResult != null );
            _contractResult = contractResult;
            foreach( var r in contractResult.Contexts )
            {
                var c = Add( new StObjCollectorContextualResult( r ) );
                _totalSpecializationCount += c._specializations.Length;
            }
            _buildValueCollector = new BuildValueCollector();
        }

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
        public int TotalSpecializationCount
        {
            get { return _totalSpecializationCount; }
        }

        /// <summary>
        /// Gets all the <see cref="IStObjResult"/> ordered by their dependencies.
        /// Empty if <see cref="HasFatalError"/> is true.
        /// </summary>
        public IReadOnlyList<IStObjResult> OrderedStObjs
        {
            get { return _orderedStObjs; }
        }

        internal BuildValueCollector BuildValueCollector
        {
            get { return _buildValueCollector; }
        }

        internal IEnumerable<MutableItem> AllMutableItems
        {
            get { return Contexts.SelectMany( r => r.InternalMapper.RawMappings.Values ); }
        }

        internal IEnumerable<MutableItem> FindHighestImplFor( Type t )
        {
            return Contexts.Select( r => r.InternalMapper.ToHighestImpl( t ) ).Where( m => m != null );
        }

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
