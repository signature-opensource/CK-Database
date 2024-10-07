using System;
using System.Reflection;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup;

public class SqlAlterProcedureAttributeImpl : SetupObjectItemRefMemberAttributeImplBase, IStObjSetupDynamicInitializer
{
    /// <summary>
    /// Initializes a new <see cref="SqlAlterProcedureAttributeImpl"/> bound 
    /// to a <see cref="SqlAlterProcedureAttribute"/>.
    /// </summary>
    /// <param name="a">The attribute.</param>
    public SqlAlterProcedureAttributeImpl( SqlAlterProcedureAttribute a )
        : base( a )
    {
    }

    /// <summary>
    /// Gets the original attribute.
    /// </summary>
    protected new SqlAlterProcedureAttribute Attribute => (SqlAlterProcedureAttribute)base.Attribute;

    void IStObjSetupDynamicInitializer.DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
    {
        state.PushNextRoundAction( DoTransform );
    }

    void DoTransform( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
    {
        object? transformer = GetTransformerObject( state, stObj.ClassType );
        if( transformer != null )
        {
            MethodInfo? m = transformer.GetType().GetMethod( Member.Name );
            if( m == null )
            {
                state.Monitor.Fatal( $"Unable to find transformer method '{Member.Name}' on '{transformer.GetType().AssemblyQualifiedName}'." );
                return;
            }
            SqlPackageBaseItem container = (SqlPackageBaseItem)item;
            m.Invoke( transformer, [new SqlTransformContext( state.Monitor, container, (SqlProcedureItem)SetupObjectItem )] );
        }
    }

    object? GetTransformerObject( IStObjSetupDynamicInitializerState state, Type holderType )
    {
        string cacheKey = "Transformer\\" + holderType.AssemblyQualifiedName;
        object transformer = state.Memory[cacheKey];
        if( transformer == this ) return null;
        if( transformer == null )
        {
            AssemblyName a = holderType.Assembly.GetName();
            a.Name += ".Engine";
            string transformerTypeName = holderType.FullName + ", " + a.FullName;
            Type transformerType = SimpleTypeFinder.WeakResolver( transformerTypeName, false );
            if( transformerType == null )
            {
                string altTypeName = "Engine." + transformerTypeName;
                transformerType = SimpleTypeFinder.WeakResolver( altTypeName, false );
                if( transformerType == null )
                {
                    state.Monitor.Fatal( $"Unable to locate transformer object. Tried '{transformerTypeName}' and '{altTypeName}'." );
                    state.Memory[cacheKey] = this;
                    return null;
                }
            }
            transformer = Activator.CreateInstance( transformerType );
            state.Memory[cacheKey] = transformer;
        }
        return transformer;
    }
}
