using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;

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
        bool _implementHasBeenAlreadyBeenCalled;

        /// <summary>
        /// Initializes a new <see cref="SqlBaseItemMethodAttributeImplBase"/> bound to a <see cref="SetupObjectItemMemberAttributeBase"/> 
        /// and a item type ("Function", "Procedure", etc.).
        /// </summary>
        /// <param name="a">The attribute.</param>
        /// <param name="expectedItemType">The expected type of the object.</param>
        protected SqlBaseItemMethodAttributeImplBase( SetupObjectItemMemberAttributeBase a, string expectedItemType )
            : base( a )
        {
            _expectedItemType = expectedItemType;
        }

        protected override IContextLocNaming BuildFullName( SetupObjectItemAttributeRegisterer r, SetupObjectItemBehavior b, string attributeName )
        {
            return r.SqlBuildFullName( b, attributeName );
        }

        protected override SetupObjectItem CreateSetupObjectItem( SetupObjectItemAttributeRegisterer r, IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument )
        {
            ISqlSetupAspect sql = SetupEngineAspectProvider.GetSetupEngineAspect<ISqlSetupAspect>();
            return SqlBaseItem.Create( 
                sql.SqlParser, 
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

        bool IAutoImplementorMethod.Implement( IActivityMonitor monitor, MethodInfo m, IDynamicAssembly dynamicAssembly, TypeBuilder tB, bool isVirtual )
        {
            // 1 - Not ready to implement anything (no body yet): 
            //     - Checks that the MethodInfo is the Member (Debug only).
            //     - returns false to implement a stub.
            if( SetupObjectItem == null )
            {
                if( _implementHasBeenAlreadyBeenCalled )
                {
                    monitor.Warn().Send( "Implement has already been called: no resource should have been found for method {0}.", Member.Name );
                }
                else
                {
                    Debug.Assert( CK.Reflection.MemberInfoEqualityComparer.Default.Equals( m, Member ), "IAutoImplementorMethod called with a method that differs from the IAttributeAmbientContextBoundInitializer initilaized member." );
                    _implementHasBeenAlreadyBeenCalled = true;
                }
                return false;
            }
            // 3 - Ready to implement the method (SetupObjectItem has been initialized by DynamicItemInitialize).
            using( monitor.OpenInfo().Send( "Generating {0}.", SqlCallableAttributeImpl.DumpMethodSignature( m ) ) )
            {
                var target = SetupObjectItem is SqlTransformerItem
                                ? ((SqlTransformerItem)SetupObjectItem).Target
                                : (SetupObjectItem.TransformTarget ?? SetupObjectItem);
                var item = (SqlObjectItem)target;
                return DoImplement( monitor, m, item, dynamicAssembly, tB, isVirtual );
            }
        }

        /// <summary>
        /// Implements the given method on the given <see cref="TypeBuilder"/> that is bound to the given <see cref="SqlObjectItem"/>.
        /// Implementations can rely on the <paramref name="dynamicAssembly"/> to store shared information if needed.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="m">The method to implement.</param>
        /// <param name="sqlItem">The associated <see cref="SqlBaseItem"/> (target of the method).</param>
        /// <param name="dynamicAssembly">Dynamic assembly being implemented.</param>
        /// <param name="tB">The type builder to use.</param>
        /// <param name="isVirtual">True if a virtual method must be implemented. False if it must be sealed.</param>
        /// <returns>
        /// True if the method is actually implemented, false if, for any reason, another implementation (empty for instance) must be generated 
        /// (for instance, whenever the method is not ready to be implemented). 
        /// Any error must be logged into the <paramref name="monitor"/>.
        /// </returns>
        protected abstract bool DoImplement( IActivityMonitor monitor, MethodInfo m, SqlBaseItem sqlItem, IDynamicAssembly dynamicAssembly, TypeBuilder tB, bool isVirtual );

    }

}
