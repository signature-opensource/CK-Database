using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
namespace CK.StObj
{

    class GStObj : IStObj
    {
        public GStObj( IStObjRuntimeBuilder rb, string t, IStObj g, string actualType )
        {
            ObjectType = Type.GetType( t );
            Generalization = g;
            if( actualType != null )
            {
                Instance = rb.CreateInstance( actualType == t ? ObjectType : Type.GetType( actualType ) );
                Leaf = this;
            }
        }

        public Type ObjectType { get; }

        public IContextualStObjMap Context { get; internal set; }

        public IStObj Generalization { get; }

        public IStObj Specialization { get; internal set; }

        internal object Instance;

        internal GStObj Leaf;

        internal StObjImplementation AsStObjImplementation => new StObjImplementation( this, Instance );
    }

    class GContext : IContextualStObjMap
    {
        readonly Dictionary<Type, GStObj> _mappings;

        public GContext( IStObjMap allContexts, Dictionary<Type, GStObj> map, string name )
        {
            AllContexts = allContexts;
            _mappings = map;
            Context = name;
            foreach( var gs in map.Values ) gs.Context = this;
        }

        public IEnumerable<object> Implementations => _mappings.Values.Where( s => s.Specialization == null ).Select( s => s.Instance );

        public IEnumerable<StObjImplementation> StObjs => _mappings.Values.Select( v => v.AsStObjImplementation );

        public IEnumerable<KeyValuePair<Type, object>> Mappings => _mappings.Select( v => new KeyValuePair<Type, object>( v.Key, v.Value.Instance ) );

        public IStObjMap AllContexts { get; }

        public string Context { get; }

        public int MappedTypeCount => _mappings.Count;

        public IEnumerable<Type> Types => _mappings.Keys;

        IContextualRoot<IContextualTypeMap> IContextualTypeMap.AllContexts => AllContexts;

        public bool IsMapped( Type t ) => _mappings.ContainsKey( t );

        public object Obtain( Type t ) => GToLeaf( t )?.Instance;

        public IStObj ToLeaf( Type t ) => GToLeaf( t );

        public Type ToLeafType( Type t ) => GToLeaf( t )?.ObjectType;

