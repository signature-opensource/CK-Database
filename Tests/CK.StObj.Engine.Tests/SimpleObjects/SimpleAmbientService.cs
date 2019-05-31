using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.StObj.Engine.Tests.SimpleObjects
{
    public interface IExternal { }

    /// <summary>
    /// This is a simple ambient service: it can be Scoped or Singleton
    /// but since it is used by ObjectA, it is necessarily a singleton.
    /// </summary>
    public class SimpleAmbientService : IAmbientService
    {
        /// <summary>
        /// External is not an Ambient service.
        /// Since we do not know anything about it we oconsider it as a Scoped service.
        /// Considering External as a Scoped service makes this one necessarily a Scoped service
        /// and this introduces a failure since this is used by an Ambient Object.
        /// The only way to make this work is to explicitly declares IExternal as a scoped service.
        /// </summary>
        /// <param name="external">The external service.</param>
        public SimpleAmbientService( IExternal external )
        {
        }
    }
}
