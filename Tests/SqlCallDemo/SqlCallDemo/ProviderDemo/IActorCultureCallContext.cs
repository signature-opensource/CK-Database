using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace SqlCallDemo
{
    /// <summary>
    /// This context composes two other contexts.
    /// </summary>
    public interface IActorCultureCallContext : IActorCallContext, ICultureCallContext
    {
    }

    /// <summary>
    /// Its disposable type is composed of the non-disposable one and all the disposable version of its components.
    /// It is actually the same pattern as when only one context is concerned (see <see cref="IDisposableActorCallContext"/>).
    /// </summary>
    public interface IDisposableActorCultureCallContext : IActorCultureCallContext, IDisposableActorCallContext, IDisposableCultureCallContext
    {
    }
}
