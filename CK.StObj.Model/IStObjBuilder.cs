using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Constructor that accepts an <see cref="IActivityLogger"/> and an instance of 
    /// this <see cref="IStObjEngineConfiguration"/>.
    /// </summary>
    public interface IStObjBuilder
    {
        void Run();
    }
}
