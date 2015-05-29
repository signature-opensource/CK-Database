using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer.Setup;

namespace CK.SqlServer
{

    /// <summary>
    /// Base object to implement <see cref="ISqlCallContextProvider{T}"/> that works with <see cref="SqlStandardCallContextRefCounter"/>.
    /// The abstract <see cref="CreateContext"/> method must create the actual context object.
    /// </summary>
    /// <typeparam name="T">Exposed type. Typically an interface that specializes <see cref="IDisposableSqlCallContext"/> but can be the implementation type itself.</typeparam>
    /// <typeparam name="TImpl">Implementation type that inherits from <see cref="SqlStandardCallContextRefCounter"/> and is (implements) a <typeparamref name="T"/>.</typeparam>
    public abstract class SqlStandardCallContextProvider<T, TImpl> : ISqlCallContextProvider<T>
        where T : IDisposableSqlCallContext
        where TImpl : SqlStandardCallContextRefCounter, T
    {
        TImpl _ctx;

        /// <summary>
        /// Acquires a typed <see cref="IDisposableSqlCallContext"/> that must be disposed once done with it.
        /// </summary>
        /// <returns>A disposable call context.</returns>
        public T Acquire()
        {
            if( _ctx == null || _ctx.IsDisposed ) _ctx = CreateContext();
            else _ctx.AddRef();
            return _ctx;
        }

        /// <summary>
        /// Must create a new <typeparamref name="TImpl"/>.
        /// This is automaticaly called whenever a new context must be created.
        /// </summary>
        /// <returns>A new context.</returns>
        protected abstract TImpl CreateContext();
    }
}
