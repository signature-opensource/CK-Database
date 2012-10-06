using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class StObjCollectorContextualResult : IContextualResult
    {
        readonly AmbientContractCollectorContextualResult<StObjTypeInfo> _contractResult;
        readonly StObjContextualMapper _mappings;
        readonly IReadOnlyCollection<MutableItem> _itemsEx;
        bool _fatalError;

        internal StObjCollectorContextualResult( AmbientContractCollectorContextualResult<StObjTypeInfo> contractResult, StObjContextualMapper mappings )
        {
            _contractResult = contractResult;
            _mappings = mappings;
            _itemsEx = new ReadOnlyCollectionOnICollection<MutableItem>( _mappings.MutableItems );
        }

        /// <summary>
        /// Gets the <see cref="Type"/> that identifies this context. <see cref="AmbientContractCollector.DefaultContext"/> for the default context.
        /// </summary>
        public Type Context
        {
            get { return _contractResult.Context; }
        }

        /// <summary>
        /// Gets whether this result can be used or not.
        /// </summary>
        public bool HasFatalError
        {
            get { return _fatalError || _contractResult.HasFatalError; }
        }

        /// <summary>
        /// Gets the <see cref="IStObjContextualMapper"/> that exposes structured objects.
        /// </summary>
        public IStObjContextualMapper StObjMapper
        {
            get { return _mappings; }
        }

        /// <summary>
        /// Gets the collection of structure objects that have been collected for this <see cref="Context"/>.
        /// </summary>
        public IReadOnlyCollection<IStObj> StObjItems
        {
            get { return _itemsEx; }
        }

        internal AmbientContractCollectorContextualResult<StObjTypeInfo> AmbientContractResult
        {
            get { return _contractResult; }
        }

        internal void AddStObjConfiguredItem( MutableItem item )
        {
            _mappings.Add( item );
        }

        internal ICollection<MutableItem> MutableItems
        {
            get { return _mappings.MutableItems; }
        }

        internal MutableItem Find( Type t )
        {
            return _mappings.Find( t );
        }

        internal void SetFatal()
        {
            _fatalError = true;
        }

    }
}
