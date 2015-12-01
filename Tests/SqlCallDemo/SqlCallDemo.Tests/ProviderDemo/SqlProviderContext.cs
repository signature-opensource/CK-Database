using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer;

namespace SqlCallDemo.Tests.ProviderDemo
{
    /// <summary>
    /// The context provider captures any information required to create new contexts whenever needed.
    /// It is typically stored in the request context (owin context, http current items, etc.).
    /// </summary>
    class SqlProviderContext : SqlStandardCallContextProvider<SqlFinalApplicationContext, SqlFinalApplicationContext>
    {
        int _actorId;
        int _xlcid;
        int _zoneId;

        public SqlProviderContext( int actorId, int xlcid, int zoneId )
        {
            _actorId = actorId;
            _xlcid = xlcid;
            _zoneId = zoneId;
        }

        protected override SqlFinalApplicationContext CreateContext()
        {
            return new SqlFinalApplicationContext( _actorId, _xlcid, _zoneId );
        }
    }
}
