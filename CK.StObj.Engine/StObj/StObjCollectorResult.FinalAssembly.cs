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
        /// Makes each <see cref="IStObjResult.ObjectAccessor"/> a function that obtains its object from the <see cref="StObjContextRoot"/>.
        /// The <see cref="IStObjResult.InitialObject"/> remains the same: this is useful if the object itself implements interfaces (such
        /// as <see cref="IStObjStructuralConfigurator"/>) that configures the setup process to be able to keep some context across calls.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="finalObjects">The final mapper.</param>
        /// <returns>True on success, false on error.</returns>
        public bool InjectFinalObjectAccessor( IActivityMonitor monitor, StObjContextRoot finalObjects )
        {
            try
            {
                foreach( MutableItem o in _orderedStObjs )
                {
                    if( o.Specialization == null )
                    {
                        o.InjectFinalObjectAccessor( finalObjects );
                    }
                }
                return true;
            }
            catch( Exception ex )
            {
                monitor.Error().Send( ex );
                return false;
            }
        }

        /// <summary>
        /// Generates final assembly. It is optionaly saved to disk depending on <see cref="BuilderFinalAssemblyConfiguration.DoNotGenerateFinalAssembly"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="runtimeBuilder">Runtime builder to use to create final mapper object.</param>
        /// <param name="config">The <see cref="BuilderFinalAssemblyConfiguration"/> to use.</param>
        /// <returns>The final objects map, null if any error occured (logged into <paramref name="monitor"/>).</returns>
        public StObjContextRoot GenerateFinalAssembly( IActivityMonitor monitor, IStObjRuntimeBuilder runtimeBuilder, BuilderFinalAssemblyConfiguration config )
        {
            if( config == null ) throw new ArgumentNullException( "config" );
            return GenerateFinalAssembly( monitor, runtimeBuilder, config.DoNotGenerateFinalAssembly, config.Directory, config.AssemblyName, config.ExternalVersionStamp, config.SignAssembly, config.SignKeyPair );
        }

        /// <summary>
        /// Generates final assembly.
        /// This internally creates a <see cref="DynamicAssembly"/> that contains the compiled type mapper and all the automatically implemented objects.
        /// On success, the root object is instanciated (from the in memory dll and with the help of the <see cref="IStObjRuntimeBuilder"/>) and the dll is optionaly saved to disk. 
        /// If an error occured, null is returned and errors are logged into the monitor.
        /// </summary>
        /// <param name="monitor">Logger to use.</param>
        /// <param name="runtimeBuilder">Runtime builder to use to create final mapper object.</param>
        /// <param name="doNotGenerateFinalAssembly">See <see cref="BuilderFinalAssemblyConfiguration.DoNotGenerateFinalAssembly"/>.</param>
        /// <param name="directory">See <see cref="BuilderFinalAssemblyConfiguration.Directory"/>.</param>
        /// <param name="assemblyName">See <see cref="BuilderFinalAssemblyConfiguration.AssemblyName"/>.</param>
        /// <param name="externalVersionStamp">See <see cref="BuilderFinalAssemblyConfiguration.ExternalVersionStamp"/>.</param>
        /// <param name="signAssembly">See <see cref="BuilderFinalAssemblyConfiguration.SignAssembly"/>.</param>
        /// <param name="signKeyPair">See <see cref="BuilderFinalAssemblyConfiguration.SignKeyPair"/>.</param>
        /// <returns>The final objects map, null if any error occured (logged into <paramref name="monitor"/>).</returns>
        public StObjContextRoot GenerateFinalAssembly( 
            IActivityMonitor monitor, 
            IStObjRuntimeBuilder runtimeBuilder,
            bool doNotGenerateFinalAssembly, 
            string directory = null, 
            string assemblyName = null, 
            string externalVersionStamp = null, 
            bool signAssembly = false, 
            StrongNameKeyPair signKeyPair = null )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            if( HasFatalError ) throw new InvalidOperationException();
            try
            {
                if( doNotGenerateFinalAssembly ) directory = null;
                else if( String.IsNullOrEmpty( directory ) )
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

                DynamicAssembly a = new DynamicAssembly( directory, assemblyName, externalVersionStamp, signKeyPair, doNotGenerateFinalAssembly ? AssemblyBuilderAccess.Run : AssemblyBuilderAccess.RunAndSave );

                TypeBuilder root = a.ModuleBuilder.DefineType( StObjContextRoot.RootContextTypeName, TypeAttributes.Class | TypeAttributes.Sealed, typeof( StObjContextRoot ) );
                
                if( !FinalizeTypesCreationAndCreateCtor( monitor, a, root ) ) return null;
                // Concretize the actual StObjContextRoot type to detect any error before generating the embedded resource.
                Type stobjContectRootType = root.CreateType();

                RawOutStream outS = new RawOutStream();
                using( monitor.OpenTrace().Send( "Generating resource informations." ) )
                {
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
                    
                    long typeMappingSize = outS.Memory.Length;
                    monitor.Trace().Send( "Type mappings require {0} bytes in resource.", typeMappingSize );
                    
                    // Once Contexts are serialized, we serialize the values that have been injected during graph construction.
                    outS.Formatter.Serialize( outS.Memory, _buildValueCollector.Values.ToArray() );

                    long valuesSize = outS.Memory.Length - typeMappingSize;
                    monitor.Trace().Send( "Configured properties and parameter require {0} bytes in resource.", valuesSize );

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

                    long graphDescSize = outS.Memory.Length - valuesSize - typeMappingSize;
                    monitor.Trace().Send( "Graph description requires {0} bytes in resource.", graphDescSize );
                    
                }
                if( !doNotGenerateFinalAssembly )
                {
                    // Generates the Resource BLOB now.
                    outS.Memory.Position = 0;
                    a.ModuleBuilder.DefineManifestResource( StObjContextRoot.RootContextTypeName + ".Data", outS.Memory, ResourceAttributes.Private );
                    a.Save();
                }

                // Time to instanciate the final mapper.
                // Injects the resource stream explicitely: GetManifestResourceStream raises "The invoked member is not supported in a dynamic assembly." exception 
                // when called on a dynamic assembly.
                outS.Memory.Position = 0;
                return (StObjContextRoot)Activator.CreateInstance( stobjContectRootType, new object[] { monitor, runtimeBuilder ?? StObjContextRoot.DefaultStObjRuntimeBuilder, outS.Memory } );
            }
            catch( Exception ex )
            {
                monitor.Error().Send( ex, "While generating final assembly '{0}'.", assemblyName );
                return null;
            }
        }

        private bool FinalizeTypesCreationAndCreateCtor( IActivityMonitor monitor, DynamicAssembly a, TypeBuilder root )
        {
            int typeCreatedCount, typeErrorCount;
            using( monitor.OpenInfo().Send( "Generating dynamic types." ) )
            {
                FieldBuilder types = DefineTypeInitializer( monitor, a, root, out typeCreatedCount, out typeErrorCount );

                var baseCtor = root.BaseType.GetConstructor( BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof( IActivityMonitor ), typeof( IStObjRuntimeBuilder ), typeof( Type[] ), typeof( Stream ) }, null );
                Debug.Assert( baseCtor != null, "StObjContextRoot ctor signature is: ( IActivityMonitor monitor, IStObjRuntimeBuilder runtimeBuilder, Type[] allTypes, Stream resources = null )" );

                DefineConstructorWithStreamArgument( root, types, baseCtor );
                DefineConstructorWithoutStreamArgument( root, types, baseCtor );

                if( typeErrorCount > 0 ) monitor.CloseGroup( String.Format( "Failed to generate {0} types out of {1}.", typeErrorCount, typeCreatedCount ) );
                else monitor.CloseGroup( String.Format( "{0} types generated.", typeCreatedCount ) );
            }
            return typeErrorCount == 0;
        }

        FieldBuilder DefineTypeInitializer( IActivityMonitor monitor, DynamicAssembly a, TypeBuilder root, out int typeCreatedCount, out int typeErrorCount )
        {
            var types = root.DefineField( "_types", typeof( Type[] ), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly );
            var typeCtor = root.DefineTypeInitializer();
            var g = typeCtor.GetILGenerator();

            LocalBuilder allTypes = g.DeclareLocal( typeof( Type[] ) );
            g.LdInt32( _orderedStObjs.Count );
            g.Emit( OpCodes.Newarr, typeof( Type ) );
            g.StLoc( allTypes );

            MethodInfo typeFromToken = typeof( Type ).GetMethod( "GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public );

            typeCreatedCount = typeErrorCount = 0;
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
            g.LdLoc( allTypes );
            g.Emit( OpCodes.Stsfld, types );
            g.Emit( OpCodes.Ret );
            return types;
        }

        static void DefineConstructorWithStreamArgument( TypeBuilder root, FieldBuilder types, ConstructorInfo baseCtor )
        {
            var ctor = root.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof( IActivityMonitor ), typeof( IStObjRuntimeBuilder ), typeof( Stream ) } );
            var g = ctor.GetILGenerator();
            g.LdArg( 0 );
            g.LdArg( 1 );
            g.LdArg( 2 );
            g.Emit( OpCodes.Ldsfld, types );
            g.LdArg( 3 );
            g.Emit( OpCodes.Call, baseCtor );

            g.Emit( OpCodes.Ret );
        }

        static void DefineConstructorWithoutStreamArgument( TypeBuilder root, FieldBuilder types, ConstructorInfo baseCtor )
        {
            var ctor = root.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof( IActivityMonitor ), typeof( IStObjRuntimeBuilder ) } );
            var g = ctor.GetILGenerator();
            g.LdArg( 0 );
            g.LdArg( 1 );
            g.LdArg( 2 );
            g.Emit( OpCodes.Ldsfld, types );
            g.Emit( OpCodes.Ldnull );
            g.Emit( OpCodes.Call, baseCtor );

            g.Emit( OpCodes.Ret );
        }
    }
}
