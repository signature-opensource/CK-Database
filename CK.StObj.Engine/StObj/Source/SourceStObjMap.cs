using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.StObj
{
    class SourceStObj : IStObj
    {
        public SourceStObj( IContextualStObjMap c, Type objectType, IStObj g )
        {
            Context = c;
            ObjectType = objectType;
            Generalization = g;
        }

        public Type ObjectType { get; }

        public IContextualStObjMap Context { get; }

        public IStObj Generalization { get; }

        public IStObj Specialization { get; internal set; }
    }


    class SourceContextualStObjMap : IContextualStObjMap
    {
        readonly IStObjMap _allContexts;
        readonly string _context;
        readonly Dictionary<Type, object> _mappings;

        public IEnumerable<object> Implementations => _mappings.Values;

        public IEnumerable<StObjImplementation> StObjs => throw new NotImplementedException();

        public IEnumerable<KeyValuePair<Type, object>> Mappings => _mappings;

        public IStObjMap AllContexts => _allContexts;

        public string Context => _context;

        public int MappedTypeCount => throw new NotImplementedException();

        public IEnumerable<Type> Types => throw new NotImplementedException();

        IContextualRoot<IContextualTypeMap> IContextualTypeMap.AllContexts => throw new NotImplementedException();

        public bool IsMapped( Type t )
        {
            throw new NotImplementedException();
        }

        public object Obtain( Type t )
        {
            throw new NotImplementedException();
        }

        public IStObj ToLeaf( Type t )
        {
            if( t == null ) throw new ArgumentNullException( "t" );
        }

        public Type ToLeafType( Type t )
        {
            throw new NotImplementedException();
        }
    }


    class SourceStObjMap : IStObjMap
    {
        public SourceStObjMap()
        {

        }

        public IEnumerable<StObjImplementation> AllStObjs => Contexts.SelectMany( c => c.StObjs );

        public IContextualStObjMap Default { get; }

        public IReadOnlyCollection<IContextualStObjMap> Contexts { get; }

        public IContextualStObjMap FindContext( string context ) => Contexts.FirstOrDefault( c => ReferenceEquals( c.Context, context ?? String.Empty ) );
    }
}
