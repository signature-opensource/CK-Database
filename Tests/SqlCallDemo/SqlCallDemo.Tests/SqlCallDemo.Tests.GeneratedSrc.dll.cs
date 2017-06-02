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
        public GStObj(IStObjRuntimeBuilder rb, Type t, IStObj g, Type actualType)
        {
            ObjectType = t;
            Generalization = g;
            if (actualType != null)
            {
                Instance = rb.CreateInstance(actualType);
                Leaf = this;
            }
        }

        public Type ObjectType { get; }

        public IContextualStObjMap Context { get; internal set; }

        public IStObj Generalization { get; }

        public IStObj Specialization { get; internal set; }

        internal object Instance;

        internal GStObj Leaf;

        internal StObjImplementation AsStObjImplementation => new StObjImplementation(this, Instance);
    }

    class GContext : IContextualStObjMap
    {
        readonly Dictionary<Type, GStObj> _mappings;

        public GContext(GeneratedRootContext allContexts, Dictionary<Type, GStObj> map, string name)
        {
            AllContexts = allContexts;
            _mappings = map;
            Context = name;
            var distinct = new HashSet<object>();
            foreach (var gs in map.Values)
            {
                gs.Context = this;
                distinct.Add(gs.Instance);
            }
            Implementations = distinct.ToArray();
        }

        public IEnumerable<object> Implementations { get; }

        public IEnumerable<StObjImplementation> StObjs => AllContexts._stObjs.Where(s => s.Context == this).Select(s => s.AsStObjImplementation);

        public IEnumerable<KeyValuePair<Type, object>> Mappings => _mappings.Select(v => new KeyValuePair<Type, object>(v.Key, v.Value.Instance));

        internal GeneratedRootContext AllContexts { get; }

        IStObjMap IContextualStObjMap.AllContexts => AllContexts;

        public string Context { get; }

        public int MappedTypeCount => _mappings.Count;

        public IEnumerable<Type> Types => _mappings.Keys;

        IContextualRoot<IContextualTypeMap> IContextualTypeMap.AllContexts => AllContexts;

        public bool IsMapped(Type t) => _mappings.ContainsKey(t);

        public object Obtain(Type t) => GToLeaf(t)?.Instance;

        public IStObj ToLeaf(Type t) => GToLeaf(t);

        public Type ToLeafType(Type t) => GToLeaf(t)?.ObjectType;

        GStObj GToLeaf(Type t)
        {
            GStObj s;
            if (_mappings.TryGetValue(t, out s))
            {
                return s.Leaf;
            }
            return null;
        }
    }
    public class GeneratedRootContext : IStObjMap
    {
        readonly GContext[] _contexts;
        internal readonly GStObj[] _stObjs;
        public GeneratedRootContext(IActivityMonitor monitor, IStObjRuntimeBuilder rb)
        {
            _stObjs = new GStObj[14];
            _stObjs[0] = new GStObj(rb, typeof(CK._g.poco.Factory), null, typeof(CK._g.poco.Factory));
            _stObjs[1] = new GStObj(rb, typeof(CK.SqlServer.Setup.SqlDefaultDatabase), null, typeof(CK.SqlServer.Setup.SqlDefaultDatabase));
            _stObjs[2] = new GStObj(rb, typeof(SqlCallDemo.AllDefaultValuesPackage), null, typeof(CK._g.AllDefaultValuesPackage1));
            _stObjs[3] = new GStObj(rb, typeof(SqlCallDemo.CommandDemo.CmdDemoPackage), null, typeof(CK._g.CmdDemoPackage10));
            _stObjs[4] = new GStObj(rb, typeof(SqlCallDemo.ComplexType.ComplexTypePackage), null, typeof(CK._g.ComplexTypePackage9));
            _stObjs[5] = new GStObj(rb, typeof(SqlCallDemo.FunctionPackage), null, typeof(CK._g.FunctionPackage2));
            _stObjs[6] = new GStObj(rb, typeof(SqlCallDemo.GuidRefTestPackage), null, typeof(CK._g.GuidRefTestPackage3));
            _stObjs[7] = new GStObj(rb, typeof(SqlCallDemo.OutputParameterPackage), null, typeof(CK._g.OutputParameterPackage4));
            _stObjs[8] = new GStObj(rb, typeof(SqlCallDemo.PocoPackage), null, typeof(CK._g.PocoPackage5));
            _stObjs[9] = new GStObj(rb, typeof(SqlCallDemo.ProviderDemo.ProviderDemoPackage), null, typeof(CK._g.ProviderDemoPackage8));
            _stObjs[10] = new GStObj(rb, typeof(SqlCallDemo.PurelyInputLogPackage), null, typeof(CK._g.PurelyInputLogPackage6));
            _stObjs[11] = new GStObj(rb, typeof(SqlCallDemo.ReturnPackage), null, typeof(CK._g.ReturnPackage7));
            _stObjs[12] = new GStObj(rb, typeof(SqlCallDemo.PocoPackageWithAgeAndHeight), null, typeof(SqlCallDemo.PocoPackageWithAgeAndHeight));
            _stObjs[13] = new GStObj(rb, typeof(SqlCallDemo.PocoPackageWithPower), null, typeof(SqlCallDemo.PocoPackageWithPower));
            _contexts = new GContext[1];
            Dictionary<Type, GStObj> map = new Dictionary<Type, GStObj>();
            map.Add(Type.GetType(@"SqlCallDemo.AllDefaultValuesPackage, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[2]);
            map.Add(Type.GetType(@"SqlCallDemo.FunctionPackage, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[5]);
            map.Add(Type.GetType(@"SqlCallDemo.GuidRefTestPackage, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[6]);
            map.Add(Type.GetType(@"SqlCallDemo.OutputParameterPackage, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[7]);
            map.Add(Type.GetType(@"SqlCallDemo.PocoPackage, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[8]);
            map.Add(Type.GetType(@"SqlCallDemo.PocoPackageWithAgeAndHeight, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[12]);
            map.Add(Type.GetType(@"SqlCallDemo.PocoPackageWithPower, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[13]);
            map.Add(Type.GetType(@"SqlCallDemo.PurelyInputLogPackage, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[10]);
            map.Add(Type.GetType(@"SqlCallDemo.ReturnPackage, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[11]);
            map.Add(Type.GetType(@"SqlCallDemo.ProviderDemo.ProviderDemoPackage, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[9]);
            map.Add(Type.GetType(@"SqlCallDemo.ComplexType.ComplexTypePackage, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[4]);
            map.Add(Type.GetType(@"SqlCallDemo.CommandDemo.CmdDemoPackage, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[3]);
            map.Add(Type.GetType(@"CK.SqlServer.Setup.SqlDefaultDatabase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[1]);
            map.Add(Type.GetType(@"CK._g.poco.Factory, SqlCallDemo.Tests.Generated, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"), _stObjs[0]);
            map.Add(Type.GetType(@"CK.Core.IPocoFactory`1[[SqlCallDemo.IThing, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27]], CK.StObj.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[0]);
            map.Add(Type.GetType(@"CK.Core.IPocoFactory`1[[SqlCallDemo.PocoSupport.IThingReadOnlyProp, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27]], CK.StObj.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[0]);
            map.Add(Type.GetType(@"CK.Core.IPocoFactory`1[[SqlCallDemo.PocoSupport.IThingWithAge, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27]], CK.StObj.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[0]);
            map.Add(Type.GetType(@"CK.Core.IPocoFactory`1[[SqlCallDemo.PocoSupport.IThingWithAgeAndHeight, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27]], CK.StObj.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[0]);
            map.Add(Type.GetType(@"CK.Core.IPocoFactory`1[[SqlCallDemo.PocoSupport.IThingWithPower, SqlCallDemo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27]], CK.StObj.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"), _stObjs[0]);
            _contexts[0] = new GContext(this, map, @"");
            Default = _contexts[0];
            int iStObj = 14;
            while (--iStObj >= 0)
            {
                var o = _stObjs[iStObj];
                if (o.Specialization == null)
                {
                    GStObj g = (GStObj)o.Generalization;
                    while (g != null)
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
            if (_stObjs[1].ObjectType.GetMethod("StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null) throw new Exception("NULL: " + @"_stObjs[1].ObjectType.GetMethod( ""StObjConstruct"", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )");
            _stObjs[1].ObjectType.GetMethod("StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(_stObjs[1].Instance, new object[] { @"Data Source=.;Initial Catalog=CKDB_TEST_SqlCallDemo;Integrated Security=True", });
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Database", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[2].Instance, _stObjs[1].Instance);
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Schema", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[2].Instance, @"CK");
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Database", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[3].Instance, _stObjs[1].Instance);
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Schema", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[3].Instance, @"Command");
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Database", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[4].Instance, _stObjs[1].Instance);
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Schema", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[4].Instance, @"CK");
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Database", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[5].Instance, _stObjs[1].Instance);
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Schema", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[5].Instance, @"CK");
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Database", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[6].Instance, _stObjs[1].Instance);
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Schema", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[6].Instance, @"CK");
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Database", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[7].Instance, _stObjs[1].Instance);
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Schema", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[7].Instance, @"CK");
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Database", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[8].Instance, _stObjs[1].Instance);
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Schema", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[8].Instance, @"CK");
            if (_stObjs[8].ObjectType.GetMethod("StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null) throw new Exception("NULL: " + @"_stObjs[8].ObjectType.GetMethod( ""StObjConstruct"", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )");
            _stObjs[8].ObjectType.GetMethod("StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(_stObjs[8].Instance, new object[] { _stObjs[0].Instance, });
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Database", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[9].Instance, _stObjs[1].Instance);
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Schema", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[9].Instance, @"Provider");
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Database", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[10].Instance, _stObjs[1].Instance);
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Schema", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[10].Instance, @"CK");
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Database", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[11].Instance, _stObjs[1].Instance);
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Schema", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[11].Instance, @"CK");
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Database", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[12].Instance, _stObjs[1].Instance);
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Schema", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[12].Instance, @"CK");
            if (_stObjs[12].ObjectType.GetMethod("StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null) throw new Exception("NULL: " + @"_stObjs[12].ObjectType.GetMethod( ""StObjConstruct"", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )");
            _stObjs[12].ObjectType.GetMethod("StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(_stObjs[12].Instance, new object[] { _stObjs[8].Instance, });
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Database", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[13].Instance, _stObjs[1].Instance);
            Type.GetType(@"CK.SqlServer.Setup.SqlPackageBase, CK.SqlServer.Setup.Model, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27").GetProperty("Schema", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_stObjs[13].Instance, @"CK");
            if (_stObjs[13].ObjectType.GetMethod("StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null) throw new Exception("NULL: " + @"_stObjs[13].ObjectType.GetMethod( ""StObjConstruct"", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic )");
            _stObjs[13].ObjectType.GetMethod("StObjConstruct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(_stObjs[13].Instance, new object[] { _stObjs[8].Instance, _stObjs[12].Instance, });
        }
        public IEnumerable<StObjImplementation> AllStObjs => Contexts.SelectMany(c => c.StObjs);
        public IContextualStObjMap Default { get; }
        public IReadOnlyCollection<IContextualStObjMap> Contexts => _contexts;
        public IContextualStObjMap FindContext(string context) => Contexts.FirstOrDefault(c => ReferenceEquals(c.Context, context ?? String.Empty));
    }
}
namespace CK._g
{
    using System.Data;
    using System.Data.SqlClient;
    public class AllDefaultValuesPackage1 : SqlCallDemo.AllDefaultValuesPackage
    {
        public AllDefaultValuesPackage1() { }
        public override System.String AllDefaultValues(CK.SqlServer.SqlStandardCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd11();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = @"All Defaults";
            tP = cmd_parameters[1];
            tP.Value = 3712;
            tP = cmd_parameters[2];
            tP.Value = 9223372036854775807m;
            tP = cmd_parameters[3];
            tP.Value = -32768;
            tP = cmd_parameters[4];
            tP.Value = 255;
            tP = cmd_parameters[5];
            tP.Value = 1;
            tP = cmd_parameters[6];
            tP.Value = 123456789012345678m;
            tP = cmd_parameters[7];
            tP.Value = 1234567890.0123456789m;
            tP = cmd_parameters[8];
            tP.Value = @"2011-10-26";
            tP = cmd_parameters[9];
            tP.Value = -4.575858E-06;
            tP = cmd_parameters[10];
            tP.Value = -4.5588E-09;
            tP = cmd_parameters[11];
            tP.Value = new System.Byte[] { (byte)10, (byte)59 };
            tP = cmd_parameters[12];
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[12].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR12 = (System.String)tempObj;
            return getR12;
        }
    }
    static class CreatorForSqlCommand
    {
        public static SqlCommand Cmd11()
        {
            var cmd = new SqlCommand(@"CK.sAllDefaultValues");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@NVarChar", SqlDbType.NVarChar, 64);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@Int", SqlDbType.Int);
            cmd.Parameters.Add(p2);
            var p3 = new SqlParameter(@"@BigInt", SqlDbType.BigInt);
            cmd.Parameters.Add(p3);
            var p4 = new SqlParameter(@"@SmallInt", SqlDbType.SmallInt);
            cmd.Parameters.Add(p4);
            var p5 = new SqlParameter(@"@TinyInt", SqlDbType.TinyInt);
            cmd.Parameters.Add(p5);
            var p6 = new SqlParameter(@"@Bit", SqlDbType.Bit);
            cmd.Parameters.Add(p6);
            var p7 = new SqlParameter(@"@Numeric", SqlDbType.Decimal);
            cmd.Parameters.Add(p7);
            var p8 = new SqlParameter(@"@Numeric2010", SqlDbType.Decimal);
            p8.Precision = 20;
            p8.Scale = 10;
            cmd.Parameters.Add(p8);
            var p9 = new SqlParameter(@"@DateTime", SqlDbType.DateTime);
            cmd.Parameters.Add(p9);
            var p10 = new SqlParameter(@"@Float", SqlDbType.Float);
            cmd.Parameters.Add(p10);
            var p11 = new SqlParameter(@"@Real", SqlDbType.Real);
            cmd.Parameters.Add(p11);
            var p12 = new SqlParameter(@"@Bin", SqlDbType.VarBinary, 50);
            cmd.Parameters.Add(p12);
            var p13 = new SqlParameter(@"@TextResult", SqlDbType.NVarChar, 1024);
            p13.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p13);
            return cmd;
        }
        public static SqlCommand Cmd12()
        {
            var cmd = new SqlCommand(@"Command.sCommandRun");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@ActorId", SqlDbType.Int);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@CompanyName", SqlDbType.NVarChar, 128);
            cmd.Parameters.Add(p2);
            var p3 = new SqlParameter(@"@LaunchnDate", SqlDbType.DateTime2);
            cmd.Parameters.Add(p3);
            var p4 = new SqlParameter(@"@Delay", SqlDbType.Int);
            p4.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p4);
            var p5 = new SqlParameter(@"@ActualCompanyName", SqlDbType.NVarChar, 128);
            p5.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p5);
            return cmd;
        }
        public static SqlCommand Cmd15()
        {
            var cmd = new SqlCommand(@"Command.sProtoUserCreate");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@ActorId", SqlDbType.Int);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@UserName", SqlDbType.NVarChar, 255);
            cmd.Parameters.Add(p2);
            var p3 = new SqlParameter(@"@Email", SqlDbType.NVarChar, 255);
            cmd.Parameters.Add(p3);
            var p4 = new SqlParameter(@"@Phone", SqlDbType.VarChar, 42);
            cmd.Parameters.Add(p4);
            var p5 = new SqlParameter(@"@ProtoUserId", SqlDbType.Int);
            p5.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p5);
            return cmd;
        }
        public static SqlCommand Cmd16()
        {
            var cmd = new SqlCommand(@"CK.sComplexTypeStupidEmpty");
            cmd.CommandType = CommandType.StoredProcedure;
            return cmd;
        }
        public static SqlCommand Cmd18()
        {
            var cmd = new SqlCommand(@"CK.sComplexTypeSimple");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@Id", SqlDbType.Int);
            p1.Direction = ParameterDirection.InputOutput;
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@Name", SqlDbType.NVarChar, 50);
            p2.Direction = ParameterDirection.InputOutput;
            cmd.Parameters.Add(p2);
            var p3 = new SqlParameter(@"@CreationDate", SqlDbType.DateTime);
            p3.Direction = ParameterDirection.InputOutput;
            cmd.Parameters.Add(p3);
            var p4 = new SqlParameter(@"@NullableInt", SqlDbType.Int);
            p4.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p4);
            return cmd;
        }
        public static SqlCommand Cmd23()
        {
            var cmd = new SqlCommand(@"CK.fStringFunction");
            cmd.CommandType = CommandType.StoredProcedure;
            var pR = new SqlParameter(@"RETURN_VALUE", SqlDbType.NVarChar, 60);
            cmd.Parameters.Add(pR).Direction = ParameterDirection.ReturnValue;
            var p1 = new SqlParameter(@"@V", SqlDbType.Int);
            cmd.Parameters.Add(p1);
            return cmd;
        }
        public static SqlCommand Cmd25()
        {
            var cmd = new SqlCommand(@"CK.fByteFunction");
            cmd.CommandType = CommandType.StoredProcedure;
            var pR = new SqlParameter(@"RETURN_VALUE", SqlDbType.TinyInt);
            cmd.Parameters.Add(pR).Direction = ParameterDirection.ReturnValue;
            var p1 = new SqlParameter(@"@V", SqlDbType.Int);
            cmd.Parameters.Add(p1);
            return cmd;
        }
        public static SqlCommand Cmd28()
        {
            var cmd = new SqlCommand(@"CK.sWithEnumIO");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@BytePower", SqlDbType.TinyInt);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@Power", SqlDbType.Int);
            p2.Direction = ParameterDirection.InputOutput;
            cmd.Parameters.Add(p2);
            return cmd;
        }
        public static SqlCommand Cmd31()
        {
            var cmd = new SqlCommand(@"CK.sGuidRefTest");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@ReplaceInAndOut", SqlDbType.Bit);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@InOnly", SqlDbType.UniqueIdentifier);
            cmd.Parameters.Add(p2);
            var p3 = new SqlParameter(@"@InAndOut", SqlDbType.UniqueIdentifier);
            p3.Direction = ParameterDirection.InputOutput;
            cmd.Parameters.Add(p3);
            var p4 = new SqlParameter(@"@TextResult", SqlDbType.NVarChar, 128);
            p4.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p4);
            return cmd;
        }
        public static SqlCommand Cmd32()
        {
            var cmd = new SqlCommand(@"CK.sOutputInputParameterWithDefault");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@TextResult", SqlDbType.NVarChar, 128);
            p1.Direction = ParameterDirection.InputOutput;
            cmd.Parameters.Add(p1);
            return cmd;
        }
        public static SqlCommand Cmd33()
        {
            var cmd = new SqlCommand(@"CK.sOutputParameterWithDefault");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@TextResult", SqlDbType.NVarChar, 128);
            p1.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p1);
            return cmd;
        }
        public static SqlCommand Cmd34()
        {
            var cmd = new SqlCommand(@"CK.sPocoThingWrite");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@Name", SqlDbType.VarChar, 50);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@Result", SqlDbType.VarChar, 800);
            p2.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p2);
            var p3 = new SqlParameter(@"@Age", SqlDbType.Int);
            cmd.Parameters.Add(p3);
            var p4 = new SqlParameter(@"@Height", SqlDbType.Int);
            cmd.Parameters.Add(p4);
            var p5 = new SqlParameter(@"@Power", SqlDbType.Int);
            cmd.Parameters.Add(p5);
            return cmd;
        }
        public static SqlCommand Cmd35()
        {
            var cmd = new SqlCommand(@"Provider.sActorOnly");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@ActorId", SqlDbType.Int);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@TextResult", SqlDbType.NVarChar, 128);
            p2.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p2);
            return cmd;
        }
        public static SqlCommand Cmd37()
        {
            var cmd = new SqlCommand(@"Provider.sCultureOnly");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@CultureId", SqlDbType.Int);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@TextResult", SqlDbType.NVarChar, 128);
            p2.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p2);
            return cmd;
        }
        public static SqlCommand Cmd38()
        {
            var cmd = new SqlCommand(@"Provider.sTenantOnly");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@TenantId", SqlDbType.Int);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@TextResult", SqlDbType.NVarChar, 128);
            p2.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p2);
            return cmd;
        }
        public static SqlCommand Cmd39()
        {
            var cmd = new SqlCommand(@"Provider.sActorCulture");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@ActorId", SqlDbType.Int);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@CultureId", SqlDbType.Int);
            cmd.Parameters.Add(p2);
            var p3 = new SqlParameter(@"@TextResult", SqlDbType.NVarChar, 128);
            p3.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p3);
            return cmd;
        }
        public static SqlCommand Cmd41()
        {
            var cmd = new SqlCommand(@"Provider.sCultureTenant");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@CultureId", SqlDbType.Int);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@TenantId", SqlDbType.Int);
            cmd.Parameters.Add(p2);
            var p3 = new SqlParameter(@"@TextResult", SqlDbType.NVarChar, 128);
            p3.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p3);
            return cmd;
        }
        public static SqlCommand Cmd42()
        {
            var cmd = new SqlCommand(@"Provider.sAllContexts");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@ActorId", SqlDbType.Int);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@CultureId", SqlDbType.Int);
            cmd.Parameters.Add(p2);
            var p3 = new SqlParameter(@"@TenantId", SqlDbType.Int);
            cmd.Parameters.Add(p3);
            var p4 = new SqlParameter(@"@TextResult", SqlDbType.NVarChar, 128);
            p4.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p4);
            return cmd;
        }
        public static SqlCommand Cmd44()
        {
            var cmd = new SqlCommand(@"CK.sPurelyInputSimpleLog");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@LogText", SqlDbType.NVarChar, 250);
            cmd.Parameters.Add(p1);
            return cmd;
        }
        public static SqlCommand Cmd45()
        {
            var cmd = new SqlCommand(@"CK.sPurelyInputLog");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@OneMore", SqlDbType.Bit);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@LogText", SqlDbType.NVarChar, 250);
            cmd.Parameters.Add(p2);
            var p3 = new SqlParameter(@"@WaitTimeMS", SqlDbType.Int);
            cmd.Parameters.Add(p3);
            return cmd;
        }
        public static SqlCommand Cmd46()
        {
            var cmd = new SqlCommand(@"CK.sStringReturn");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@V", SqlDbType.Int);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@TextResult", SqlDbType.NVarChar, 128);
            p2.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p2);
            return cmd;
        }
        public static SqlCommand Cmd47()
        {
            var cmd = new SqlCommand(@"CK.sIntReturn");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@V", SqlDbType.Int);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@Result", SqlDbType.Int);
            p2.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p2);
            return cmd;
        }
        public static SqlCommand Cmd49()
        {
            var cmd = new SqlCommand(@"CK.sIntReturnWithActor");
            cmd.CommandType = CommandType.StoredProcedure;
            var p1 = new SqlParameter(@"@ActorId", SqlDbType.Int);
            cmd.Parameters.Add(p1);
            var p2 = new SqlParameter(@"@Def", SqlDbType.NVarChar, 64);
            cmd.Parameters.Add(p2);
            var p3 = new SqlParameter(@"@Result", SqlDbType.Int);
            p3.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p3);
            return cmd;
        }
    }
    public class CmdDemoPackage10 : SqlCallDemo.CommandDemo.CmdDemoPackage
    {
        public CmdDemoPackage10() { }
        public override System.Threading.Tasks.Task<SqlCallDemo.CommandDemo.CmdDemo.ResultPOCO> RunCommandAsync(CK.SqlServer.ISqlCallContext ctx, SqlCallDemo.CommandDemo.CmdDemo cmd)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd12();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)cmd.ActorId ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)cmd.CompanyName ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)cmd.LaunchnDate ?? DBNull.Value;
            tP = cmd_parameters[3];
            tP = cmd_parameters[4];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<SqlCallDemo.CommandDemo.CmdDemo.ResultPOCO>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f13, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task<SqlCallDemo.CommandDemo.CmdDemo.ResultReadOnly> RunCommandROAsync(CK.SqlServer.ISqlCallContext ctx, SqlCallDemo.CommandDemo.CmdDemo cmd)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd12();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)cmd.ActorId ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)cmd.CompanyName ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)cmd.LaunchnDate ?? DBNull.Value;
            tP = cmd_parameters[3];
            tP = cmd_parameters[4];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<SqlCallDemo.CommandDemo.CmdDemo.ResultReadOnly>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f14, default(System.Threading.CancellationToken));
        }
        public override System.Int32 CreateProtoUser(CK.SqlServer.ISqlCallContext ctx, System.Int32 actorId, SqlCallDemo.CommandDemo.ProtoUserData data)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd15();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)actorId ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)data.UserName ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)data.Email ?? DBNull.Value;
            tP = cmd_parameters[3];
            tP.Value = (object)data.Phone ?? DBNull.Value;
            tP = cmd_parameters[4];
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[4].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR4 = (System.Int32)tempObj;
            return getR4;
        }
    }
    static class _build_func_
    {
        internal static readonly Func<System.Data.SqlClient.SqlCommand, SqlCallDemo.CommandDemo.CmdDemo.ResultPOCO> _f13 = f13;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, SqlCallDemo.CommandDemo.CmdDemo.ResultReadOnly> _f14 = f14;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, SqlCallDemo.ComplexType.ComplexTypeStupidEmpty> _f17 = f17;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, SqlCallDemo.ComplexType.ComplexTypeSimple> _f19 = f19;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, SqlCallDemo.ComplexType.ComplexTypeSimpleWithCtor> _f20 = f20;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, SqlCallDemo.ComplexType.ComplexTypeSimpleWithExtraProperty> _f21 = f21;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, SqlCallDemo.ComplexType.ComplexTypeSimpleWithMissingProperty> _f22 = f22;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, System.String> _f24 = f24;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, System.Byte> _f26 = f26;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, System.Nullable<System.Byte>> _f27 = f27;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, SqlCallDemo.FunctionPackage.Power> _f29 = f29;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, System.Nullable<SqlCallDemo.FunctionPackage.Power>> _f30 = f30;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, System.String> _f36 = f36;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, System.String> _f40 = f40;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, System.String> _f43 = f43;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, System.Int32> _f48 = f48;
        internal static readonly Func<System.Data.SqlClient.SqlCommand, System.Int32> _f50 = f50;
        private static SqlCallDemo.CommandDemo.CmdDemo.ResultPOCO f13(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            var oR = new SqlCallDemo.CommandDemo.CmdDemo.ResultPOCO();
            object tempObj;
            tempObj = parameters[3].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR3 = (System.Int32)tempObj;
            oR.Delay = getR3; tempObj = parameters[4].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR4 = (System.String)tempObj;
            oR.ActualCompanyName = getR4; return oR;
        }
        private static SqlCallDemo.CommandDemo.CmdDemo.ResultReadOnly f14(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            object tempObj;
            tempObj = parameters[3].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR3 = (System.Int32)tempObj;
            tempObj = parameters[4].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR4 = (System.String)tempObj;
            var oR = new SqlCallDemo.CommandDemo.CmdDemo.ResultReadOnly(getR3, getR4);
            return oR;
        }
        private static SqlCallDemo.ComplexType.ComplexTypeStupidEmpty f17(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            var oR = new SqlCallDemo.ComplexType.ComplexTypeStupidEmpty();
            return oR;
        }
        private static SqlCallDemo.ComplexType.ComplexTypeSimple f19(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            var oR = new SqlCallDemo.ComplexType.ComplexTypeSimple();
            object tempObj;
            tempObj = parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.Int32)tempObj;
            oR.Id = getR0; tempObj = parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            oR.Name = getR1; tempObj = parameters[2].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR2 = (System.DateTime)tempObj;
            oR.CreationDate = getR2; tempObj = parameters[3].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR3 = (System.Nullable<System.Int32>)tempObj;
            oR.NullableInt = getR3; return oR;
        }
        private static SqlCallDemo.ComplexType.ComplexTypeSimpleWithCtor f20(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            object tempObj;
            tempObj = parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.Int32)tempObj;
            tempObj = parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            tempObj = parameters[2].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR2 = (System.DateTime)tempObj;
            var oR = new SqlCallDemo.ComplexType.ComplexTypeSimpleWithCtor(getR0, getR1, getR2);
            return oR;
        }
        private static SqlCallDemo.ComplexType.ComplexTypeSimpleWithExtraProperty f21(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            var oR = new SqlCallDemo.ComplexType.ComplexTypeSimpleWithExtraProperty();
            object tempObj;
            tempObj = parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.Int32)tempObj;
            oR.Id = getR0; tempObj = parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            oR.Name = getR1; tempObj = parameters[2].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR2 = (System.DateTime)tempObj;
            oR.CreationDate = getR2; return oR;
        }
        private static SqlCallDemo.ComplexType.ComplexTypeSimpleWithMissingProperty f22(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            var oR = new SqlCallDemo.ComplexType.ComplexTypeSimpleWithMissingProperty();
            object tempObj;
            tempObj = parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            oR.Name = getR1; return oR;
        }
        private static System.String f24(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            object tempObj;
            tempObj = parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.String)tempObj;
            return getR0;
        }
        private static System.Byte f26(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            object tempObj;
            tempObj = parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.Byte)tempObj;
            return getR0;
        }
        private static System.Nullable<System.Byte> f27(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            object tempObj;
            tempObj = parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.Nullable<System.Byte>)tempObj;
            return getR0;
        }
        private static SqlCallDemo.FunctionPackage.Power f29(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            object tempObj;
            tempObj = parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (SqlCallDemo.FunctionPackage.Power)tempObj;
            return getR1;
        }
        private static System.Nullable<SqlCallDemo.FunctionPackage.Power> f30(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            object tempObj;
            tempObj = parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.Nullable<SqlCallDemo.FunctionPackage.Power>)tempObj;
            return getR1;
        }
        private static System.String f36(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            object tempObj;
            tempObj = parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            return getR1;
        }
        private static System.String f40(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            object tempObj;
            tempObj = parameters[2].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR2 = (System.String)tempObj;
            return getR2;
        }
        private static System.String f43(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            object tempObj;
            tempObj = parameters[3].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR3 = (System.String)tempObj;
            return getR3;
        }
        private static System.Int32 f48(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            object tempObj;
            tempObj = parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.Int32)tempObj;
            return getR1;
        }
        private static System.Int32 f50(System.Data.SqlClient.SqlCommand c)
        {
            var parameters = c.Parameters;
            object tempObj;
            tempObj = parameters[2].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR2 = (System.Int32)tempObj;
            return getR2;
        }
    }
    public class ComplexTypePackage9 : SqlCallDemo.ComplexType.ComplexTypePackage
    {
        public ComplexTypePackage9() { }
        public override System.Threading.Tasks.Task<SqlCallDemo.ComplexType.ComplexTypeStupidEmpty> GetComplexTypeStupidEmptyAsync(CK.SqlServer.ISqlCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd16();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            return ctx.Executor.ExecuteNonQueryAsyncTyped<SqlCallDemo.ComplexType.ComplexTypeStupidEmpty>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f17, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task<SqlCallDemo.ComplexType.ComplexTypeSimple> GetComplexTypeSimpleAsync(CK.SqlServer.ISqlCallContext ctx, System.Int32 id)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd18();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)id ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = @"The name...";
            tP = cmd_parameters[2];
            tP.Value = @"2015-06-03";
            tP = cmd_parameters[3];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<SqlCallDemo.ComplexType.ComplexTypeSimple>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f19, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task<SqlCallDemo.ComplexType.ComplexTypeSimpleWithCtor> GetComplexTypeSimpleWithCtorAsync(CK.SqlServer.ISqlCallContext ctx, System.Int32 id)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd18();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)id ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = @"The name...";
            tP = cmd_parameters[2];
            tP.Value = @"2015-06-03";
            tP = cmd_parameters[3];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<SqlCallDemo.ComplexType.ComplexTypeSimpleWithCtor>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f20, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task<SqlCallDemo.ComplexType.ComplexTypeSimpleWithExtraProperty> GetComplexTypeSimpleWithExtraPropertyAsync(CK.SqlServer.ISqlCallContext ctx, System.Int32 id)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd18();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)id ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = @"The name...";
            tP = cmd_parameters[2];
            tP.Value = @"2015-06-03";
            tP = cmd_parameters[3];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<SqlCallDemo.ComplexType.ComplexTypeSimpleWithExtraProperty>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f21, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task<SqlCallDemo.ComplexType.ComplexTypeSimpleWithMissingProperty> GetComplexTypeSimpleWithMissingPropertyAsync(CK.SqlServer.ISqlCallContext ctx, System.Int32 id)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd18();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)id ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = @"The name...";
            tP = cmd_parameters[2];
            tP.Value = @"2015-06-03";
            tP = cmd_parameters[3];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<SqlCallDemo.ComplexType.ComplexTypeSimpleWithMissingProperty>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f22, default(System.Threading.CancellationToken));
        }
        public override SqlCallDemo.ComplexType.ComplexTypeStupidEmpty GetComplexTypeStupidEmpty(CK.SqlServer.ISqlCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd16();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            var oR = new SqlCallDemo.ComplexType.ComplexTypeStupidEmpty();
            return oR;
        }
        public override SqlCallDemo.ComplexType.ComplexTypeSimple GetComplexTypeSimple(CK.SqlServer.ISqlCallContext ctx, System.Int32 id)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd18();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)id ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = @"The name...";
            tP = cmd_parameters[2];
            tP.Value = @"2015-06-03";
            tP = cmd_parameters[3];
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            var oR = new SqlCallDemo.ComplexType.ComplexTypeSimple();
            object tempObj;
            tempObj = cmd_parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.Int32)tempObj;
            oR.Id = getR0; tempObj = cmd_parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            oR.Name = getR1; tempObj = cmd_parameters[2].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR2 = (System.DateTime)tempObj;
            oR.CreationDate = getR2; tempObj = cmd_parameters[3].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR3 = (System.Nullable<System.Int32>)tempObj;
            oR.NullableInt = getR3; return oR;
        }
        public override SqlCallDemo.ComplexType.ComplexTypeSimpleWithCtor GetComplexTypeSimpleWithCtor(CK.SqlServer.ISqlCallContext ctx, System.Int32 id)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd18();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)id ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = @"The name...";
            tP = cmd_parameters[2];
            tP.Value = @"2015-06-03";
            tP = cmd_parameters[3];
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.Int32)tempObj;
            tempObj = cmd_parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            tempObj = cmd_parameters[2].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR2 = (System.DateTime)tempObj;
            var oR = new SqlCallDemo.ComplexType.ComplexTypeSimpleWithCtor(getR0, getR1, getR2);
            return oR;
        }
        public override SqlCallDemo.ComplexType.ComplexTypeSimpleWithExtraProperty GetComplexTypeSimpleWithExtraProperty(CK.SqlServer.ISqlCallContext ctx, System.Int32 id)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd18();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)id ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = @"The name...";
            tP = cmd_parameters[2];
            tP.Value = @"2015-06-03";
            tP = cmd_parameters[3];
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            var oR = new SqlCallDemo.ComplexType.ComplexTypeSimpleWithExtraProperty();
            object tempObj;
            tempObj = cmd_parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.Int32)tempObj;
            oR.Id = getR0; tempObj = cmd_parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            oR.Name = getR1; tempObj = cmd_parameters[2].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR2 = (System.DateTime)tempObj;
            oR.CreationDate = getR2; return oR;
        }
        public override SqlCallDemo.ComplexType.ComplexTypeSimpleWithMissingProperty GetComplexTypeSimpleWithMissingProperty(CK.SqlServer.ISqlCallContext ctx, System.Int32 id)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd18();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)id ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = @"The name...";
            tP = cmd_parameters[2];
            tP.Value = @"2015-06-03";
            tP = cmd_parameters[3];
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            var oR = new SqlCallDemo.ComplexType.ComplexTypeSimpleWithMissingProperty();
            object tempObj;
            tempObj = cmd_parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            oR.Name = getR1; return oR;
        }
    }
    public class FunctionPackage2 : SqlCallDemo.FunctionPackage
    {
        public FunctionPackage2() { }
        public override System.String StringFunction(CK.SqlServer.SqlStandardCallContext ctx, System.Nullable<System.Int32> v)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd23();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP = cmd_parameters[1];
            tP.Value = (object)v ?? DBNull.Value;
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.String)tempObj;
            return getR0;
        }
        public override System.Threading.Tasks.Task<System.String> StringFunctionAsync(CK.SqlServer.SqlStandardCallContext ctx, System.Nullable<System.Int32> v)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd23();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP = cmd_parameters[1];
            tP.Value = (object)v ?? DBNull.Value;
            return ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQueryAsyncTyped<System.String>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f24, default(System.Threading.CancellationToken));
        }
        public override System.Byte ByteFunction(CK.SqlServer.SqlStandardCallContext ctx, System.Int32 v)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd25();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP = cmd_parameters[1];
            tP.Value = (object)v ?? DBNull.Value;
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.Byte)tempObj;
            return getR0;
        }
        public override System.Threading.Tasks.Task<System.Byte> ByteFunctionAsync(CK.SqlServer.SqlStandardCallContext ctx, System.Int32 v)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd25();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP = cmd_parameters[1];
            tP.Value = (object)v ?? DBNull.Value;
            return ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQueryAsyncTyped<System.Byte>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f26, default(System.Threading.CancellationToken));
        }
        public override System.Nullable<System.Byte> NullableByteFunction(CK.SqlServer.SqlStandardCallContext ctx, System.Int32 v)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd25();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP = cmd_parameters[1];
            tP.Value = (object)v ?? DBNull.Value;
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.Nullable<System.Byte>)tempObj;
            return getR0;
        }
        public override System.Threading.Tasks.Task<System.Nullable<System.Byte>> NullableByteFunctionAsync(CK.SqlServer.SqlStandardCallContext ctx, System.Int32 v)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd25();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP = cmd_parameters[1];
            tP.Value = (object)v ?? DBNull.Value;
            return ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQueryAsyncTyped<System.Nullable<System.Byte>>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f27, default(System.Threading.CancellationToken));
        }
        public override SqlCallDemo.FunctionPackage.Power ProcWithEnumIO(CK.SqlServer.ISqlCallContext ctx, SqlCallDemo.FunctionPackage.BPower bytePower, SqlCallDemo.FunctionPackage.Power power)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd28();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)bytePower ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)power ?? DBNull.Value;
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (SqlCallDemo.FunctionPackage.Power)tempObj;
            return getR1;
        }
        public override System.Threading.Tasks.Task<SqlCallDemo.FunctionPackage.Power> ProcWithEnumIOAsync(CK.SqlServer.ISqlCallContext ctx, SqlCallDemo.FunctionPackage.BPower bytePower, SqlCallDemo.FunctionPackage.Power power)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd28();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)bytePower ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)power ?? DBNull.Value;
            return ctx.Executor.ExecuteNonQueryAsyncTyped<SqlCallDemo.FunctionPackage.Power>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f29, default(System.Threading.CancellationToken));
        }
        public override System.Nullable<SqlCallDemo.FunctionPackage.Power> ProcWithNullableEnumIO(CK.SqlServer.ISqlCallContext ctx, System.Nullable<SqlCallDemo.FunctionPackage.BPower> bytePower, System.Nullable<SqlCallDemo.FunctionPackage.Power> power)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd28();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)bytePower ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)power ?? DBNull.Value;
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.Nullable<SqlCallDemo.FunctionPackage.Power>)tempObj;
            return getR1;
        }
        public override System.Threading.Tasks.Task<System.Nullable<SqlCallDemo.FunctionPackage.Power>> ProcWithNullableEnumIOAsync(CK.SqlServer.ISqlCallContext ctx, System.Nullable<SqlCallDemo.FunctionPackage.BPower> bytePower, System.Nullable<SqlCallDemo.FunctionPackage.Power> power)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd28();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)bytePower ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)power ?? DBNull.Value;
            return ctx.Executor.ExecuteNonQueryAsyncTyped<System.Nullable<SqlCallDemo.FunctionPackage.Power>>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f30, default(System.Threading.CancellationToken));
        }
    }
    public class GuidRefTestPackage3 : SqlCallDemo.GuidRefTestPackage
    {
        public GuidRefTestPackage3() { }
        public override void GuidRefTest(CK.SqlServer.SqlStandardCallContext ctx, System.Boolean replaceInAndOut, System.Guid inOnly, ref System.Guid inAndOut, out System.String textResult)
        {
            textResult = default(System.String);
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)replaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)inOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)inAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            inAndOut = (System.Guid)cmd_parameters[2].Value;
            textResult = (System.String)cmd_parameters[3].Value;
        }
        public override System.String GuidRefTestReturn(CK.SqlServer.SqlStandardCallContext ctx, System.Boolean replaceInAndOut, System.Guid inOnly, ref System.Guid inAndOut)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)replaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)inOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)inAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            inAndOut = (System.Guid)cmd_parameters[2].Value;
            object tempObj;
            tempObj = cmd_parameters[3].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR3 = (System.String)tempObj;
            return getR3;
        }
        public override System.String GuidRefTestReturnWithInterfaceContext(SqlCallDemo.INonStandardSqlCallContextSpecialized ctx, System.Boolean replaceInAndOut, System.Guid inOnly, ref System.Guid inAndOut)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)replaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)inOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)inAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            ctx.GetExecutor().ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            inAndOut = (System.Guid)cmd_parameters[2].Value;
            object tempObj;
            tempObj = cmd_parameters[3].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR3 = (System.String)tempObj;
            return getR3;
        }
        public override System.Guid GuidRefTestReturnInOut(CK.SqlServer.SqlStandardCallContext ctx, System.Boolean replaceInAndOut, System.Guid inOnly, System.Guid inAndOut, out System.String textResult)
        {
            textResult = default(System.String);
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)replaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)inOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)inAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            textResult = (System.String)cmd_parameters[3].Value;
            object tempObj;
            tempObj = cmd_parameters[2].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR2 = (System.Guid)tempObj;
            return getR2;
        }
        public override System.String GuidRefTestReturnWithInterfaceContext(SqlCallDemo.INonStandardSqlCallContextByProperty ctx, System.Boolean replaceInAndOut, System.Guid inOnly, ref System.Guid inAndOut)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)replaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)inOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)inAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            inAndOut = (System.Guid)cmd_parameters[2].Value;
            object tempObj;
            tempObj = cmd_parameters[3].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR3 = (System.String)tempObj;
            return getR3;
        }
        public override System.Data.SqlClient.SqlCommand CmdGuidRefTest(System.Boolean replaceInAndOut, System.Guid inOnly, ref System.Guid inAndOut, out System.String textResult)
        {
            textResult = default(System.String);
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)replaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)inOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)inAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            return cmd_loc;
        }
        public override System.Data.SqlClient.SqlCommand CmdGuidRefTest(System.Nullable<System.Boolean> replaceInAndOut, System.Nullable<System.Guid> inOnly, ref System.Nullable<System.Guid> inAndOut, out System.String textResult)
        {
            textResult = default(System.String);
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)replaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)inOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)inAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            return cmd_loc;
        }
        public override System.Data.SqlClient.SqlCommand CmdGuidRefTestWithoutTextResult(System.Boolean replaceInAndOut, System.Guid inOnly, ref System.Guid inAndOut)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)replaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)inOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)inAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            return cmd_loc;
        }
        public override void CmdGuidRefTest(ref System.Data.SqlClient.SqlCommand cmd, System.Boolean replaceInAndOut, System.Guid inOnly, ref System.Guid inAndOut, out System.String textResult)
        {
            textResult = default(System.String);
            SqlCommand cmd_loc;
            if (cmd == null) cmd = CK._g.CreatorForSqlCommand.Cmd31();
            cmd_loc = cmd;
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)replaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)inOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)inAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
        }
        public override System.Data.SqlClient.SqlCommand CmdGuidRefTest(SqlCallDemo.GuidRefTestPackage.GuidRefTestContext context, ref System.Guid inAndOut, out System.String textResult)
        {
            textResult = default(System.String);
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)context.ReplaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)context.InOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)inAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            return cmd_loc;
        }
        public override System.Data.SqlClient.SqlCommand CmdGuidRefTest(SqlCallDemo.GuidRefTestPackage.GuidRefTestInOutContext context, out System.String textResult)
        {
            textResult = default(System.String);
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)context.ReplaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)context.InOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)context.InAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            return cmd_loc;
        }
        public override System.Data.SqlClient.SqlCommand CmdGuidRefTest(SqlCallDemo.GuidRefTestPackage.GuidRefTestInOutContext context)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)context.ReplaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)context.InOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)context.InAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            return cmd_loc;
        }
        public override SqlCallDemo.GuidRefTestPackage.ReturnedWrapper CmdGuidRefTestReturnsWrapper(System.Boolean replaceInAndOut, System.Guid inOnly, ref System.Guid inAndOut)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)replaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)inOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)inAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            return new SqlCallDemo.GuidRefTestPackage.ReturnedWrapper(cmd_loc);
        }
        public override SqlCallDemo.GuidRefTestPackage.ReturnedWrapperWithParameters CmdGuidRefTestReturnsWrapperWithParameters(System.Boolean replaceInAndOut, System.String stringParameter, System.Guid inOnly, System.String anotherParameter, System.Int32 yetAnotherOne, ref System.Guid inAndOut)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)replaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)inOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)inAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            return new SqlCallDemo.GuidRefTestPackage.ReturnedWrapperWithParameters(cmd_loc, stringParameter, anotherParameter, yetAnotherOne);
        }
        public override SqlCallDemo.GuidRefTestPackage.ReturnedWrapperWithContext CmdGuidRefTestReturnsWrapperWithContext(SqlCallDemo.GuidRefTestPackage.GuidRefTestInOutContext context)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd31();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)context.ReplaceInAndOut ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)context.InOnly ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)context.InAndOut ?? DBNull.Value;
            tP = cmd_parameters[3];
            return new SqlCallDemo.GuidRefTestPackage.ReturnedWrapperWithContext(cmd_loc, context, (SqlCallDemo.IAmTheClassThatDefinesTheProcedure)this);
        }
    }
    public class OutputParameterPackage4 : SqlCallDemo.OutputParameterPackage
    {
        public OutputParameterPackage4() { }
        public override System.String OutputInputParameterWithDefault(CK.SqlServer.SqlStandardCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd32();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = @"The Sql Default.";
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.String)tempObj;
            return getR0;
        }
        public override System.String OutputInputParameterWithDefault(CK.SqlServer.SqlStandardCallContext ctx, System.String textResult)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd32();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)textResult ?? DBNull.Value;
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.String)tempObj;
            return getR0;
        }
        public override System.String OutputParameterWithDefault(CK.SqlServer.SqlStandardCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd33();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Direction = ParameterDirection.InputOutput;
            tP.Value = @"The Sql Default.";
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.String)tempObj;
            return getR0;
        }
        public override System.String OutputParameterWithDefault(CK.SqlServer.SqlStandardCallContext ctx, System.String textResult)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd33();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Direction = ParameterDirection.InputOutput;
            tP.Value = (object)textResult ?? DBNull.Value;
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[0].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR0 = (System.String)tempObj;
            return getR0;
        }
        public override System.Threading.Tasks.Task<System.String> OutputParameterWithDefaultAsync(CK.SqlServer.SqlStandardCallContext ctx, System.String textResult)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd33();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Direction = ParameterDirection.InputOutput;
            tP.Value = (object)textResult ?? DBNull.Value;
            return ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQueryAsyncTyped<System.String>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f24, default(System.Threading.CancellationToken));
        }
    }
    public class PocoPackage5 : SqlCallDemo.PocoPackage
    {
        public PocoPackage5() { }
        public override System.String Write(CK.SqlServer.ISqlCallContext ctx, SqlCallDemo.IThing thing)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd34();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)((CK._g.poco.Poco0)thing).Name ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP = cmd_parameters[2];
            tP.Value = (object)((CK._g.poco.Poco0)thing).Age ?? DBNull.Value;
            tP = cmd_parameters[3];
            tP.Value = (object)((CK._g.poco.Poco0)thing).Height ?? DBNull.Value;
            tP = cmd_parameters[4];
            tP.Value = (object)((CK._g.poco.Poco0)thing).Power ?? DBNull.Value;
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            return getR1;
        }
    }
    public class ProviderDemoPackage8 : SqlCallDemo.ProviderDemo.ProviderDemoPackage
    {
        public ProviderDemoPackage8() { }
        public override System.String ActorOnly(SqlCallDemo.IActorCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd35();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.ActorId ?? DBNull.Value;
            tP = cmd_parameters[1];
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            return getR1;
        }
        public override System.Threading.Tasks.Task<System.String> ActorOnlyAsync(SqlCallDemo.IActorCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd35();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.ActorId ?? DBNull.Value;
            tP = cmd_parameters[1];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<System.String>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f36, default(System.Threading.CancellationToken));
        }
        public override System.String CultureOnly(SqlCallDemo.ICultureCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd37();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.CultureId ?? DBNull.Value;
            tP = cmd_parameters[1];
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            return getR1;
        }
        public override System.Threading.Tasks.Task<System.String> CultureOnlyAsync(SqlCallDemo.ICultureCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd37();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.CultureId ?? DBNull.Value;
            tP = cmd_parameters[1];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<System.String>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f36, default(System.Threading.CancellationToken));
        }
        public override System.String TenantOnly(SqlCallDemo.ITenantCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd38();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.TenantId ?? DBNull.Value;
            tP = cmd_parameters[1];
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            return getR1;
        }
        public override System.Threading.Tasks.Task<System.String> TenantOnlyAsync(SqlCallDemo.ITenantCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd38();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.TenantId ?? DBNull.Value;
            tP = cmd_parameters[1];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<System.String>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f36, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task<System.String> ActorCulture(SqlCallDemo.IActorCultureCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd39();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.ActorId ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)ctx.CultureId ?? DBNull.Value;
            tP = cmd_parameters[2];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<System.String>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f40, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task<System.String> ActorCultureAsync(SqlCallDemo.IActorCultureCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd39();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.ActorId ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)ctx.CultureId ?? DBNull.Value;
            tP = cmd_parameters[2];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<System.String>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f40, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task<System.String> CultureTenant(SqlCallDemo.ICultureTenantCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd41();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.CultureId ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)ctx.TenantId ?? DBNull.Value;
            tP = cmd_parameters[2];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<System.String>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f40, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task<System.String> CultureTenantAsync(SqlCallDemo.ICultureTenantCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd41();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.CultureId ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)ctx.TenantId ?? DBNull.Value;
            tP = cmd_parameters[2];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<System.String>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f40, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task<System.String> AllContexts(SqlCallDemo.IAllCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd42();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.ActorId ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)ctx.CultureId ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)ctx.TenantId ?? DBNull.Value;
            tP = cmd_parameters[3];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<System.String>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f43, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task<System.String> AllContextsAsync(SqlCallDemo.IAllCallContext ctx)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd42();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.ActorId ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)ctx.CultureId ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)ctx.TenantId ?? DBNull.Value;
            tP = cmd_parameters[3];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<System.String>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f43, default(System.Threading.CancellationToken));
        }
    }
    public class PurelyInputLogPackage6 : SqlCallDemo.PurelyInputLogPackage
    {
        public PurelyInputLogPackage6() { }
        public override System.Threading.Tasks.Task SimpleLog(CK.SqlServer.SqlStandardCallContext ctx, System.String logText)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd44();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)logText ?? DBNull.Value;
            return ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQueryAsync(Database.ConnectionString, cmd_loc, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task Log(CK.SqlServer.SqlStandardCallContext ctx, System.Nullable<System.Boolean> oneMore, System.String logText)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd45();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)oneMore ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)logText ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = 0;
            return ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQueryAsync(Database.ConnectionString, cmd_loc, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task LogWithDefaultBitValue(CK.SqlServer.SqlStandardCallContext ctx, System.String logText)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd45();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = 1;
            tP = cmd_parameters[1];
            tP.Value = (object)logText ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = 0;
            return ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQueryAsync(Database.ConnectionString, cmd_loc, default(System.Threading.CancellationToken));
        }
        public override System.Threading.Tasks.Task LogWait(CK.SqlServer.SqlStandardCallContext ctx, System.String logText, System.Int32 waitTimeMS, System.Threading.CancellationToken cancellationToken)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd45();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = 1;
            tP = cmd_parameters[1];
            tP.Value = (object)logText ?? DBNull.Value;
            tP = cmd_parameters[2];
            tP.Value = (object)waitTimeMS ?? DBNull.Value;
            return ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQueryAsync(Database.ConnectionString, cmd_loc, cancellationToken);
        }
    }
    public class ReturnPackage7 : SqlCallDemo.ReturnPackage
    {
        public ReturnPackage7() { }
        public override System.String StringReturn(CK.SqlServer.SqlStandardCallContext ctx, System.Int32 v)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd46();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)v ?? DBNull.Value;
            tP = cmd_parameters[1];
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.String)tempObj;
            return getR1;
        }
        public override System.Threading.Tasks.Task<System.String> StringReturnAsync(CK.SqlServer.SqlStandardCallContext ctx, System.Int32 v)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd46();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)v ?? DBNull.Value;
            tP = cmd_parameters[1];
            return ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQueryAsyncTyped<System.String>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f36, default(System.Threading.CancellationToken));
        }
        public override System.Int32 IntReturn(CK.SqlServer.SqlStandardCallContext ctx, System.Nullable<System.Int32> v)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd47();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)v ?? DBNull.Value;
            tP = cmd_parameters[1];
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[1].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR1 = (System.Int32)tempObj;
            return getR1;
        }
        public override System.Threading.Tasks.Task<System.Int32> IntReturnAsync(CK.SqlServer.SqlStandardCallContext ctx, System.Nullable<System.Int32> v)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd47();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)v ?? DBNull.Value;
            tP = cmd_parameters[1];
            return ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQueryAsyncTyped<System.Int32>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f48, default(System.Threading.CancellationToken));
        }
        public override System.Int32 IntReturnWithActor(SqlCallDemo.IActorCallContextIsExecutor ctx, System.String def)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd49();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.ActorId ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)def ?? DBNull.Value;
            tP = cmd_parameters[2];
            ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[2].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR2 = (System.Int32)tempObj;
            return getR2;
        }
        public override System.Threading.Tasks.Task<System.Int32> IntReturnWithActorAsync(SqlCallDemo.IActorCallContextIsExecutor ctx, System.String def)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd49();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.ActorId ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)def ?? DBNull.Value;
            tP = cmd_parameters[2];
            return ((CK.SqlServer.ISqlCommandExecutor)ctx).ExecuteNonQueryAsyncTyped<System.Int32>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f50, default(System.Threading.CancellationToken));
        }
        public override System.Int32 IntReturnWithActor(SqlCallDemo.IActorCallContext ctx, System.String def)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd49();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.ActorId ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)def ?? DBNull.Value;
            tP = cmd_parameters[2];
            ctx.Executor.ExecuteNonQuery(Database.ConnectionString, cmd_loc);
            object tempObj;
            tempObj = cmd_parameters[2].Value;
            if (tempObj == DBNull.Value) tempObj = null;
            var getR2 = (System.Int32)tempObj;
            return getR2;
        }
        public override System.Threading.Tasks.Task<System.Int32> IntReturnWithActorAsync(SqlCallDemo.IActorCallContext ctx, System.String def)
        {
            SqlCommand cmd_loc;
            cmd_loc = CK._g.CreatorForSqlCommand.Cmd49();
            SqlParameterCollection cmd_parameters = cmd_loc.Parameters;
            SqlParameter tP;
            tP = cmd_parameters[0];
            tP.Value = (object)ctx.ActorId ?? DBNull.Value;
            tP = cmd_parameters[1];
            tP.Value = (object)def ?? DBNull.Value;
            tP = cmd_parameters[2];
            return ctx.Executor.ExecuteNonQueryAsyncTyped<System.Int32>(Database.ConnectionString, cmd_loc, CK._g._build_func_._f50, default(System.Threading.CancellationToken));
        }
    }
}