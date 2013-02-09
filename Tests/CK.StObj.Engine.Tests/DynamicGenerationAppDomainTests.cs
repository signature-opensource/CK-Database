using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    public class DynamicGenerationAppDomainTests
    {
        [Test]
        public void CommonAncestor()
        {
            string di1 = @"C:\Test\toto\titi\truc";
            string di2 = @"C:\Test\toto\titi\bidule";
            string di3 = @"C:\Test\toto\titi";

            var result = StObjContextRoot.FindCommonAncestor( new []{ di1, di2, di3 } );
            Assert.That( result, Is.EqualTo( @"C:\Test\toto\titi" ) );

            string di4 = @"C:\Test\toto\tata";
            result = StObjContextRoot.FindCommonAncestor( new []{ di1, di2, di3, di4 } );
            Assert.That( result, Is.EqualTo( @"C:\Test\toto" ) );

            string di5 = @"C:\Test\tata";
            result = StObjContextRoot.FindCommonAncestor( new []{ di1, di2, di3, di4, di5 } );
            Assert.That( result, Is.EqualTo( @"C:\Test" ) );

            string di6 = @"D:\Test\tata";
            result = StObjContextRoot.FindCommonAncestor( new []{ di1, di2, di3, di4, di5, di6 } );
            Assert.That( result, Is.Null );
        }

        [Test]
        public void BuildInCurrentAppDomain()
        {
            AppDomain other = null;
            try
            {
                other = AppDomain.CreateDomain( "Test" );
                other.DoCallBack( ProcessInCurrentAppDomain );
            }
            finally
            {
                AppDomain.Unload( other );
            }
        }

        static void ProcessInCurrentAppDomain()
        {
            var localTestDir = Path.Combine( TestHelper.TempFolder, "BuildInCurrentAppDomain" );
            try
            {
                string outputDir = Path.Combine( localTestDir, "Output" );
                if( !Directory.Exists( outputDir ) ) Directory.CreateDirectory( outputDir );

                File.Delete( Path.Combine( TestHelper.BinFolder, "AutoGenTestObjBuilder.dll" ) );
                GenerateAutoGenTestObjBuilderDll( TestHelper.BinFolder );

                // Cleanup any previous run traces.
                File.Delete( Path.Combine( outputDir, "MyLittleAssembly.dll" ) );

                var config = new StObjEngineConfigurationTest();
                config.AppDomainConfiguration.UseIndependentAppDomain = false;
                config.FinalAssemblyConfiguration.Directory = outputDir;
                config.FinalAssemblyConfiguration.AssemblyName = "MyLittleAssembly";
                config.AppDomainConfiguration.Assemblies.DiscoverAssemblyNames.Add( "AutoGenTestObjBuilder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" );

                using( StObjBuildResult result = StObjContextRoot.Build( config, TestHelper.Logger ) )
                {
                    Assert.That( result.Success, Is.True, "Build succeed..." );
                    Assert.That( result.IndependentAppDomain, Is.Null, "...in this app domain." );
                    Assert.That( File.Exists( Path.Combine( outputDir, "MyLittleAssembly.dll" ) ), Is.True, "Build generated the dll." );
                }
            }
            finally
            {
                Directory.Delete( localTestDir, true );
            }
        }

        [Test]
        public void GenerateDllTests()
        {
            string binDir = TestHelper.BinFolder;
            string inputDir = Path.Combine( binDir, "Input" );
            try
            {
                GenerateAutoGenTestObjBuilderDll( inputDir );
                Assert.That( File.Exists( Path.Combine( inputDir, "AutoGenTestObjBuilder.dll" ) ), Is.True, "Dll has been generated" );
                Assert.That( AppDomain.CurrentDomain.GetAssemblies().Any( x => x.FullName.Contains( "AutoGenTestObjBuilder" ) ), Is.False );

                AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;
                setup.ApplicationBase = inputDir;
                setup.PrivateBinPathProbe = "*";
                setup.PrivateBinPath = inputDir;
                var appdomain = AppDomain.CreateDomain( "StObjContextRoot.Build.IndependentAppDomain", null, setup );

                File.Copy( Path.Combine( binDir, "CK.StObj.Engine.Tests.dll" ), Path.Combine( inputDir, "CK.StObj.Engine.Tests.dll" ) );

                var o = (AppDomainAnalyzer)Activator.CreateInstance( appdomain, typeof( AppDomainAnalyzer ).Assembly.FullName, typeof( AppDomainAnalyzer ).FullName ).Unwrap();
                o.LoadAssembly( "AutoGenTestObjBuilder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" );
                var assembly = o.GetAllAssemblyNames();

                Assert.That( AppDomain.CurrentDomain.GetAssemblies().Where( x => x.FullName.Contains( "AutoGenTestObjBuilder" ) ).Count(), Is.EqualTo( 0 ) );
                Assert.That( assembly.Where( x => x.Contains( "AutoGenTestObjBuilder" ) ).Count(), Is.EqualTo( 1 ) );

            }
            finally
            {
                Directory.Delete( inputDir, true );
            }
        }

        [Test]
        public void BuildWithIndependentAppDomain()
        {
            ProcessInIndependentAppDomain();
            //AppDomain other = null;
            //try
            //{
            //    other = AppDomain.CreateDomain( "Test" );
            //    other.DoCallBack( ProcessInIndependentAppDomain );
            //}
            //finally
            //{
            //    AppDomain.Unload( other );
            //}
        }

        static private void ProcessInIndependentAppDomain()
        {
            var localTestDir = Path.Combine( TestHelper.TempFolder, "BuildWithIndependentAppDomain" );
            try
            {
                string inputDir = Path.Combine( localTestDir, "Input" );
                GenerateAutoGenTestObjBuilderDll( inputDir );
                Assert.That( File.Exists( Path.Combine( inputDir, "AutoGenTestObjBuilder.dll" ) ), Is.True, "Test input dll must heve been generated." );

                string outputDir = Path.Combine( localTestDir, "Output" );
                if( !Directory.Exists( outputDir ) ) Directory.CreateDirectory( outputDir );

                // Cleanup any previous run traces.
                File.Delete( Path.Combine( outputDir, "MyLittleAssembly.dll" ) );

                var config = new StObjEngineConfigurationTest();
                config.AppDomainConfiguration.UseIndependentAppDomain = true;
                config.FinalAssemblyConfiguration.Directory = outputDir;
                config.FinalAssemblyConfiguration.AssemblyName = "MyLittleAssembly";

                config.AppDomainConfiguration.ProbePaths.Add( inputDir );
                config.AppDomainConfiguration.ProbePaths.Add( TestHelper.BinFolder );
                config.AppDomainConfiguration.Assemblies.DiscoverAssemblyNames.Add( "AutoGenTestObjBuilder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" );

                using( StObjBuildResult result = StObjContextRoot.Build( config, TestHelper.Logger ) )
                {
                    Assert.That( result.Success, Is.True, "Build succeed..." );
                    Assert.That( result.IndependentAppDomain, Is.Not.Null.And.Not.EqualTo( AppDomain.CurrentDomain ), "...in an independant AppDomain." );
                    Assert.That( result.IndependentAppDomain.BaseDirectory, Is.EqualTo( Directory.GetParent( TestHelper.TempFolder ).FullName ) );

                    Assert.That( File.Exists( Path.Combine( outputDir, "MyLittleAssembly.dll" ) ), Is.True, "Build generated the dll." );

                    var analyser = (AppDomainAnalyzer)result.IndependentAppDomain.CreateInstanceAndUnwrap( typeof( AppDomainAnalyzer ).Assembly.FullName, typeof( AppDomainAnalyzer ).FullName );
                    string[] assemblies = analyser.GetAllAssemblyNames();
                    Assert.That( assemblies.Any( x => x.Contains( "AutoGenTestObjBuilder" ) ), Is.True );
                    Assert.That( AppDomain.CurrentDomain.GetAssemblies().Any( x => x.FullName.Contains( "AutoGenTestObjBuilder" ) ), Is.False, "AutoGenTestObjBuilder has not been loaded in this application domain." );
                }
            }
            finally
            {
                Directory.Delete( localTestDir, true );
            }
        }

        static void GenerateAutoGenTestObjBuilderDll( string dir )
        {           
            CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateExecutable = false;
            parameters.OutputAssembly = Path.Combine( dir, "AutoGenTestObjBuilder.dll" );
            parameters.ReferencedAssemblies.Add( "CK.Core.dll" );
            parameters.ReferencedAssemblies.Add( "CK.Reflection.dll" );
            parameters.ReferencedAssemblies.Add( "CK.StObj.Model.dll" );
            if( !Directory.Exists( dir ) ) Directory.CreateDirectory( dir );

            DirectoryInfo d = new DirectoryInfo( dir );

            Assert.That( d.Exists );
            if( !File.Exists( Path.Combine( d.FullName, "CK.Core.dll" ) ) )
            {
                File.Copy( Path.Combine( TestHelper.BinFolder, @"CK.Core.dll" ), Path.Combine( d.FullName, "CK.Core.dll" ), true );
            }
            if( !File.Exists( Path.Combine( d.FullName, "CK.Reflection.dll" ) ) )
            {
                File.Copy( Path.Combine( TestHelper.BinFolder, @"CK.Reflection.dll" ), Path.Combine( d.FullName, "CK.Reflection.dll" ), true );
            }
            if( !File.Exists( Path.Combine( d.FullName, "CK.StObj.Model.dll" ) ) )
            {
                File.Copy( Path.Combine( TestHelper.BinFolder, @"CK.StObj.Model.dll" ), Path.Combine( d.FullName, "CK.StObj.Model.dll" ), true );
            }

            CompilerResults r = CodeDomProvider.CreateProvider( "C#" ).CompileAssemblyFromSource( parameters,
                @"  using System;
                    using System.Reflection;
                    using CK.Core;
                    using CK.Setup;
                    
                    namespace CK.StObj.Engine.Tests
                    {
                        public class AutoGenTestObjBuilder
                        {
                            class AutoImplementedAttribute : Attribute, IAutoImplementorMethod
                            {
                                public bool Implement( IActivityLogger logger, System.Reflection.MethodInfo m, System.Reflection.Emit.TypeBuilder b, bool isVirtual )
                                {
                                    CK.Reflection.EmitHelper.ImplementEmptyStubMethod( b, m, isVirtual );
                                    return true;
                                }
                            }

                            public class A : IAmbientContract
                            {
                            }

                            public abstract class B : A
                            {
                                [AutoImplemented]
                                public abstract int Auto( int i );
                            }

                            public interface IC : IAmbientContract
                            {
                                A TheA { get; }
                            }

                            public class C : IC
                            {
                                [AmbientContract]
                                public A TheA { get; private set; }
                            }

                            public class D : C
                            {
                                [AmbientProperty( IsOptional = true )]
                                public string AnOptionalString { get; private set; }
                            }
                        }
                    }"
            );
            foreach( CompilerError item in r.Errors ) Console.WriteLine( item.ErrorText );
            Assert.That( r.Errors.Count, Is.EqualTo( 0 ) );
            Assert.That( File.Exists( r.PathToAssembly ) );
        }

        public class AppDomainAnalyzer : MarshalByRefObject
        {
            public void LoadAssembly( string assembly )
            {
                AppDomain.CurrentDomain.Load( assembly );
            }

            public string[] GetAllAssemblyNames()
            {
                return AppDomain.CurrentDomain.GetAssemblies().Select( x => x.FullName ).ToArray();
            }
        }

        [Serializable]
        public class StObjEngineConfigurationTest : IStObjEngineConfiguration
        {
            public StObjEngineConfigurationTest()
            {
                AppDomainConfiguration = new BuilderAppDomainConfiguration();
                FinalAssemblyConfiguration = new BuilderFinalAssemblyConfiguration();
            }

            public string BuilderAssemblyQualifiedName
            {
                get { return "CK.Setup.BasicStObjBuilder, CK.StObj.Engine"; }
            }

            public BuilderAppDomainConfiguration AppDomainConfiguration { get; private set; }

            public BuilderFinalAssemblyConfiguration FinalAssemblyConfiguration { get; private set; }

            public Action<IActivityLogger,BasicStObjBuilder> RunHook { get; set; }

            public void BuildRunHook( IActivityLogger logger, BasicStObjBuilder config )
            {
                AppDomain.CurrentDomain.Load( "AutoGenTestObjBuilder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" );
            }
        }
    }
}
