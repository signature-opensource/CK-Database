using System;
using System.Collections;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public partial class SqlProcedureAttributeImpl : IStObjSetupDynamicInitializer, IAutoImplementorMethod
    {
        readonly SqlProcedureAttribute _attr;       
        MethodInfo _method;
        BestInitializer _theBest;

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

        public SqlProcedureAttributeImpl( SqlProcedureAttribute a )
        {
            _attr = a;
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
            string[] names = SqlObjectItemAttributeImpl.BuildNames( packageItem.Object, _attr.ProcedureName );
            if( names == null )
            {
                state.Monitor.Error().Send( "Invalid object name '{0}' in attribute of '{1}' for '{2}'.", _attr.ProcedureName, _method.Name, item.FullName );
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
                SqlObjectProtoItem proto = SqlObjectItemAttributeImpl.LoadProtoItemFromResource( state.Monitor, packageItem, _theBest.Names, SqlObjectProtoItem.TypeProcedure );
                if( proto == null ) return;
                // On success, creates the SqlProcedureItem bound to the MethodInfo that must call it.
                _theBest.Item = proto.CreateProcedureItem( state.Monitor );
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
                _method = m;
                return false;
            }
            // 3 - Ready to implement the method (_theBest.Item has been initialized by DynamicItemInitialize above).
            using( monitor.OpenInfo().Send( "Generating method '{0}.{1}'.", m.DeclaringType.FullName, m.Name ) )
            {
                SqlProcedureItem item = _theBest.Item as SqlProcedureItem;
                MethodInfo mCreateCommand = item != null ? item.AssumeCommandBuilder( monitor, dynamicAssembly, (ModuleBuilder)tB.Module ) : null;
                if( mCreateCommand == null )
                {
                    monitor.Error().Send( "Invalid low level creation method for '{0}'.", item.FullName );
                    return false;
                }

                ParameterInfo[] mParameters = m.GetParameters();
                GenerationType gType;
                if( m.ReturnType == typeof( void )
                        && mParameters.Length >= 1
                        && mParameters[0].ParameterType.IsByRef
                        && !mParameters[0].IsOut
                        && mParameters[0].ParameterType.GetElementType() == SqlObjectItem.TypeCommand )
                {
                    gType = GenerationType.ByRefSqlCommand;
                }
                else
                {
                    if( m.ReturnType == SqlObjectItem.TypeCommand ) gType = GenerationType.ReturnSqlCommand;
                    else
                    {
                        if( !m.ReturnType.GetConstructors().Any( ctor => ctor.GetParameters().Any( p => p.ParameterType == SqlObjectItem.TypeCommand && !p.ParameterType.IsByRef && !p.HasDefaultValue ) ) )
                        {
                            monitor.Error().Send( "Ctor '{0}.{1}' must return a SqlCommand -OR- a type that has at least one constructor with a non optional SqlCommand (among other parameters) -OR- accepts a SqlCommand by reference as its first argument.", m.DeclaringType.FullName, m.Name );
                            return false;
                        }
                        gType = GenerationType.ReturnWrapper;
                    }
                }
                SqlExprParameterList sqlParameters = item.OriginalStatement.Parameters;
                return GenerateCreateSqlCommand( gType, monitor, mCreateCommand, item.OriginalStatement.Name, sqlParameters, m, mParameters, tB, isVirtual );
            }
        }

    }
}
