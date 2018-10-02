#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\AmbientContract\IAmbientContract.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// This interface marker states that a class or an interface instance
    /// is a gateway to an external resource.
    /// Such gateways are necessarily unique instance in a "context" that is defined
    /// by the <see cref="IStObjMap"/>.
    /// To be considered an ambient contract, a Type must be marked with this exact interface
    /// (there is no "duck typing" by name like for <see cref="ISingletonAmbientService"/>
    /// and <see cref="IScopedAmbientService"/>.
    /// </summary>
    /// <remarks>
    /// If the lifetimes of <see cref="IAmbientContract"/> and <see cref="ISingletonAmbientService"/>
    /// instances are the same, their roles are different as well as the way they are handled.
    /// Ambient contracts should not use any constructor injection and support StObjConstruct/StObjInitialize
    /// private methods that isolate dependencies between a base class and its specializations whereas
    /// singleton services relies on "normal" constructor injection.
    /// Ambient contracts must be used to model actual singletons in a system, typically external resources
    /// with wich the system interacts such as a data repository or an external service.
    /// Singleton services are more classical services that happens to be able to be shared by different
    /// activities because of their thread safety and the fact that they depend only on other singleton
    /// services or ambient contracts.
    /// </remarks>
    public interface IAmbientContract
    {
    }

}
