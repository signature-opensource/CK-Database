using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    /// <summary>
    /// Simple context provider of mere <see cref="IDisposableSqlCallContext"/>.
    /// Can be used whenever a basic <see cref="ISqlCallContext"/> is enough (when there is no need for application specific properties).
    /// </summary>
    public sealed class SqlStandardCallContextProvider : SqlStandardCallContextProvider<IDisposableSqlCallContext, SqlStandardCallContextRefCounter>
    {
        /// <summary>
        /// Simply returns a new <see cref="SqlStandardCallContextRefCounter"/>.
        /// </summary>
        /// <returns>The new context.</returns>
        protected override SqlStandardCallContextRefCounter CreateContext()
        {
            return new SqlStandardCallContextRefCounter();
        }
    }
}
