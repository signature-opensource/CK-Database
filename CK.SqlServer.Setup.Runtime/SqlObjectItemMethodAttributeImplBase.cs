#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlMethodForObjectItemAttributeImplBase.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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

namespace CK.SqlServer.Setup
{

    public abstract class SqlObjectItemMethodAttributeImplBase : SetupObjectItemMemberAttributeImplBase, IAutoImplementorMethod
    {
        readonly string _sqlObjectProtoItemType;
        bool _implementHasBeenAlreadyBeenCalled;

        /// <summary>
        /// Initializes a new <see cref="SqlObjectItemMethodAttributeImplBase"/> bound to a <see cref="SqlObjectItemMemberAttributeBase"/> 
        /// and a <see cref="SqlObjectProtoItem.Type"/>.
        /// </summary>
        /// <param name="a">The attribute.</param>
        /// <param name="sqlObjectProtoItemType">The type of the object.</param>
        protected SqlObjectItemMethodAttributeImplBase( SqlObjectItemMemberAttributeBase a, string sqlObjectProtoItemType )
            : base( a )
        {
            _sqlObjectProtoItemType = sqlObjectProtoItemType;
        }

        protected new SqlObjectItemMemberAttributeBase Attribute 
        { 
            get { return (SqlObjectItemMemberAttributeBase)base.Attribute; } 
        }

        protected override IContextLocNaming BuildFullName( IMutableSetupItem ownerItem, IStObjResult ownerStObj, string attributeName )
        {
            SqlPackageBaseItem p = (SqlPackageBaseItem)ownerItem;
            var name = new SqlContextLocName( attributeName );
            if( name.Context == null ) name.Context = p.Context;
            if( name.Location == null ) name.Location = p.Location;
            if( name.Schema == null ) name.Schema = p.GetObject().Schema;
            return name;
        }

        protected override SetupObjectItem CreateSetupObjectItem( IActivityMonitor monitor, IMutableSetupItem ownerItem, IStObjResult ownerStObj, IContextLocNaming name )
        {
            SqlPackageBaseItem p = (SqlPackageBaseItem)ownerItem;
            return SqlObjectItemAttributeImpl.LoadItemFromResource( monitor, p, Attribute.MissingDependencyIsError, (SqlContextLocName)name, _sqlObjectProtoItemType );
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
            // 3 - Ready to implement the method (BestSetupObjectItem has been initialized by DynamicItemInitialize).
            using( monitor.OpenInfo().Send( "Generating {0}.", SqlProcedureAttributeImpl.DumpMethodSignature( m ) ) )
            {
                return DoImplement( monitor, m, (SqlObjectItem)SetupObjectItem, dynamicAssembly, tB, isVirtual );
            }
        }

        /// <summary>
        /// Implements the given method on the given <see cref="TypeBuilder"/> that targets the given <see cref="SqlObjectItem"/>.
        /// Implementations can rely on the <paramref name="dynamicAssemblyMemory"/> to store shared information if needed.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="m">The method to implement.</param>
        /// <param name="sqlObjectItem">The associated <see cref="SqlObjectItem"/> (target of the method).</param>
        /// <param name="dynamicAssembly">Dynamic assembly being implemented.</param>
        /// <param name="b">The type builder to use.</param>
        /// <param name="isVirtual">True if a virtual method must be implemented. False if it must be sealed.</param>
        /// <returns>
        /// True if the method is actually implemented, false if, for any reason, another implementation (empty for instance) must be generated 
        /// (for instance, whenever the method is not ready to be implemented). Any error must be logged into the <paramref name="monitor"/>.
        /// </returns>
        protected abstract bool DoImplement( IActivityMonitor monitor, MethodInfo m, SqlObjectItem sqlObjectItem, IDynamicAssembly dynamicAssembly, TypeBuilder tB, bool isVirtual );

    }

}
