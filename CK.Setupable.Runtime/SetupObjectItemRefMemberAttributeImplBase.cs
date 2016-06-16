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
    public abstract class SetupObjectItemRefMemberAttributeImplBase : IAttributeAmbientContextBoundInitializer
    {
        readonly SetupObjectItemRefMemberAttributeBase _attribute;
        ICKCustomAttributeTypeMultiProvider _owner;
        MemberInfo _member;
        ISetupObjectItemProvider _setupItemProvider;

        /// <summary>
        /// Initializes a new <see cref="SetupObjectItemRefMemberAttributeImplBase"/> bound to a <see cref="SetupObjectItemRefMemberAttributeBase"/>.
        /// </summary>
        /// <param name="a">The attribute.</param>
        protected SetupObjectItemRefMemberAttributeImplBase( SetupObjectItemRefMemberAttributeBase a )
        {
            _attribute = a;
        }

        /// <summary>
        /// Gets the original attribute.
        /// </summary>
        protected SetupObjectItemRefMemberAttributeBase Attribute => _attribute; 

        /// <summary>
        /// Gets the owner (type and provider of its other attributes).
        /// </summary>
        protected ICKCustomAttributeTypeMultiProvider Owner => _owner; 

        /// <summary>
        /// Gets the member to which the attribute applies.
        /// </summary>
        protected MemberInfo Member => _member; 

        void IAttributeAmbientContextBoundInitializer.Initialize( ICKCustomAttributeTypeMultiProvider owner, MemberInfo m )
        {
            _owner = owner;
            _member = m;
            int count = _owner.GetCustomAttributes<ISetupObjectItemProvider>( _member ).Count();
            if( count == 0 ) throw new CKException( "Attribute {0} on {1}.{2} cannot find a SetupItem. There must be one and only one attribute that defines a SetupObjectItem.", Attribute.GetShortTypeName(), _owner.Type.Name, _member.Name );
            if( count > 1 ) throw new CKException( "Attribute {0} on {1}.{2} found multiple SetupItem. There must be one and only one attribute that defines a SetupObjectItem.", Attribute.GetShortTypeName(), _owner.Type.Name, _member.Name );
            _setupItemProvider = _owner.GetCustomAttributes<ISetupObjectItemProvider>( _member ).Single();
        }

        /// <summary>
        /// Gets the associated <see cref="SetupObjectItem"/> to work with.
        /// </summary>
        public SetupObjectItem SetupObjectItem => _setupItemProvider.SetupObjectItem; 

    }

}
