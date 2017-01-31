#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObj\Impl\StObj.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace CK.Core
{
    class StObj : IStObj
    {
        StObj _generalization;
        StObj _specialization;
        StObj _leafSpecialization;
        int _cacheIndex;
        StObjContext _context;
        readonly StObjContextRoot _root;
        readonly Type _type;
        int[] _constructParametersIndex;
        MethodInfo _construct;

        struct PropertySetter
        {
            public PropertyInfo Property;
            public int Index;
        }
        PropertySetter[] _preConstruct;
        PropertySetter[] _postBuild;

        public StObj( StObjContextRoot root, Type t )
        {
            _root = root;
            _type = t;
        }

        public IContextualStObjMap Context => _context; 

        public Type ObjectType => _type; 

        public IStObj Generalization => _generalization; 

        public IStObj Specialization => _specialization; 

        public IStObj LeafSpecialization => _leafSpecialization; 

        public int CacheIndex => _cacheIndex; 

        internal void Initialize( BinaryReader r )
        {
            _context = _root.DoFindContext( r.ReadString() );
            int idx = r.ReadInt32();
            if( idx >= 0 ) _generalization = _root.StObjs[idx];
            idx = r.ReadInt32();
            if( idx >= 0 ) _specialization = _root.StObjs[idx];
            _leafSpecialization = _root.StObjs[r.ReadInt32()];
            _cacheIndex = r.ReadInt32();
            
            int ctorParams = r.ReadInt32();
            if( ctorParams >= 0 )
            {
                _constructParametersIndex = new int[ctorParams];
                for( int i = 0; i < ctorParams; ++i )
                {
                    _constructParametersIndex[i] = r.ReadInt32();
                }
                Type actualType = _type.Namespace != "<CK>" ? _type : _type.GetTypeInfo().BaseType;
                _construct = actualType.GetMethod( "Construct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly );
            }
            _preConstruct = ReadPropertySetters( r );
            if( _specialization == null ) _postBuild = ReadPropertySetters( r );
        }

        private static PropertySetter[] ReadPropertySetters( BinaryReader r )
        {
            PropertySetter[] setters = null;
            int nb = r.ReadInt32();
            if( nb > 0 )
            {
                setters = new PropertySetter[nb];
                for( int i = 0; i < nb; ++i )
                {
                    Type t = SimpleTypeFinder.WeakResolver( r.ReadString(), true );
                    setters[i].Property = t.GetProperty( r.ReadString(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
                    setters[i].Index = r.ReadInt32();
                }
            }
            return setters;
        }

        internal void CallConstruct( IActivityMonitor monitor, Func<int, object> itemResolver, object instance )
        {
            if( _preConstruct != null )
            {
                foreach( var p in _preConstruct )
                {
                    p.Property.SetValue( instance, GetValueFromIndex( itemResolver, p.Index ), null );
                }
            }
            if( _construct == null ) return;
            object[] parameters = new object[_constructParametersIndex.Length];
            for( int i = 0; i < _constructParametersIndex.Length; ++i )
            {
                int idx = _constructParametersIndex[i];
                if( idx == Int32.MaxValue ) parameters[i] = monitor;
                else parameters[i] = GetValueFromIndex( itemResolver, idx );
            }
            _construct.Invoke( instance, parameters );
        }

        internal void SetPostBuilProperties( Func<int, object> itemResolver, object instance )
        {
            if( _postBuild != null )
            {
                foreach( var p in _postBuild )
                {
                    p.Property.SetValue( instance, GetValueFromIndex( itemResolver, p.Index ), null );
                }
            }
        }

        object GetValueFromIndex( Func<int, object> itemResolver, int idx )
        {
            return idx < 0 ? itemResolver( -(idx + 1) ) : _root.BuilderValues[idx];
        }

    }
}
