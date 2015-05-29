using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer;

namespace SqlCallDemo.Tests.ProviderDemo
{
    /// <summary>
    /// This context provider is cleaner: it exposes a IAllContext instead of the actual context object.
    /// </summary>
    class SqlProviderContextCleaner : SqlStandardCallContextProvider<IDisposableAllCallContext, SqlFinalApplicationContext>
    {
        int _actorId;
        int _xlcid;
        int _zoneId;

        public SqlProviderContextCleaner( int actorId, int xlcid, int zoneId )
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
