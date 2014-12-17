#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\AutoImplementor\ImplementableAbstractPropertyInfo.cs) is part of CK-Database. 
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
    /// Associates an <see cref="IAutoImplementorProperty"/> to use and keeps the last one used 
    /// for a <see cref="Property"/>.
    /// </summary>
    public class ImplementableAbstractPropertyInfo
    {
        IAutoImplementorProperty _toUse;
        internal ImplementableTypeInfo _type;
        internal IAutoImplementorProperty _last;

        internal ImplementableAbstractPropertyInfo( PropertyInfo p, IAutoImplementorProperty impl )
        {
            Property = p;
            _toUse = impl;
        }

        /// <summary>
        /// Abstract property that has to be automatically implemented.
        /// </summary>
        public readonly PropertyInfo Property;

        /// <summary>
        /// Gets or sets the current <see cref="IAutoImplementorProperty"/> to use.
        /// When null or same as <see cref="LastImplementor"/>, property is considered as having already been implemented.
        /// </summary>
        public IAutoImplementorProperty ImplementorToUse
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
        /// Gets the last <see cref="IAutoImplementorProperty"/> that has been used to 
        /// generate the <see cref="ImplementableTypeInfo.LastGeneratedType"/>.
        /// </summary>
        public IAutoImplementorProperty LastImplementor { get { return _last; } }

        /// <summary>
        /// Gets wether this method is waiting for an implementation: its <see cref="ImplementorToUse"/> is not null 
        /// and differs from <see cref="LastImplementor"/>.
        /// </summary>
        public bool ExpectImplementation
        {
            get { return ImplementorToUse != null && ImplementorToUse != _last; }
        }
    }

}
