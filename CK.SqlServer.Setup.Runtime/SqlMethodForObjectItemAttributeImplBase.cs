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
    public abstract class SqlMethodForObjectItemAttributeImplBase : IStObjSetupDynamicInitializer, IAutoImplementorMethod
    {
        readonly SqlMethodForObjectItemAttributeBase _attr;
        readonly string _sqlObjectProtoItemType;
        MethodInfo _method;
        BestInitializer _theBest;

        /// <summary>
        /// Initializes a new <see cref="SqlMethodForObjectItemAttributeImplBase"/> bound to a <see cref="SqlMethodForObjectItemAttributeBase"/> 
        /// and a <see cref="SqlObjectProtoItem.Type"/>.
        /// </summary>
        /// <param name="a">The attribute.</param>
        /// <param name="sqlObjectProtoItemType">The type of the object.</param>
        protected SqlMethodForObjectItemAttributeImplBase( SqlMethodForObjectItemAttributeBase a, string sqlObjectProtoItemType )
        {
            _attr = a;
            _sqlObjectProtoItemType = sqlObjectProtoItemType;
        }

        /// <summary>
        /// This is used both for the key and the value.
        /// This secures the key in the memory dictionary: only a private BestInitializer can be equal to a BestInitializer.
        /// </summary>
        class BestInitializer
        {
            int _hash;

            public BestInitializer( string[] names )
            {
                Names = names;
                _hash = names[0].GetHashCode();
            }

            public override bool Equals( object obj )
            {
                BestInitializer x = obj as BestInitializer;
                return x != null && x.Names[0] == Names[0];
            }

            public override int GetHashCode()
            {
                return _hash;
            }

            public readonly string[] Names;

            public IStObjSetupDynamicInitializer Initializer;

            public SqlObjectItem Item;
        }

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            // 2 - Finds the most specific responsible of this resource.
            //      - first, gets the name of the external object.
            //      - Based on the name, registers this initializer as being the most precise one: this can be overridden (and will be) 
            //        by followers that are bound to the same external name.
            //      - Pushes an action that will be executed after followers have been executed.
            //
            SqlPackageBaseItem packageItem = (SqlPackageBaseItem)item;
            string[] names = SqlObjectItemAttributeImpl.BuildNames( packageItem.GetObject(), _attr.ObjectName );
            if( names == null )
            {
                state.Monitor.Error().Send( "Invalid object name '{0}' in attribute of '{1}' for '{2}'.", _attr.ObjectName, _method.Name, item.FullName );
                return;
            }
            _theBest = AssumeBestInitializer( state, names, this );
            state.PushAction( DynamicItemInitializeAfterFollowing );
        }

        private static BestInitializer AssumeBestInitializer( IStObjSetupDynamicInitializerState state, string[] names, IStObjSetupDynamicInitializer initializer )
        {
            var meBest = new BestInitializer( names );
            BestInitializer theBest = (BestInitializer)state.Memory[meBest];
            if( theBest == null ) state.Memory[meBest] = theBest = meBest;
            Debug.Assert( theBest.Names[0] == names[0] );
            theBest.Initializer = initializer;
            theBest.Names[1] = names[1];
            theBest.Names[2] = names[2];
            return theBest;
        }

        void DynamicItemInitializeAfterFollowing( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            SqlPackageBaseItem packageItem = (SqlPackageBaseItem)item;
            // If we are the best, our resource wins.
            if( _theBest.Initializer == this )
            {
                Debug.Assert( _theBest.Item == null, "We are the only winner." );
                // 2 - Attempts to load the resource.
                SqlObjectProtoItem proto = SqlObjectItemAttributeImpl.LoadProtoItemFromResource( state.Monitor, packageItem, _theBest.Names, _sqlObjectProtoItemType );
                if( proto == null ) return;
                // On success, creates the SqlObjectItem bound to the MethodInfo that must call it.
                _theBest.Item = proto.CreateItem( state.Monitor );
                if( _theBest.Item != null )
                {
                    if( !_theBest.Item.MissingDependencyIsError.HasValue ) _theBest.Item.MissingDependencyIsError = _attr.MissingDependencyIsError;
                    packageItem.Children.Add( _theBest.Item );
                }
            }
        }

        bool IAutoImplementorMethod.Implement( IActivityMonitor monitor, MethodInfo m, IDynamicAssembly dynamicAssembly, TypeBuilder tB, bool isVirtual )
        {
            // 1 - Not ready to implement anything (no body yet): 
            //     - memorizes the MethodInfo.
            //     - returns false to implement a stub.
            if( _theBest == null || _theBest.Item == null )
            {
                if( _method != null )
                {
                    monitor.Warn().Send( "DynamicItemInitialize has not been called: no resource should have been found for method {0}.", _method.Name );
                }
                _method = m;
                return false;
            }
            // 3 - Ready to implement the method (_theBest.Item has been initialized by DynamicItemInitialize above).
            using( monitor.OpenInfo().Send( "Generating method '{0}.{1}'.", m.DeclaringType.FullName, m.Name ) )
            {
                return DoImplement( monitor, m, _theBest.Item, dynamicAssembly, tB, isVirtual );
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
