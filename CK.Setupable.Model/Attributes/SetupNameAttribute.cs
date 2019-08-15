#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\SetupNameAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.Setup
{
    /// <summary>
    /// Basic attribute that defines the Setup name of an object.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class SetupNameAttribute : Attribute, IAttributeSetupName
    {
        readonly string _name;

        /// <summary>
        /// Initializes a new <see cref="SetupNameAttribute"/> with a name.
        /// </summary>
        /// <param name="fullName">Name of the object.</param>
        public SetupNameAttribute( string fullName )
        {
            _name = fullName;
        }

        /// <summary>
        /// Gets the full name of the setup object.
        /// </summary>
        public string FullName
        {
            get { return _name; }
        }

    }
}
