using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    internal class StObjSetupDataBase
    {
        readonly IStObjSetupData _parent;

        Type _driverType;
        string _driverTypeName;
        string _containerFullName;
        DependentItemList _requires;
        DependentItemList _requiredBy;
        bool _hasModel;
        bool _noContent;

        internal StObjSetupDataBase( IActivityLogger logger, Type t, StObjSetupDataBase parent = null )
        {
            _parent = parent as IStObjSetupData;
            bool isInRoot = _parent == null;

            _requires = RequiresAttribute.GetRequirements( logger, t, typeof( RequiresAttribute ) );
            _requiredBy = RequiresAttribute.GetRequirements( logger, t, typeof( RequiredByAttribute ) );
            SetupAttribute setupAttr = SetupAttribute.GetSetupAttribute( t );
            if( setupAttr != null )
            {
                _containerFullName = setupAttr.ContainerFullName;
                _driverType = setupAttr.DriverType;
                _driverTypeName = setupAttr.DriverTypeName;
                
                _hasModel = setupAttr.HasModel;
                
                if( setupAttr.NoContentDefined ) _noContent = setupAttr.NoContent;
                else _noContent = parent != null ? parent.NoContent : false;
            }
            else
            {
                // No Package attribute...
                if( parent != null )
                {
                    // _hasModel remains false (this is not because the base class has an associated Model that
                    // a specialization has one), but NoContent is stronger: if a base class states that it has no content,
                    // its specialization, by default, also reject content.
                    _noContent = parent.NoContent;
                    
                    // We accept to inherit from parent HasModel, only if 
                    // we are the root ambiant contract (consider attributes on above classes to 
                    // be kind of "definer").
                    if( isInRoot )
                    {
                        _hasModel = parent.HasModel;
                        // There is currently no other attribute that works this way
                        // (Requirements are handled below, full name and versions are fundamentally by StObj).
                    }
                }
            }
            // Container full name, driver type & name inherit by default.
            if( parent != null )
            {
                if( _driverType == null ) _driverType = parent.DriverType;
                if( _driverTypeName == null ) _driverTypeName = parent.DriverTypeName;
                if( _containerFullName == null ) _containerFullName = parent.ContainerFullName;
            }

            // If we are the root of the ambiant contract, we consider that base classes
            // preinitialize our value.
            if( isInRoot && parent != null )
            {
                Requires.AddRange( parent.Requires );
                RequiredBy.AddRange( parent.RequiredBy );
            }
        }

        public IStObjSetupData Parent
        {
            get { return _parent; }
        }
        
        public string ContainerFullName
        {
            get { return _containerFullName; }
            set { _containerFullName = value; }
        }

        public IDependentItemList Requires
        {
            get { return _requires; }
        }

        public IDependentItemList RequiredBy
        {
            get { return _requiredBy; }
        }

        public bool NoContent
        {
            get { return _noContent; }
            set { _noContent = value; }
        }

        public Type DriverType
        {
            get { return _driverType; }
            set { _driverType = value; }
        }

        public string DriverTypeName
        {
            get { return _driverTypeName; }
            set { _driverTypeName = value; }
        }

        public bool HasModel
        {
            get { return _hasModel; }
            set { _hasModel = value; }
        }


        internal static StObjSetupDataBase CreateRootData( IActivityLogger logger, Type t )
        {
            if( t == typeof( object ) ) return null;
            StObjSetupDataBase b = CreateRootData( logger, t.BaseType );
            return new StObjSetupDataBase( logger, t, b ); 
        }
    }
}
