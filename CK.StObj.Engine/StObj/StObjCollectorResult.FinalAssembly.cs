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
using System.IO;

namespace CK.Setup
{
    public partial class StObjCollectorResult : MultiContextualResult<StObjCollectorContextualResult>
    {
        /// <summary>
        /// Generates final assembly (or not depending on <see cref="StObjFinalAssemblyConfiguration.DoNotGenerateFinalAssembly"/>.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="config">The <see cref="StObjFinalAssemblyConfiguration"/> to use.</param>
        public void GenerateFinalAssembly( IActivityLogger logger, StObjFinalAssemblyConfiguration config )
        {
            if( config == null ) throw new ArgumentNullException( "config" );
            if( config.DoNotGenerateFinalAssembly ) return;
            GenerateFinalAssembly( logger, config.Directory, config.AssemblyName, config.ExternalVersionStamp, config.SignAssembly, config.SignKeyPair );
        }

        /// <summary>
        /// Generates final assembly.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="directory">See <see cref="StObjFinalAssemblyConfiguration.Directory"/>.</param>
        /// <param name="assemblyName">See <see cref="StObjFinalAssemblyConfiguration.AssemblyName"/>.</param>
        /// <param name="externalVersionStamp">See <see cref="StObjFinalAssemblyConfiguration.ExternalVersionStamp"/>.</param>
        /// <param name="signAssembly">See <see cref="StObjFinalAssemblyConfiguration.SignAssembly"/>.</param>
        /// <param name="signKeyPair">See <see cref="StObjFinalAssemblyConfiguration.SignKeyPair"/>.</param>
        public void GenerateFinalAssembly( IActivityLogger logger, string directory = null, string assemblyName = null, string externalVersionStamp = null, bool signAssembly = false, StrongNameKeyPair signKeyPair = null )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            if( HasFatalError ) throw new InvalidOperationException();
            if( String.IsNullOrEmpty( directory ) )
            {
                directory = StObjFinalAssemblyConfiguration.GetFinalDirectory( directory );
                logger.Info( "No directory has been specified for final assembly. Trying to use the path of CK.StObj.Model assembly: {0}", directory );
            }
            if( String.IsNullOrEmpty( assemblyName ) )
            {
                assemblyName = StObjFinalAssemblyConfiguration.GetFinalAssemblyName( assemblyName );
                logger.Info( "No assembly name has been specified for final assembly. Using default: {0}", assemblyName );
            }
            if( signAssembly )
            {
                if( signKeyPair == null ) signKeyPair = DynamicAssembly.DynamicKeyPair;
            }
            else if( signKeyPair != null ) throw new ArgumentException( "A StrongNameKeyPair has been provided but signAssembly flag is false. signKeyPair must be null in this case." );

            DynamicAssembly a = new DynamicAssembly( directory, assemblyName, externalVersionStamp, signKeyPair );
            
            TypeBuilder root = a.ModuleBuilder.DefineType( StObjContextRoot.RootContextTypeName, TypeAttributes.Class | TypeAttributes.Sealed, typeof( StObjContextRoot ), Type.EmptyTypes );
            FinalizeTypesCreationAndCreateCtor( logger, a, root );
            root.CreateType();

            RawOutStream outS = new RawOutStream();
            outS.Writer.Write( Contexts.Count );
            foreach( var c in Contexts )
            {
                outS.Writer.Write( c.Context );
                IDictionary all = c.InternalMapper.RawMappings;
                var typeMapping = all.Cast<KeyValuePair<object, MutableItem>>().Where( e => e.Key is Type );
                int typeMappingCount = typeMapping.Count();
                // Serializes multiple ...,Type.AssemblyQualifiedName,int,... for Type to final Type/Item mappings (where index is the IndexOrdered in allTypes).
                // We skip highest implementation Type mappings - AmbientContractInterfaceKey keys - since there is no ToStObj mapping on final (runtime) IContextualStObjMap.
                outS.Writer.Write( typeMappingCount );
                foreach( var e in typeMapping  )
                {
                    outS.Writer.Write( ((Type)e.Key).AssemblyQualifiedName );
                    outS.Writer.Write( e.Value.IndexOrdered );
                }
            }
            outS.Writer.Flush();
            // Once Contexts are serialized, we serialize the values that have been injected during graph construction.
            outS.Formatter.Serialize( outS.Memory, _buildValueCollector.Values.ToArray() );

            outS.Writer.Write( _totalSpecializationCount );
            foreach( var m in _orderedStObjs )
            {
                outS.Writer.Write( m.Context.Context );
                outS.Writer.Write( m.Generalization != null ? m.Generalization.IndexOrdered : -1 );
                outS.Writer.Write( m.Specialization != null ? m.Specialization.IndexOrdered : -1 );
                outS.Writer.Write( m.LeafSpecialization.IndexOrdered );
                outS.Writer.Write( m.SpecializationIndexOrdered );

                if( m.AmbientTypeInfo.Construct != null )
                {
                    outS.Writer.Write( m.ConstructParameters.Count );
                    foreach( MutableParameter p in m.ConstructParameters )
                    {
                        outS.Writer.Write( p.BuilderValueIndex );
                    }
                }
                else outS.Writer.Write( -1 );
                m.WritePreConstructProperties( outS.Writer );
                if( m.Specialization == null ) m.WritePostBuildProperties( outS.Writer );
             }
            // Generates the Resource BLOB now.
            outS.Memory.Position = 0;
            a.ModuleBuilder.DefineManifestResource( StObjContextRoot.RootContextTypeName + ".Data", outS.Memory, ResourceAttributes.Private );
            
            a.Save();
        }

        private void FinalizeTypesCreationAndCreateCtor( IActivityLogger logger, DynamicAssembly a, TypeBuilder root )
        {
            var ctor = root.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof( IActivityLogger ) } );
            var g = ctor.GetILGenerator();

            LocalBuilder locLogger = g.DeclareLocal( typeof( IActivityLogger ) );
            g.LdArg( 1 );
            g.StLoc( locLogger );

            LocalBuilder allTypes = g.DeclareLocal( typeof( Type[] ) );
            g.LdInt32( _orderedStObjs.Count );
            g.Emit( OpCodes.Newarr, typeof( Type ) );
            g.StLoc( allTypes );

            MethodInfo typeFromToken = typeof( Type ).GetMethod( "GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public );

            foreach( var m in _orderedStObjs )
            {
                Type t = null;
                if( m.Specialization == null )
                {
                    t = m.CreateFinalType( logger, a );
                }
                else
                {
                    t = m.AmbientTypeInfo.Type;
                }
                g.LdLoc( allTypes );
                g.LdInt32( m.IndexOrdered );
                g.Emit( OpCodes.Ldtoken, t );
                g.Emit( OpCodes.Call, typeFromToken );
                g.Emit( OpCodes.Stelem_Ref );
            }
            
            var baseCtor = root.BaseType.GetConstructor( BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof( IActivityLogger ), typeof( Type[] ) }, null );
            g.LdArg( 0 );
            g.LdArg( 1 );
            g.LdLoc( allTypes );
            g.Emit( OpCodes.Call, baseCtor );

            g.Emit( OpCodes.Ret );
        }
    }
}
