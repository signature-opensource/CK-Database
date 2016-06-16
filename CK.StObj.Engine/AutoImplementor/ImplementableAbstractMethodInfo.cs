#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\AutoImplementor\ImplementableAbstractMethodInfo.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Associates an <see cref="IAutoImplementorMethod"/> to use and keeps the last one used 
    /// for a <see cref="Method"/>.
    /// </summary>
    public class ImplementableAbstractMethodInfo
    {
        IAutoImplementorMethod _toUse;
        internal ImplementableTypeInfo _type;
        internal IAutoImplementorMethod _last;

        internal ImplementableAbstractMethodInfo( MethodInfo m, IAutoImplementorMethod impl )
        {
            Method = m;
            _toUse = impl;
        }

        /// <summary>
        /// Abstract method that has to be automatically implemented.
        /// </summary>
        public readonly MethodInfo Method;

        /// <summary>
        /// Gets or sets the current <see cref="IAutoImplementorMethod"/> to use.
        /// When null or same as <see cref="LastImplementor"/>, method is considered as having already been implemented.
        /// </summary>
        public IAutoImplementorMethod ImplementorToUse
        {
            get { return _toUse; }
            set
            {
                if( _toUse != value && _toUse != _last )
                {
                    _toUse = value;
                    _type.ImplementorChanged( this );
                }
            }
        }

        /// <summary>
        /// Gets the last <see cref="IAutoImplementorMethod"/> that has been used to 
        /// generate the <see cref="ImplementableTypeInfo.LastGeneratedType"/>.
        /// </summary>
        public IAutoImplementorMethod LastImplementor => _last; 

        /// <summary>
        /// Gets wether this property is waiting for an implementation: its <see cref="ImplementorToUse"/> is not null 
        /// and differs from <see cref="LastImplementor"/>.
        /// </summary>
        public bool ExpectImplementation => ImplementorToUse != null && ImplementorToUse != _last; 

    }

}
