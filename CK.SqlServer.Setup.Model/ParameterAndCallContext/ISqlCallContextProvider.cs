using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    /// <summary>
    /// Simple generic interface that gives access to a typed <see cref="IDisposableSqlCallContext"/>.
    /// The <see cref="SqlStandardCallContextProvider{T,TImpl}"/> provides a standard implementation for it. 
    /// </summary>
    /// <typeparam name="T">A <see cref="IDisposableSqlCallContext"/> interface.</typeparam>
    public interface ISqlCallContextProvider<out T> where T : IDisposableSqlCallContext
    {
        /// <summary>
        /// Acquires a typed <see cref="IDisposableSqlCallContext"/> that must be disposed once done with it.
        /// </summary>
        /// <returns>A disposable call context.</returns>
        T Acquire();
    }

}
