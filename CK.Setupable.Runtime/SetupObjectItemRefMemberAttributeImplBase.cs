using System;
using System.Linq;
using System.Reflection;
using CK.Core;

namespace CK.Setup;

/// <summary>
/// Implementation of <see cref="SetupObjectItemRefMemberAttributeBase"/>.
/// </summary>
public abstract class SetupObjectItemRefMemberAttributeImplBase : IAttributeContextBoundInitializer
{
    readonly SetupObjectItemRefMemberAttributeBase _attribute;
    ITypeAttributesCache _owner;
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
    protected ITypeAttributesCache Owner => _owner;

    /// <summary>
    /// Gets the member to which the attribute applies.
    /// </summary>
    protected MemberInfo Member => _member;

    void IAttributeContextBoundInitializer.Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
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
