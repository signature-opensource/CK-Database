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
    /// overridable <see cref="CreateSqlBaseItem"/> factory method or the more basic <see cref="CreateSetupObjectItem"/>.
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
        /// <param name="parser">The sql parser service that will be used..</param>
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
        /// This method simply calls the <see cref="SqlBaseItem.SqlBuildFullName"/> static helper.
        /// </summary>
        /// <param name="container">The item's container.</param>
        /// <param name="b">The behavior (Define, Replace or Transform).</param>
        /// <param name="attributeName">The raw attribute name.</param>
        /// <returns>The context-location-name for the item.</returns>
        protected override IContextLocNaming BuildFullName( ISetupItem container, SetupObjectItemBehavior b, string attributeName )
        {
            return SqlBaseItem.SqlBuildFullName( (SqlPackageBaseItem)container, b, attributeName );
        }

        /// <summary>
        /// Creates the <see cref="SetupObjectItem"/>.
        /// This is called only once the potential replacements have been analysed and resolved.
        /// This implementation simply calls the centralized <see cref="SqlBaseItem.CreateStandardSqlBaseItem"/> helper.
        /// </summary>
        /// <param name="registerer">The registerer that gives access to the <see cref="IStObjSetupDynamicInitializerState"/>.</param>
        /// <param name="firstContainer">
        /// The first container that defined this object.
        /// Actual container if the object has been replaced is provided by 
        /// the registerer's container (<see cref="SetupObjectItemAttributeRegisterer.Container" />).
        /// </param>
        /// <param name="name">Full name of the object to create.</param>
        /// <param name="transformArgument">Optional transform argument if this object is a transformer.</param>
        /// <returns>The created object or null if an error occurred and has been logged.</returns>
        protected override SetupObjectItem CreateSetupObjectItem( SetupObjectItemAttributeRegisterer registerer, IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument )
        {
            return SqlBaseItem.CreateStandardSqlBaseItem(
                _parser,
                registerer,
                (SqlContextLocName)name,
                (SqlPackageBaseItem)firstContainer,
                (SqlBaseItem)transformArgument,
                new[] { _expectedItemType },
                CreateSqlBaseItem );
        }

        /// <summary>
        /// Extension point to create specialized <see cref="SqlBaseItem"/> (other than the standard objects like <see cref="SqlViewItem"/>,
        /// or <see cref="SqlProcedureItem"/>).
        /// Returns null by default: returning null triggers the use of a default factory that handles the standard items.
        /// This can also be used to inspect/validate the <paramref name="text"/> since error or fatal logged to the monitor stops the process.
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
