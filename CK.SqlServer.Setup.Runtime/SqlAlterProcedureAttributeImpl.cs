using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
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
            object transformer = GetTransformerObject( state, stObj.ObjectType );
            if( transformer != null )
            {
                MethodInfo m = transformer.GetType().GetMethod( Member.Name );
                if( m == null )
                {
                    state.Monitor.Fatal().Send( "Unable to find transformer method '{0}' on '{1}'.", Member.Name, transformer.GetType().AssemblyQualifiedName );
                    return;
                }

            }

        }

        object GetTransformerObject( IStObjSetupDynamicInitializerState state, Type holderType )
        {
            string cacheKey = "Transformer\\" + holderType.AssemblyQualifiedName;
            object transformer = state.Memory[cacheKey];
            if( transformer == this ) return null;
            if( transformer == null )
            {
                AssemblyName a = holderType.Assembly.GetName();
                a.Name += ".Runtime";
                string transformerType = holderType.FullName + ", " + a.FullName;
                transformer = SimpleTypeFinder.WeakDefault.ResolveType( transformerType, false );
                if( transformer == null )
                {
                    state.Monitor.Fatal().Send( "Unable to locate transformer object. Tried '{0}'.", transformerType );
                    state.Memory[cacheKey] = this;
                }
            }
            return transformer;
        }
    }

}
