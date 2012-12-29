using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using CK.Reflection;
using System.Resources;
using System.Collections;

namespace CK.Setup
{
    public partial class StObjCollectorResult : MultiContextualResult<StObjCollectorContextualResult>
    {

        public void GenerateFinalAssembly( IActivityLogger logger, string directory, string assemblyName = null )
        {
            if( HasFatalError ) throw new InvalidOperationException();
            DynamicAssembly a = new DynamicAssembly( directory, assemblyName ?? DynamicAssembly.DefaultAssemblyName );
            
            TypeBuilder root = a.ModuleBuilder.DefineType( StObjContextRoot.RootContextTypeName, TypeAttributes.Class | TypeAttributes.Sealed, typeof( StObjContextRoot ), Type.EmptyTypes );
            {
                var ctor = root.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes );
                var g = ctor.GetILGenerator();
                var baseCtor = root.BaseType.GetConstructor( BindingFlags.NonPublic|BindingFlags.Instance, null, Type.EmptyTypes, null );
                g.Emit( OpCodes.Ldarg_0 );
                g.Emit( OpCodes.Call, baseCtor );
                g.Emit( OpCodes.Ret );
            }
            root.CreateType();

            Type[] finalTypes = new Type[_totalSpecializationCount];
            foreach( var m in _orderedStObjs )
            {
                if( m.Specialization == null )
                {
                    finalTypes[m.SpecializationIndexOrdered] = m.CreateFinalType( logger, a );
                }
            }

            RawOutStream outS = new RawOutStream();
            outS.Serialize( finalTypes );
            
            outS.Writer.Write( Contexts.Count );
            foreach( var c in Contexts )
            {
                outS.Writer.Write( c.Context );
                outS.Writer.Flush();
                // Serializes an array of Tuple<Type,int> for Type to final Type mappings.
                {
                    var mappings = c.StObjMapper.TypeMappings.GetRawMappings().Cast<KeyValuePair<object,MutableItem>>()
                                    .Where( e => e.Key is Type )
                                    .Select( e => Tuple.Create( (Type)e.Key, e.Value.SpecializationIndexOrdered ) );
                    outS.Serialize( mappings.ToArray() );

                }
                // Serializes Tuple<Type,Type> for Type to final Type mappings.
                {
                    var mappings = c.StObjMapper.TypeMappings.GetRawMappings().Cast<KeyValuePair<object, MutableItem>>()
                                    .Where( e => e.Key is AmbientContractInterfaceKey )
                                    .Select( e => Tuple.Create( ((AmbientContractInterfaceKey)e.Key).InterfaceType, e.Value.AmbientTypeInfo.Type ) );
                    outS.Serialize( mappings.ToArray() );
                }
            }
            outS.Memory.Position = 0;
            a.ModuleBuilder.DefineManifestResource( StObjContextRoot.RootContextTypeName + ".Data", outS.Memory, ResourceAttributes.Private );
            
            a.Save();
        }

    }
}
