using System;
using System.Collections.Generic;
using System.Text;

namespace SqlActorPackage
{
    /// <summary>
    /// This interface is registered via StObjInitialize method.
    /// </summary>
    public interface IAnyService
    {
        string CallService();
    }
}
