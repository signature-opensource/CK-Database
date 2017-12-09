using CK.CodeGen.Abstractions;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;
using System;
using System.Reflection;
using System.Text;

namespace CK.SqlServer.Setup
{

    /// <summary>
    /// Implementation for any specialization of <see cref="SetupObjectItemMemberAttributeBase"/>.
    /// This attribute implementation associates a <see cref="SqlBaseItem"/> to an abstract method 
    /// that is emitted thanks to <see cref="DoImplement"/>.
    /// Any kind of SqlBaseItem can be handled by this base class thanks to the 
    /// overridable <see cref="CreateSqlBaseItem"/> factory method.
    /// </summary>
    public abstract class SqlBaseItemMethodAttributeImplBase : SetupObjectItemMemberAttributeImplBase, IAutoImplementorMethod
    {
        readonly string _expectedItemType;
        readonly ISqlServerParser _parser;

        /// <summary>
        /// Initializes a new <see cref="SqlBaseItemMethodAttributeImplBase"/> bound to a <see cref="SetupObjectItemMemberAttributeBase"/> 
        /// and a item type ("Function", "Procedure", etc.).
        /// </summary>
        /// <param name="a">The attribute.</param>
        /// <param name="expectedItemType">The expected type of the object.</param>
        protected SqlBaseItemMethodAttributeImplBase( SetupObjectItemMemberAttributeBase a, ISqlServerParser parser, string expectedItemType )
            : base( a )
        {
            _parser = parser;
            _expectedItemType = expectedItemType;
        }

        /// <summary>
        /// Must build the full name of the item based on the raw attribute name, whether this is
        /// a definition, a replacement or a transformation and the container of the item.
        /// </summary>
        /// <param name="container">The item's container.</param>
        /// <param name="b">The behavior (Define, Replace or Transform).</param>
        /// <param name="attributeName">The raw attribute name.</param>
        /// <returns>The context-location-name for the item.</returns>
        protected override IContextLocNaming BuildFullName( ISetupItem container, SetupObjectItemBehavior b, string attributeName )
        {
            return SqlBaseItemAttributeImpl.SqlBuildFullName( (SqlPackageBaseItem)container, b, attributeName );
        }

        protected override SetupObjectItem CreateSetupObjectItem( SetupObjectItemAttributeRegisterer r, IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument )
        {
            return SqlBaseItem.Create(
                _parser,
                r,
                (SqlContextLocName)name,
                (SqlPackageBaseItem)firstContainer,
                (SqlBaseItem)transformArgument,
                new[] { _expectedItemType },
                CreateSqlBaseItem );
        }

        /// <summary>
        /// Extension point to create specialized <see cref="SqlBaseItem"/> (other than the standard objects like <see cref="SqlViewItem"/>,
        /// or <see cref="SqlProcedure"/>).
        /// Returns null by default: returning null triggers the use of a default factory that handles the standard items.
        /// This can also be used to inspect/validate the <paramref name="text"/> since error or fatal logged to the <paramref name="monitor"/> stops the process.
        /// </summary>
        /// <param name="r">The registerer that gives access to the <see cref="IStObjSetupDynamicInitializerState"/>.</param>
        /// <param name="name">The item name.</param>
        /// <param name="text">The parsed text.</param>
        /// <returns>A new <see cref="SqlBaseItem"/> or null (if an error occured or the default factory must be used).</returns>
        protected virtual SqlBaseItem CreateSqlBaseItem( SetupObjectItemAttributeRegisterer r, SqlContextLocName name, ISqlServerParsedText text )
        {
            return null;
        }

        bool IAutoImplementorMethod.Implement( IActivityMonitor monitor, MethodInfo m, IDynamicAssembly dynamicAssembly, ITypeScope tS )
        {
            using( monitor.OpenInfo( $"Generating {SqlCallableAttributeImpl.DumpMethodSignature( m )}." ) )
            {
                SqlBaseItem sqlItem = null;

                string methodKey = $"Method:{m.Module.ModuleVersionId}.{m.MetadataToken}";

                // SetupObjectItem is initialized by DynamicItemInitialize.
                // If it is null, we are be in a "second run" (a SetupFolder).
                if( dynamicAssembly.IsSecondaryRun )
                {
                    sqlItem = (SqlBaseItem)dynamicAssembly.GetPrimaryRunResult( methodKey );
                }
                else
                {
                    if( SetupObjectItem == null ) throw new Exception( "SetupObjectItem must have been initialized by DynamicItemInitialize." );
                    var target = SetupObjectItem is SqlTransformerItem
                                ? ((SqlTransformerItem)SetupObjectItem).Target
                                : (SetupObjectItem.TransformTarget ?? SetupObjectItem);
                    sqlItem = (SqlBaseItem)target;
                    dynamicAssembly.SetPrimaryRunResult( methodKey, sqlItem, false );
                }
                return DoImplement( monitor, m, sqlItem, dynamicAssembly, tS );
            }
        }

        /// <summary>
        /// Implements the given method on the given <see cref="ITypeScope"/> that is bound to the given <see cref="SqlObjectItem"/>.
        /// Implementations can rely on the <paramref name="dynamicAssembly"/> to store shared information if needed.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="m">The method to implement.</param>
        /// <param name="sqlItem">The associated <see cref="SqlBaseItem"/> (target of the method).</param>
        /// <param name="dynamicAssembly">Dynamic assembly being implemented.</param>
        /// <param name="b">The class builder to use.</param>
        /// <returns>
        /// True on success, false on error. 
        /// Any error must be logged into the <paramref name="monitor"/>.
        /// </returns>
        protected abstract bool DoImplement( IActivityMonitor monitor, MethodInfo m, SqlBaseItem sqlItem, IDynamicAssembly dynamicAssembly, ITypeScope b );
    }

}
