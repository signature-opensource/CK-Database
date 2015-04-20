using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    public abstract class SetupObjectItemMemberAttributeImplBase : IStObjSetupDynamicInitializer, IAttributeAmbientContextBoundInitializer, ISetupObjectItemProvider
    {
        readonly AmbientContextBoundDelegationAttribute _attribute;
        ICKCustomAttributeTypeMultiProvider _owner;
        MemberInfo _member;
        SetupObjectItemAttributeImplBase.BestInitializer _theBest;
        
        /// <summary>
        /// Name of the object.
        /// </summary>
        protected readonly string ObjectName;

        /// <summary>
        /// Initializes a new <see cref="SetupObjectItemMemberAttributeImplBase"/> bound to a <see cref="SetupObjectItemMemberAttributeBase"/>.
        /// </summary>
        /// <param name="a">The attribute.</param>
        protected SetupObjectItemMemberAttributeImplBase( SetupObjectItemMemberAttributeBase a )
        {
            _attribute = a;
            ObjectName = a.ObjectName;
        }

        /// <summary>
        /// Initializes a new <see cref="SetupObjectItemMemberAttributeImplBase"/> bound to a name.
        /// </summary>
        /// <param name="a">Attribute object.</param>
        /// <param name="objectName">The object name.</param>
        protected SetupObjectItemMemberAttributeImplBase( AmbientContextBoundDelegationAttribute a, string objectName )
        {
            _attribute = a;
            ObjectName = objectName;
        }

        /// <summary>
        /// Gets the original attribute.
        /// </summary>
        protected AmbientContextBoundDelegationAttribute Attribute
        {
            get { return _attribute; }
        }

        /// <summary>
        /// Gets the owner (type and provider of its other attributes).
        /// </summary>
        protected ICKCustomAttributeTypeMultiProvider Owner
        {
            get { return _owner; }
        }

        /// <summary>
        /// Gets the member to which the attribute applies.
        /// </summary>
        protected MemberInfo Member
        {
            get { return _member; }
        }

        void IAttributeAmbientContextBoundInitializer.Initialize( ICKCustomAttributeTypeMultiProvider owner, MemberInfo m )
        {
            _owner = owner;
            _member = m;
        }

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            IContextLocNaming name = BuildFullName( item, stObj, ObjectName );
            if( name == null )
            {
                state.Monitor.Error().Send( "Invalid object name '{0}' in {3} attribute of '{1}' for '{2}'.", ObjectName, Member.Name, item.FullName, Attribute.GetShortTypeName() );
                return;
            }
            _theBest = SetupObjectItemAttributeImplBase.AssumeBestInitializer( state, name, this );
            if( _theBest.FirstInitializer == this )
            {
                _theBest.FirstItem = CreateSetupObjectItem( state.Monitor, item, stObj, name );
                _theBest.LastPackagesSeen = item;
            }
            else state.PushAction( DynamicItemInitializeAfterFollowing );
        }

        void DynamicItemInitializeAfterFollowing( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            // If we are the best, our resource wins.
            if( _theBest.Initializer == this )
            {
                Debug.Assert( _theBest.FirstInitializer != this, "We did not push any action for the first." );
                Debug.Assert( _theBest.Item == null, "We are the only winner." );
                // When multiples methods exist bound to the same object, this avoids 
                // to load the same resource multiple times: only the first occurence per package is considered.
                if( _theBest.LastPackagesSeen != item )
                {
                    _theBest.Item = CreateSetupObjectItem( state.Monitor, item, stObj, _theBest.Name ); ;
                    _theBest.FirstItem.ReplacedBy = _theBest.Item;
                    _theBest.LastPackagesSeen = item;
                }
            }
        }

        /// <summary>
        /// Gets the best <see cref="SetupObjectItem"/> found.
        /// </summary>
        public SetupObjectItem SetupObjectItem
        {
            get { return _theBest != null ? _theBest.Item ?? _theBest.FirstItem : null; }
        }

        /// <summary>
        /// Must build the <see cref="IContextLocNaming"/> name of the future <see cref="SetupObjectItem"/> with the help of the owner object and the name in the attribute.
        /// </summary>
        /// <param name="ownerItem">Owner item.</param>
        /// <param name="ownerStObj">Owner object StObj information.</param>
        /// <param name="attributeName">Name as it appears in the attribute.</param>
        /// <returns>The name of the SetupObjectItem.</returns>
        protected abstract IContextLocNaming BuildFullName( IMutableSetupItem ownerItem, IStObjResult ownerStObj, string attributeName );

        /// <summary>
        /// Must create the <see cref="SetupObjectItem"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="ownerItem">Owner item.</param>
        /// <param name="ownerStObj">Owner object StObj information.</param>
        /// <param name="name">The name from <see cref="BuildFullName"/> method.</param>
        /// <returns>A new SetupObject.</returns>
        protected abstract SetupObjectItem CreateSetupObjectItem( IActivityMonitor monitor, IMutableSetupItem ownerItem, IStObjResult ownerStObj, IContextLocNaming name );

    }

}
