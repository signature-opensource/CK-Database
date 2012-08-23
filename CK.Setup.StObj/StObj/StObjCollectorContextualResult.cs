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
        readonly AmbiantContractCollectorContextualResult _contractResult;
        readonly StObjContextualMapper _mappings;
        bool _fatalError;

        internal StObjCollectorContextualResult( AmbiantContractCollectorContextualResult contractResult, StObjContextualMapper mappings )
        {
            _contractResult = contractResult;
            _mappings = mappings;
        }

        /// <summary>
        /// Gets the <see cref="Type"/> that identifies this context. Null for default context.
        /// </summary>
        public Type Context
        {
            get { return _contractResult.Context; }
        }

        public bool HasFatalError
        {
            get { return _fatalError || _contractResult.HasFatalError; }
        }

        public IStObjContextualMapper StObjMapper
        {
            get { return _mappings; }
        }

        internal AmbiantContractCollectorContextualResult AmbiantContractResult
        {
            get { return _contractResult; }
        }

        internal void Add( MutableItem item )
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
