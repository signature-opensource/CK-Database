using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Poco factory.
    /// These interface are automaticaaly implemented.
    /// </summary>
    public interface IPocoFactory<T> : IPoco, IAmbientContract
    {
        /// <summary>
        /// Creates a new Poco instance.
        /// </summary>
        /// <returns></returns>
        T Create();
    }
}
