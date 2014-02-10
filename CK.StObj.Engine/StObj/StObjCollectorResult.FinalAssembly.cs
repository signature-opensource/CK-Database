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
        /// Generates final assembly (or not depending on <see cref="BuilderFinalAssemblyConfiguration.DoNotGenerateFinalAssembly"/>.
        /// </summary>
        /// <param name="monitor">Logger to use.</param>
        /// <param name="config">The <see cref="BuilderFinalAssemblyConfiguration"/> to use.</param>
        /// <returns>True on success, false if any error occured (logged into <paramref name="monitor"/>).</returns>
        public bool GenerateFinalAssembly( IActivityMonitor monitor, BuilderFinalAssemblyConfiguration config )
        {
            if( config == null ) throw new ArgumentNullException( "config" );
            if( config.DoNotGenerateFinalAssembly ) return true;
            return GenerateFinalAssembly( monitor, config.Directory, config.AssemblyName, config.ExternalVersionStamp, config.SignAssembly, config.SignKeyPair );
        }

        /// <summary>
        /// Generates final assembly.
        /// </summary>
        /// <param name="monitor">Logger to use.</param>
        /// <param name="directory">See <see cref="BuilderFinalAssemblyConfiguration.Directory"/>.</param>
        /// <param name="assemblyName">See <see cref="BuilderFinalAssemblyConfiguration.AssemblyName"/>.</param>
        /// <param name="externalVersionStamp">See <see cref="BuilderFinalAssemblyConfiguration.ExternalVersionStamp"/>.</param>
        /// <param name="signAssembly">See <see cref="BuilderFinalAssemblyConfiguration.SignAssembly"/>.</param>
        /// <param name="signKeyPair">See <see cref="BuilderFinalAssemblyConfiguration.SignKeyPair"/>.</param>
        /// <returns>True on success, false if any error occured (logged into <paramref name="monitor"/>).</returns>
        public bool GenerateFinalAssembly( IActivityMonitor monitor, string directory = null, string assemblyName = null, string externalVersionStamp = null, bool signAssembly = false, StrongNameKeyPair signKeyPair = null )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            if( HasFatalError ) throw new InvalidOperationException();
            try
            {
                if( String.IsNullOrEmpty( directory ) )
                {
                    directory = BuilderFinalAssemblyConfiguration.GetFinalDirectory( directory );
                    monitor.Info().Send( "No directory has been specified for final assembly. Trying to use the path of CK.StObj.Model assembly: {0}", directory );
                }
                if( String.IsNullOrEmpty( assemblyName ) )
                {
                    assemblyName = BuilderFinalAssemblyConfiguration.GetFinalAssemblyName( assemblyName );
                    monitor.Info().Send( "No assembly name has been specified for final assembly. Using default: {0}", assemblyName );
                }
                if( signAssembly )
                {
                    if( signKeyPair == null ) signKeyPair = DynamicAssembly.DynamicKeyPair;
                }
                else if( signKeyPair != null ) throw new ArgumentException( "A StrongNameKeyPair has been provided but signAssembly flag is false. signKeyPair must be null in this case." );

                DynamicAssembly a = new DynamicAssembly( directory, assemblyName, externalVersionStamp, signKeyPair );

                TypeBuilder root = a.ModuleBuilder.DefineType( StObjContextRoot.RootContextTypeName, TypeAttributes.Class | TypeAttributes.Sealed, typeof( StObjContextRoot ) );
                
                if( !FinalizeTypesCreationAndCreateCtor( monitor, a, root ) ) return false;
                root.CreateType();
                using( monitor.OpenTrace().Send( "Generating resource informations." ) )
                {
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
                        foreach( var e in typeMapping )
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
                }
                a.Save();
                return true;
            }
            catch( Exception ex )
            {
                monitor.Error().Send( ex, "While generating final assembly '{0}'.", assemblyName );
                return false;
            }
        }

        private bool FinalizeTypesCreationAndCreateCtor( IActivityMonitor monitor, DynamicAssembly a, TypeBuilder root )
        {
            int typeCreatedCount = 0;
            int typeErrorCount = 0;
            using( monitor.OpenInfo().Send( "Generating dynamic types." ) )
            {
                var ctor = root.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof( IActivityMonitor ), typeof( IStObjRuntimeBuilder ) } );
                var g = ctor.GetILGenerator();

                LocalBuilder locLogger = g.DeclareLocal( typeof( IActivityMonitor ) );
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
                        t = m.CreateFinalType( monitor, a );
                        if( t == null ) ++typeErrorCount;
                        ++typeCreatedCount;
                    }
                    else
                    {
                        t = m.AmbientTypeInfo.Type;
                    }
                    if( t != null )
                    {
                        g.LdLoc( allTypes );
                        g.LdInt32( m.IndexOrdered );
                        g.Emit( OpCodes.Ldtoken, t );
                        g.Emit( OpCodes.Call, typeFromToken );
                        g.Emit( OpCodes.Stelem_Ref );
                    }
                }
                var baseCtor = root.BaseType.GetConstructor( BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof( IActivityMonitor ), typeof( IStObjRuntimeBuilder ), typeof( Type[] ) }, null );
                Debug.Assert( baseCtor != null, "StObjContextRoot ctor signature is: ( IActivityMonitor monitor, IStObjRuntimeBuilder runtimeBuilder, Type[] allTypes )" );
                g.LdArg( 0 );
                g.LdArg( 1 );
                g.LdArg( 2 );
                g.LdLoc( allTypes );
                g.Emit( OpCodes.Call, baseCtor );

                g.Emit( OpCodes.Ret );

                if( typeErrorCount > 0 ) monitor.CloseGroup( String.Format( "Failed to generate {0} types out of {1}.", typeErrorCount, typeCreatedCount ) );
                else monitor.CloseGroup( String.Format( "{0} types generated.", typeCreatedCount ) );
            }
            return typeErrorCount == 0;
        }
    }
}