        GStObj GToLeaf( Type t )
        {
            GStObj s;
            if( _mappings.TryGetValue( t, out s ) )
            {
                return s.Leaf;
            }
            return null;
        }
    }
    public class GeneratedRootContext : IStObjMap
    {
        readonly GContext[] _contexts;
        readonly GStObj[] _stObjs;
        public GeneratedRootContext( IActivityMonitor monitor, IStObjRuntimeBuilder rb )
        {
            _stObjs = new GStObj[4];
            _stObjs[0] = new GStObj( rb, @"CK.StObj.Engine.Tests.DynamicGenerationTests+PostBuildSet+B, CK.StObj.Engine.Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27", null, null );
            _stObjs[1] = new GStObj( rb, @"CK.StObj.Engine.Tests.DynamicGenerationTests+PostBuildSet+BSpec, CK.StObj.Engine.Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27", _stObjs[0], @"CK._g.BSpec1" );
            _stObjs[2] = new GStObj( rb, @"CK.StObj.Engine.Tests.DynamicGenerationTests+PostBuildSet+A, CK.StObj.Engine.Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27", null, null );
            _stObjs[3] = new GStObj( rb, @"CK.StObj.Engine.Tests.DynamicGenerationTests+PostBuildSet+ASpec, CK.StObj.Engine.Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27", _stObjs[2], @"CK._g.ASpec2" );
            _contexts = new GContext[1];
            Dictionary<Type, GStObj> map = new Dictionary<Type, GStObj>();
            map.Add( Type.GetType( @"CK.StObj.Engine.Tests.DynamicGenerationTests+PostBuildSet+B, CK.StObj.Engine.Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27" ), _stObjs[0] );
            map.Add( Type.GetType( @"CK.StObj.Engine.Tests.DynamicGenerationTests+PostBuildSet+BSpec, CK.StObj.Engine.Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27" ), _stObjs[1] );
            map.Add( Type.GetType( @"CK.StObj.Engine.Tests.DynamicGenerationTests+PostBuildSet+A, CK.StObj.Engine.Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27" ), _stObjs[2] );
            map.Add( Type.GetType( @"CK.StObj.Engine.Tests.DynamicGenerationTests+PostBuildSet+ASpec, CK.StObj.Engine.Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27" ), _stObjs[3] );
            _contexts[0] = new GContext( this, map, @"" );
            Default = _contexts[0];
            int iStObj = 4;
            while( --iStObj >= 0 )
            {
                var o = _stObjs[iStObj];
                if( o.Specialization == null )
                {
                    GStObj g = (GStObj)o.Generalization;
                    while( g != null )
                    {
                        g.Specialization = o;
                        g.Instance = o.Instance;
                        g.Context = o.Context;
                        g.Leaf = o.Leaf;
                        o = g;
                        g = (GStObj)o.Generalization;
                    }
                }
            }
            if( _stObjs[1].ObjectType.GetMethod( "StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) == null ) throw new Exception( "NULL: " + @"_stObjs[1].ObjectType.GetMethod( ""StObjConstruct"", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )" );
            _stObjs[1].ObjectType.GetMethod( "StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).Invoke( _stObjs[1].Instance, Array.Empty<object>() );
            Type.GetType( @"CK.StObj.Engine.Tests.DynamicGenerationTests+PostBuildSet+A, CK.StObj.Engine.Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27" ).GetProperty( "StObjPower", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).SetValue( _stObjs[2].Instance, @"This is the A property." );
            if( _stObjs[2].ObjectType.GetMethod( "StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) == null ) throw new Exception( "NULL: " + @"_stObjs[2].ObjectType.GetMethod( ""StObjConstruct"", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )" );
            _stObjs[2].ObjectType.GetMethod( "StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).Invoke( _stObjs[2].Instance, new object[] { monitor, _stObjs[1].Instance, } );
            Type.GetType( @"CK.StObj.Engine.Tests.DynamicGenerationTests+PostBuildSet+ASpec, CK.StObj.Engine.Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27" ).GetProperty( "StObjPower", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).SetValue( _stObjs[3].Instance, @"ASpec level property." );
            if( _stObjs[3].ObjectType.GetMethod( "StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) == null ) throw new Exception( "NULL: " + @"_stObjs[3].ObjectType.GetMethod( ""StObjConstruct"", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )" );
            _stObjs[3].ObjectType.GetMethod( "StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).Invoke( _stObjs[3].Instance, new object[] { monitor, } );
            _stObjs[0].ObjectType.GetProperty( "TheA", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).SetValue( _stObjs[0].Instance, _stObjs[2].Instance );
            _stObjs[0].ObjectType.GetProperty( "TheInjectedA", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).SetValue( _stObjs[0].Instance, _stObjs[2].Instance );
            _stObjs[0].ObjectType.GetProperty( "TheA", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).SetValue( _stObjs[1].Instance, _stObjs[2].Instance );
            _stObjs[0].ObjectType.GetProperty( "TheInjectedA", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).SetValue( _stObjs[1].Instance, _stObjs[2].Instance );
            _stObjs[2].ObjectType.GetProperty( "TheB", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).SetValue( _stObjs[2].Instance, _stObjs[1].Instance );
            _stObjs[2].ObjectType.GetProperty( "TheB", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).SetValue( _stObjs[3].Instance, _stObjs[1].Instance );
            _stObjs[2].ObjectType.GetMethod( "StObjInitialize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).Invoke( _stObjs[2].Instance, new object[] { monitor, _stObjs[2].Context } ); _stObjs[3].ObjectType.GetMethod( "StObjInitialize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).Invoke( _stObjs[3].Instance, new object[] { monitor, _stObjs[3].Context } );
        }
        public IEnumerable<StObjImplementation> AllStObjs => Contexts.SelectMany( c => c.StObjs );
        public IContextualStObjMap Default { get; }
        public IReadOnlyCollection<IContextualStObjMap> Contexts => _contexts;
        public IContextualStObjMap FindContext( string context ) => Contexts.FirstOrDefault( c => ReferenceEquals( c.Context, context ?? String.Empty ) );
    }
}
namespace CK._g
{
    public class BSpec1 : CK.StObj.Engine.Tests.DynamicGenerationTests.PostBuildSet.BSpec
    {
        public BSpec1() { }
    }
    public class ASpec2 : CK.StObj.Engine.Tests.DynamicGenerationTests.PostBuildSet.ASpec
    {
        public ASpec2() { }
    }
}