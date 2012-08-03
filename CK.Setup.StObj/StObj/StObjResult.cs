using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    public class StObjResult : IContextResult, IDependentItemDiscoverer, IStObjMapper
    {
        readonly AmbiantContractResult _contractResult;
        readonly Dictionary<Type,IStObjDependentItem> _items;
        bool _fatalError;

        internal StObjResult( AmbiantContractResult contractResult )
        {
            _contractResult = contractResult;
            _items = new Dictionary<Type, IStObjDependentItem>();
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

        public IStObjMapper StObjMapper
        {
            get { return this; }
        }

        internal void Add( IStObjDependentItem item )
        {
            _items.Add( item.ItemType, item );
        }

        internal void SetFatal()
        {
            _fatalError = true;
        }

        internal void InitDependentItems( IActivityLogger logger )
        {
            Debug.Assert( !_fatalError );
            foreach( var e in _items.Values ) e.InitDependentItem( logger, this );
        }

        public IEnumerable<IDependentItem> GetOtherItemsToRegister()
        {
            return _items != null ? (IEnumerable<IDependentItem>)_items.Values : ReadOnlyListEmpty<IDependentItem>.Empty;
        }

        #region IStObjMapper Members

        int IStObjMapper.Count
        {
            get { return _items.Count; }
        }

        IAmbiantTypeMapper IStObjMapper.Mappings
        {
            get { return _contractResult.Mappings; }
        }

        IStObjDependentItem IStObjMapper.this[Type t]
        {
            get 
            {
                IStObjDependentItem r = null;
                if( t != null )
                {
                    if( !_items.TryGetValue( t, out r ) )
                    {
                        t = _contractResult.Mappings[t];
                        if( t != null )
                        {
                            _items.TryGetValue( t, out r );
                        }
                    }
                }
                return r; 
            }
        }

        #endregion
    }
}
