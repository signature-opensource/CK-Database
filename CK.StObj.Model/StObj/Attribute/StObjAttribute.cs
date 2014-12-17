#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObj\Attribute\StObjAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Default implementation of <see cref="IStObjAttribute"/> that offers a static <see cref="GetStObjAttributeForExactType"/> that knows how to merge
    /// multiple attributes information.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class StObjAttribute : Attribute, IStObjAttribute
    {
        /// <summary>
        /// Gets or sets the container of the object.
        /// This property is inherited from base classes that are not Ambient Contracts.
        /// </summary>
        public Type Container { get; set; }

        /// <summary>
        /// Gets or sets the kind of object (simple item, group or container).
        /// This property is inherited from base classes that are not Ambient Contracts.
        /// </summary>
        public DependentItemKindSpec ItemKind { get; set; }

        /// <summary>
        /// Gets or sets how Ambient Properties that reference the object must be tracked.
        /// This property is inherited from base classes that are not Ambient Contracts.
        /// </summary>
        public TrackAmbientPropertiesMode TrackAmbientProperties { get; set; }

        /// <summary>
        /// Gets or sets an array of direct dependencies.
        /// This property is not inherited, it applies only to the decorated type.
        /// </summary>
        public Type[] Requires { get; set; }

        /// <summary>
        /// Gets or sets an array of types that depend on the object.
        /// This property is not inherited, it applies only to the decorated type.
        /// </summary>
        public Type[] RequiredBy { get; set; }

        /// <summary>
        /// Gets or sets an array of types that must be Children of this item.
        /// <see cref="ItemKind"/> must be <see cref="DependentItemKind.Group"/> or <see cref="DependentItemKind.Container"/>.
        /// This property is not inherited, it applies only to the decorated type.
        /// </summary>
        public Type[] Children { get; set; }

        /// <summary>
        /// Gets or sets an array of types that must be considered as groups for this item.
        /// This property is not inherited, it applies only to the decorated type.
        /// </summary>
        public Type[] Groups { get; set; }

    }
}
