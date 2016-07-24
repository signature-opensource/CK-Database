#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlDatabase\SqlDatabaseItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlDatabaseItem : StObjDynamicContainerItem
    {
        internal readonly SqlDatabaseConnectionItem ConnectionItem;

        class Model : ISetupItem, IDependentItemGroup, IDependentItemGroupRef
        {
            readonly SqlDatabaseItem _holder;

            public Model( SqlDatabaseItem h )
            {
                _holder = h;
            }

            public IDependentItemContainerRef Container => null;

            public string Context => _holder.Context;

            public string Location => _holder.Location;

            public string Name => "Model." + _holder.Name;

            public string FullName => DefaultContextLocNaming.Format( _holder.Context, _holder.Location, Name );

            string IContextLocNaming.TransformArg => null;

            public IDependentItemRef Generalization => null;

            public IEnumerable<IDependentItemGroupRef> Groups => null;

            public IEnumerable<IDependentItemRef> RequiredBy => null;

            public IEnumerable<IDependentItemRef> Requires => new[] { _holder.ConnectionItem };

            public string TransformArg => null;

            public bool Optional => false;

            public IEnumerable<IDependentItemRef> Children => null;

            public object StartDependencySort() => null;
        }

        public SqlDatabaseItem( IActivityMonitor monitor, IStObjSetupData data )
            : base( monitor, data )
        {
            Context = data.StObj.Context.Context;
            Location = ActualObject.Name;
            ConnectionItem = new SqlDatabaseConnectionItem( this );
            Requires.Add( new Model( this ) );
        }

        public new SqlDatabase ActualObject => (SqlDatabase)base.ActualObject;
    }
}
