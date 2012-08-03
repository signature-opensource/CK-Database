using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// This interface marker states that a class or an interface instance
    /// must be unique in a context. 
    /// </summary>
    /// <remarks>
    /// The notion of "context" is not defined at this level, this interface 
    /// only declares the type as beeing a "pseudo singleton" for the global, default, scope. 
    /// </remarks>
    public interface IAmbiantContract
    {
    }

    /// <summary>
    /// This marker interface makes use of generic to scope an <see cref="IAmbiantContract"/>
    /// into a context identified by the type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type that identifies the scope of this ambiant contract.</typeparam>
    public interface IAmbiantContract<T> : IAmbiantContract
    {
    }


}
