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
        readonly AmbientContractCollectorContextualResult<StObjTypeInfo,MutableItem> _contractResult;
        readonly StObjContextualMapper _mappings;
        readonly internal MutableItem[] _specializations;
        bool _fatalError;

        internal StObjCollectorContextualResult( AmbientContractCollectorContextualResult<StObjTypeInfo, MutableItem> contractResult, StObjContextualMapper mappings )
        {
            _contractResult = contractResult;
            _mappings = mappings;
            _specializations = new MutableItem[_contractResult.ConcreteClasses.Count];
        }

        /// <summary>
        /// Gets the context name. <see cref="String.Empty"/> for the default context.
        /// </summary>
        public string Context
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

        internal AmbientContractCollectorContextualResult<StObjTypeInfo, MutableItem> AmbientContractResult
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
