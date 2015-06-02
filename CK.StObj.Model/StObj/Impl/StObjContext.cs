#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObj\Impl\StObjContext.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CK.Core
{
    internal class StObjContext : IContextualStObjMap
    {
        readonly StObjContextRoot _root;
        readonly string _name;
        readonly Dictionary<Type,int> _mappings;

        internal StObjContext( StObjContextRoot root, string name, Dictionary<Type, int> mappings )
        {
            Debug.Assert( name != null );
            _root = root;
            _name = name;
            _mappings = mappings;
        }

        public string Context
        {
            get { return _name; }
        }

        IContextualRoot<IContextualTypeMap> IContextualTypeMap.AllContexts
        {
            get { return _root; }
        }

        public IEnumerable<Type> Types 
        { 
            get { return _mappings.Keys; } 
        }

        public IStObjMap AllContexts
        {
            get { return _root; }
        }

        public int MappedTypeCount
        {
            get { return _mappings.Count; }
        }

        public Type ToLeafType( Type t )
        {
            IStObj o = ToLeaf( t );
            return o != null ? o.ObjectType : null;
        }

        public IStObj ToLeaf( Type t )
        {
            if( t == null ) throw new ArgumentNullException( "t" );
            int idx;
            if( _mappings.TryGetValue( t, out idx ) )
            {
                return _root.StObjs[idx];
            }
            return null;
        }

        public bool IsMapped( Type t )
        {
            return _mappings.ContainsKey( t );
        }

        public object Obtain( Type t )
        {
            int idx;
            if( _mappings.TryGetValue( t, out idx ) )
            {
                return _root.SingletonCache.Get( _root.StObjs[idx].CacheIndex );
            }
            return null;
        }
    }
}
