#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Package\IMutableSetupItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion


namespace CK.Setup
{
    /// <summary>
    /// A <see cref="ISetupItem"/> that exposes its associate model object .
    /// This interface should be implemented by concrete classes when they are 
    /// actually in charge of an associated object.
    /// </summary>
    public interface ISetupObjectItem : ISetupItem
    { 
        /// <summary>
        /// Gets the associated model object.
        /// </summary>
        object ActualObject { get; }               
    }
}
