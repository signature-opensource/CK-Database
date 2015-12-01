using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer.Setup;

namespace CK.SqlServer
{
    /// <summary>
    /// Reusable base class to easily implement specialized <see cref="IDisposableSqlCallContext"/>.
    /// Such specialized context implementations may implement a specialized IDisposableSqlCallContext
    /// that exposes properties that can be used to set procedure parameters or any other application defined information
    /// or may be exposed as-is.
    /// </summary>
    public class SqlStandardCallContextRefCounter : SqlStandardCallContext
    {
        // Starts at 0: this context is disposed when negative.
        int _externalRefCount;

        /// <summary>
        /// Gets whether this context has been disposed.
        /// </summary>
        internal protected bool IsDisposed
        {
            get { return _externalRefCount < 0; }
        }

        /// <summary>
        /// Increments the internal reference count.
        /// </summary>
        internal protected void AddRef()
        {
            CheckDisposed();
            ++_externalRefCount;
        }

        /// <summary>
        /// Helper methods that throws a <see cref="ObjectDisposedException"/> whenever this <see cref="IsDisposed"/> is true.
        /// </summary>
        protected void CheckDisposed()
        {
            if( _externalRefCount < 0 ) throw new ObjectDisposedException( "SqlStandardCallContextRefCounter" );
        }

        /// <summary>
        /// Overridden to decrement the reference counter and actually dispose this context
        /// if it no more used.
        /// </summary>
        public override void Dispose()
        {
            if( --_externalRefCount < 0 ) base.Dispose();
        }
    }
}
