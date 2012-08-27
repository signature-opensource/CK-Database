using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public class ContainerTypeItem : DynamicContainerItem
    {
        readonly Type _itemType;
        readonly ContainerAttribute _attr;
        readonly ContainerTypeItem _inherited;

        internal protected ContainerTypeItem( Type itemType, ContainerAttribute attr )
        {
            _itemType = itemType;
            _attr = attr;
            _inherited = null;
        }

        internal protected ContainerTypeItem( Type itemType, ContainerAttribute attr, ContainerTypeItem inherited )
        {
            _attr = null;
            _inherited = inherited;
        }

        public Type ItemType
        {
            get { return _itemType; }
        }

        public ContainerAttribute Attribute
        {
            get { return _attr; }
        }

        public ContainerTypeItem Inherited
        {
            get { return _inherited; }
        }

        //void ITypedObjectDependentItem.InitDependentItem( IActivityLogger logger, ITypedObjectMapper mapper )
        //{
        //    Container = ((IDependentItemContainer)mapper[_attr.Container]).GetReference();
        //    Requires.Add( _attr.Requires );
        //    RequiredBy.Add( _attr.RequiredBy );
        //    if( _inherited != null ) Requires.Add( _inherited );
        //    FullName = _attr.FullName;
        //    // Fundamental structure (Container & Requires) have been initialized.
        //    // We now calls virtual InitDependentItem that can alter these properties.
        //    InitDependentItem( logger, mapper );
        //}

        ///// <summary>
        ///// Offers specialized typed object an opportunity to alter structural properties.
        ///// Only attributes or directly accessible data must be used here: there must be absolutely no
        ///// decision here based on any dependency.
        ///// </summary>
        ///// <param name="logger">Logger to use.</param>
        ///// <param name="mapper">Contextual mapper to use. Every available <see cref="ITypedObjectDependentItem"/> in the context exist.</param>
        //protected virtual void InitDependentItem( IActivityLogger logger, ITypedObjectMapper mapper )
        //{
        //}

    }
}
