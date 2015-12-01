#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\Impl\MutableAmbientProperty.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Reflection;

namespace CK.Setup
{
    /// <summary>
    /// Describes an Ambient property.
    /// </summary>
    internal class MutableAmbientProperty : MutableReferenceWithValue, IStObjAmbientProperty, IStObjFinalAmbientProperty
    {
        readonly AmbientPropertyInfo _info;
        int _maxSpecializationDepthSet;
        internal bool UseValue;

        internal MutableAmbientProperty( MutableItem owner, AmbientPropertyInfo info )
            : base( owner, StObjMutableReferenceKind.AmbientProperty )
        {
            _info = info;
            Type = _info.PropertyType;
            IsOptional = _info.IsOptional;
        }

        /// <summary>
        /// Initializes a new marker object: the ambient property has not been found. 
        /// </summary>
        internal MutableAmbientProperty( MutableItem owner, string unexistingPropertyName )
            : base( owner, StObjMutableReferenceKind.AmbientProperty )
        {
            _info = null;
            Type = typeof(object);
            IsOptional = false;
            _maxSpecializationDepthSet = Int32.MaxValue;
        }

        IStObjMutableItem IStObjAmbientProperty.Owner { get { return Owner; } }

        public override string Name { get { return _info.Name; } }

        internal override string KindName { get { return "AmbientProperty"; } }

        internal override Type UnderlyingType { get { return _info.PropertyType; } }

        public override string ToString()
        {
            return String.Format( "Ambient Property '{0}' of '{1}'", Name, Owner.ToString() );
        }

        internal AmbientPropertyInfo AmbientPropertyInfo { get { return _info; } }

        internal int MaxSpecializationDepthSet { get { return _maxSpecializationDepthSet; } }

        /// <summary>
        /// Sets the final value. Public in order to implement IStObjFinalAmbientProperty.SetValue.
        /// </summary>
        public void SetValue( object value )
        {
            _maxSpecializationDepthSet = Int32.MaxValue;
            Value = value;
        }

        internal bool IsFinalValue
        {
            get { return _maxSpecializationDepthSet == Int32.MaxValue; }
        }

        internal bool SetValue( int setterSpecializationDepth, IActivityMonitor monitor, object value )
        {
            Debug.Assert( _maxSpecializationDepthSet != Int32.MaxValue );
            if( setterSpecializationDepth < _maxSpecializationDepthSet )
            {
                monitor.Error().Send( "'{0}' has already been set or configured through a more specialized object.", ToString() );
                return false;
            }
            _maxSpecializationDepthSet = setterSpecializationDepth;
            Value = value;
            UseValue = true;
            return true;
        }

        internal bool SetConfiguration( int setterSpecializationDepth, IActivityMonitor monitor, string context, Type type, StObjRequirementBehavior behavior )
        {
            Debug.Assert( _maxSpecializationDepthSet != Int32.MaxValue );
            if( setterSpecializationDepth < _maxSpecializationDepthSet )
            {
                monitor.Error().Send( "'{0}' has already been set or configured through a more specialized object.", ToString() );
                return false;
            }
            _maxSpecializationDepthSet = setterSpecializationDepth;
            Value = Type.Missing;
            Context = context;
            Type = type;
            StObjRequirementBehavior = behavior;
            UseValue = false;
            return true;
        }

    }
}
