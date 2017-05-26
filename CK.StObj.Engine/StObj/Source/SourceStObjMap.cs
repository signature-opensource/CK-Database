using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.StObj
{
    class GStObj : IStObj
    {
        public GStObj( Type t, IStObj g )
        {
            ObjectType = t;
            Generalization = g;
        }

        public Type ObjectType { get; }

        public IContextualStObjMap Context { get; internal set; }

        public IStObj Generalization { get; }

        public IStObj Specialization { get; internal set; }

        internal object Instance;

        internal StObjImplementation AsStObjImplementation => new StObjImplementation( this, Instance );
    }


    class GContext : IContextualStObjMap
    {
        readonly Dictionary<Type, GStObj> _mappings;

        public GContext( IStObjMap allContexts, Dictionary<Type, GStObj> map, string name)
        {
            AllContexts = allContexts;
            Context = name;
            Implementations = _mappings.Values.Distinct().ToArray();
        }

        public IEnumerable<object> Implementations { get; }

        public IEnumerable<StObjImplementation> StObjs => _mappings.Values.Select( v => v.AsStObjImplementation );

        public IEnumerable<KeyValuePair<Type, object>> Mappings => _mappings.Select( v => new KeyValuePair<Type, object>( v.Key, v.Value.Instance ) );

        public IStObjMap AllContexts { get; }

        public string Context { get; }

        public int MappedTypeCount => _mappings.Count;

        public IEnumerable<Type> Types => _mappings.Keys;

        IContextualRoot<IContextualTypeMap> IContextualTypeMap.AllContexts => AllContexts;

        public bool IsMapped( Type t ) => _mappings.ContainsKey( t );

        public object Obtain( Type t )
        {
            GStObj s;
            if( _mappings.TryGetValue( t, out s) )
            {

            }
        }

        public IStObj ToLeaf( Type t )
        {
            if( t == null ) throw new ArgumentNullException( "t" );
            return null;
        }

        public Type ToLeafType( Type t ) => ToLeaf( t )?.ObjectType;
    }


    class SourceStObjMap : IStObjMap
    {
        public SourceStObjMap( IActivityMonitor monitor, IStObjRuntimeBuilder runtimeBuilder )
        {
        }

        public IEnumerable<StObjImplementation> AllStObjs => Contexts.SelectMany( c => c.StObjs );

        public IContextualStObjMap Default { get; }

        public IReadOnlyCollection<IContextualStObjMap> Contexts { get; }

        public IContextualStObjMap FindContext( string context ) => Contexts.FirstOrDefault( c => ReferenceEquals( c.Context, context ?? String.Empty ) );
    }
}
