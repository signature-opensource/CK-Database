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
    /// <summary>
    /// Base implementation for <see cref="SetupObjectItemMemberAttributeBase"/> attributes applied to members that
    /// dynamically define one <see cref="SetupObjectItem"/>.
    /// </summary>
    public abstract class SetupObjectItemMemberAttributeImplBase : SetupObjectItemAttributeImplBase.ISetupItemCreator, IAttributeAmbientContextBoundInitializer, ISetupObjectItemProvider
    {
        readonly AmbientContextBoundDelegationAttribute _attribute;
        ICKCustomAttributeTypeMultiProvider _owner;
        MemberInfo _member;
        ISetupEngineAspectProvider _aspectProvider;
        SetupObjectItemAttributeImplBase.BestCreator _theBest;

        /// <summary>
        /// Initializes a new <see cref="SetupObjectItemMemberAttributeImplBase"/> bound to a <see cref="SetupObjectItemMemberAttributeBase"/>.
        /// </summary>
        /// <param name="a">The attribute.</param>
        protected SetupObjectItemMemberAttributeImplBase( SetupObjectItemMemberAttributeBase a )
            : this( a, a.ObjectName )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="SetupObjectItemMemberAttributeImplBase"/> bound to a name.
        /// </summary>
        /// <param name="a">Attribute object.</param>
        /// <param name="objectName">The object name.</param>
        protected SetupObjectItemMemberAttributeImplBase( AmbientContextBoundDelegationAttribute a, string objectName )
        {
            _attribute = a;
            ObjectName = SetupObjectItemAttributeImplBase.ExtractBehavior( out Behavior, objectName );
        }

        /// <summary>
        /// Name of the object.
        /// </summary>
        protected readonly string ObjectName;

        /// <summary>
        /// Registration behavior.
        /// </summary>
        protected readonly SetupObjectItemBehavior Behavior;

        /// <summary>
        /// Gets the original attribute.
        /// </summary>
        protected AmbientContextBoundDelegationAttribute Attribute => _attribute; 

        /// <summary>
        /// Gets the owner (type and provider of its other attributes).
        /// </summary>
        protected ICKCustomAttributeTypeMultiProvider Owner => _owner; 

        /// <summary>
        /// Gets the member to which the attribute applies.
        /// </summary>
        protected MemberInfo Member => _member; 

        /// <summary>
        /// Gets the aspects provider.
        /// </summary>
        protected ISetupEngineAspectProvider SetupEngineAspectProvider => _aspectProvider; 

        void IAttributeAmbientContextBoundInitializer.Initialize( ICKCustomAttributeTypeMultiProvider owner, MemberInfo m )
        {
            _owner = owner;
            _member = m;
        }

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            _aspectProvider = state.AspectProvider;
            var r = new SetupObjectItemAttributeImplBase.Registerer( state, item, stObj, this );
            _theBest = r.Register( Behavior, ObjectName );
            if( _theBest != null ) state.PushAction( DynamicItemInitializeAfterFollowing );
        }

        void DynamicItemInitializeAfterFollowing( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            Debug.Assert( _theBest != null );
            var r = new SetupObjectItemAttributeImplBase.Registerer( state, item, stObj, this );
            if( r.PostponeFinalizeRegister( _theBest ) && !r.HaseError ) state.PushAction( DynamicItemInitializeAfterFollowing );
        }

        /// <summary>
        /// Gets the best <see cref="SetupObjectItem"/> found.
        /// </summary>
        public SetupObjectItem SetupObjectItem => _theBest?.Item; 

        string SetupObjectItemAttributeImplBase.ISetupItemCreator.GetDetailedName( SetupObjectItemAttributeImplBase.Registerer r, string name )
        {
            return $"'{ObjectName}' in '{Member.Name}' {Attribute.GetShortTypeName()} attribute of '{r.Container.FullName}'";
        }

        IContextLocNaming SetupObjectItemAttributeImplBase.ISetupItemCreator.BuildFullName( SetupObjectItemAttributeImplBase.Registerer r, SetupObjectItemBehavior b, string name )
        {
            return BuildFullName( r, b, name );
        }

        SetupObjectItem SetupObjectItemAttributeImplBase.ISetupItemCreator.CreateSetupObjectItem( SetupObjectItemAttributeImplBase.Registerer r, IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument )
        {
            return CreateSetupObjectItem( r, firstContainer, name, transformArgument );
        }

        /// <summary>
        /// Must build the <see cref="IContextLocNaming"/> name of the future <see cref="SetupObjectItem"/> with the help of the owner object and the name in the attribute.
        /// This is called for each name in <see cref="SetupObjectItemAttributeBase.CommaSeparatedObjectNames"/>.
        /// </summary>
        /// <param name="Registerer">Registerer context object.</param>
        /// <param name="b">Registration behavior.</param>
        /// <param name="name">The raw name.</param>
        /// <returns>The name of the SetupObjectItem.</returns>
        protected abstract IContextLocNaming BuildFullName( SetupObjectItemAttributeImplBase.Registerer r, SetupObjectItemBehavior b, string name );

        /// <summary>
        /// Must create the <see cref="SetupObjectItem"/>.
        /// This is called for each name in <see cref="SetupObjectItemAttributeBase.CommaSeparatedObjectNames"/>
        /// after <see cref="BuildFullName"/> has been called.
        /// </summary>
        /// <param name="Registerer">Registerer context object.</param>
        /// <param name="firstContainer">
        /// The first container in which the item has been defined.
        /// When there is no replacement, this is the same as <see cref="SetupObjectItemAttributeImplBase.Registerer.Container"/>.
        /// </param>
        /// <param name="name">The name from <see cref="BuildFullName"/> method.</param>
        /// <param name="transformArgument">
        /// The transformation target if this setup item is a transformer.
        /// </param>
        /// <returns>
        /// A new SetupObject or null if it can not be created. If an errr occurred, it must 
        /// be logged to the monitor.
        /// </returns>
        protected abstract SetupObjectItem CreateSetupObjectItem( SetupObjectItemAttributeImplBase.Registerer r, IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument );

    }

}
