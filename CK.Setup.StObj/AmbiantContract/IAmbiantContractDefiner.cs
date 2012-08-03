using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// This interface marker states that a class or an interface instance
    /// is the base of an <see cref="IAmbiantContract"/>. 
    /// </summary>
    /// <remarks>
    /// The notion of "context" is not defined at this level, this interface 
    /// only declares the type as beeing the base (class or interface) of a 
    /// "pseudo singleton" for the global, default, scope. 
    /// </remarks>
    public interface IAmbiantContractDefiner
    {
    }

    /// <summary>
    /// This marker interface makes use of generic to scope an <see cref="IAmbiantContractDefiner"/>
    /// into a context identified by the type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type that identifies the scope of ambiant contracts that inherit/extend this interface.</typeparam>
    public interface IAmbiantContractDefiner<T> : IAmbiantContractDefiner
    {
    }


}
