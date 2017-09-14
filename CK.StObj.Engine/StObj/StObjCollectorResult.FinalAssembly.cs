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
    public partial class StObjCollectorResult
    {
        /// <summary>
        /// Generates final assembly must be called only when <see cref="BuilderFinalAssemblyConfiguration.GenerateFinalAssemblyOption"/>
        /// is not <see cref="BuilderFinalAssemblyConfiguration.GenerateOption.DoNotGenerateFile"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="callPEVrify">True to call PEVerify on the generated assembly.</param>
        /// <returns>False if any error occured (logged into <paramref name="monitor"/>).</returns>
        public bool GenerateFinalAssembly( IActivityMonitor monitor, bool callPEVrify, bool withIL, bool withSrc )
        {
            using( monitor.OpenInfo( "Generating StObj dynamic assembly." ) )
            {
                try
                {
#if NET461
                    if( _finalAssembly == null ) throw new InvalidOperationException( "Using GenerateOption.DoNotGenerateFile." );
                    if( withIL && !DoGenerateFinalAssembly( monitor, callPEVrify ) ) return false;
#endif
                    return withSrc ? GenerateSourceCode( monitor, true, withIL ) : true;
                }
                catch( Exception ex )
                {
                    monitor.Error( $"While generating final assembly '{_tempAssembly.SaveFileName}'.", ex );
                    return false;
                }
            }
        }

#if NET461

        bool DoGenerateFinalAssembly( IActivityMonitor monitor, bool callPEVrify )
        {
            TypeBuilder root = _finalAssembly.ModuleBuilder.DefineType( StObjContextRoot.RootContextTypeName, TypeAttributes.Class | TypeAttributes.Sealed, typeof( StObjContextRoot ) );

            if( !FinalizeTypesCreationAndCreateCtor( monitor, _finalAssembly, root ) ) return false;
            // Concretize the actual StObjContextRoot type to detect any error before generating the embedded resource.
            Type stobjContectRootType = root.CreateType();

            RawOutStream outS = new RawOutStream();
            using( monitor.OpenTrace( "Generating resource informations." ) )
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
                monitor.Trace( $"Type mappings require {typeMappingSize} bytes in resource." );

                // Once Contexts are serialized, we serialize the values that have been injected during graph construction.
                outS.Formatter.Write( _buildValueCollector.Values, _buildValueCollector.Values.Count );

                long valuesSize = outS.Memory.Length - typeMappingSize;
                monitor.Trace( $"Configured properties and parameter require {valuesSize} bytes in resource." );

                outS.Writer.Write( _totalSpecializationCount );
                foreach( var m in _orderedStObjs )
                {
                    outS.Writer.Write( m.Context.Context );
                    outS.Writer.Write( m.Generalization != null ? m.Generalization.IndexOrdered : -1 );
                    outS.Writer.Write( m.Specialization != null ? m.Specialization.IndexOrdered : -1 );
                    outS.Writer.Write( m.LeafSpecialization.IndexOrdered );
                    outS.Writer.Write( m.SpecializationIndexOrdered );

                    if( m.AmbientTypeInfo.StObjConstruct != null )
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
                monitor.Trace( $"Graph description requires {graphDescSize} bytes in resource." );

            }
            // Generates the Resource BLOB now.
            outS.Memory.Position = 0;
            _finalAssembly.ModuleBuilder.DefineManifestResource( StObjContextRoot.RootContextTypeName + ".Data", outS.Memory, ResourceAttributes.Private );
            _finalAssembly.Save();
            if( callPEVrify && !ExecutePEVerify( monitor ) ) return false;

            return true;
        }

        bool ExecutePEVerify(IActivityMonitor monitor)
        {
            monitor.OpenInfo( "PEVerify the generated assembly.");
            string directory = Path.GetDirectoryName(_finalAssembly.SaveFilePath);
            string peVerfiyPath = Path.Combine(directory, "PEVerify.exe");
            if (!File.Exists(peVerfiyPath))
            {
                using (monitor.OpenWarn( $"PEVerify.exe not found in directory '{directory}': extracting a self-embedded version." ))
                {
                    using (var source = Assembly.GetExecutingAssembly().GetManifestResourceStream("CK.StObj.Engine.PEVerify.PEVerify.exe"))
                    using (var target = File.Create(peVerfiyPath))
                    {
                        source.CopyTo(target);
                    }
                    using (var source = Assembly.GetExecutingAssembly().GetManifestResourceStream("CK.StObj.Engine.PEVerify.pevrfyrc.dll"))
                    using (var target = File.Create(Path.Combine(directory, "pevrfyrc.dll")))
                    {
                        source.CopyTo(target);
                    }
                }
            }
            var pInfo = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                Arguments = '"' + _finalAssembly.SaveFilePath + '"',
                FileName = peVerfiyPath,
                WorkingDirectory = directory
            };
            using (var pr = Process.Start(pInfo))
            {
                string output = pr.StandardOutput.ReadToEnd();
                if (pr.ExitCode == 0)
                {
                    monitor.Trace( output );
                }
                else
                {
                    monitor.Error( output );
                    monitor.CloseGroup( $"PEVerify.exe exited with code: {pr.ExitCode}." );
                    return false;
                }
            }
            return true;
        }

        bool FinalizeTypesCreationAndCreateCtor( IActivityMonitor monitor, DynamicAssembly a, TypeBuilder root )
        {
            int typeCreatedCount, typeErrorCount;
            using( monitor.OpenInfo( "Generating dynamic types." ) )
            {
                FieldBuilder types = DefineTypeInitializer( monitor, a, root, out typeCreatedCount, out typeErrorCount );

                DefineConstructorWithoutStreamArgument( root, types );

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

            MethodInfo typeFromToken = typeof( Type ).GetMethod( nameof( Type.GetTypeFromHandle ), BindingFlags.Static | BindingFlags.Public );

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
                    t = m.ObjectType;
                    Debug.Assert( t == m.AmbientTypeInfo.Type );
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

        static void DefineConstructorWithoutStreamArgument( TypeBuilder root, FieldBuilder types )
        {
            var baseCtor = root.BaseType.GetConstructor( BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof( IActivityMonitor ), typeof( IStObjRuntimeBuilder ), typeof( Type[] ) }, null );
            Debug.Assert( baseCtor != null, "StObjContextRoot ctor signature is: ( IActivityMonitor monitor, IStObjRuntimeBuilder runtimeBuilder, Type[] allTypes )" );

            var ctor = root.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof( IActivityMonitor ), typeof( IStObjRuntimeBuilder ) } );
            var g = ctor.GetILGenerator();
            g.LdArg( 0 );
            g.LdArg( 1 );
            g.LdArg( 2 );
            g.Emit( OpCodes.Ldsfld, types );
            g.Emit( OpCodes.Call, baseCtor );

            g.Emit( OpCodes.Ret );
        }
#endif
    }
}

