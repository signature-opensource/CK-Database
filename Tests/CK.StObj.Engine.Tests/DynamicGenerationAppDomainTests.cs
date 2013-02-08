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

            var result = StObjContextRoot.FindCommonAncestor( new List<string> { di1, di2, di3 } );
            Assert.That( result, Is.EqualTo( @"C:\Test\toto\titi" ) );

            string di4 = @"C:\Test\toto\tata";
            result = StObjContextRoot.FindCommonAncestor( new List<string> { di1, di2, di3, di4 } );
            Assert.That( result, Is.EqualTo( @"C:\Test\toto" ) );

            string di5 = @"C:\Test\tata";
            result = StObjContextRoot.FindCommonAncestor( new List<string> { di1, di2, di3, di4, di5 } );
            Assert.That( result, Is.EqualTo( @"C:\Test" ) );

            string di6 = @"D:\Test\tata";
            result = StObjContextRoot.FindCommonAncestor( new List<string> { di1, di2, di3, di4, di5, di6 } );
            Assert.That( result, Is.Null );
        }

        [Test]
        public void BuildInCurrentAppDomain()
        {
            var localTestDir = Path.Combine( TestHelper.TempFolder, "CK.StObj.Engine.Tests.Build" );
            if( !Directory.Exists( localTestDir ) ) Directory.CreateDirectory( localTestDir );

            try
            {
                using( StringWriter sw = new StringWriter() )
                {
                    ((IDefaultActivityLogger)TestHelper.Logger).Register( new ActivityLoggerTextWriterSink( sw ) );
                    StObjBuilderAppDomainConfiguration appDomainConfig = new StObjBuilderAppDomainConfiguration();
                    appDomainConfig.UseIndependentAppDomain = false;

                    StObjFinalAssemblyConfiguration assemblyConfig = new StObjFinalAssemblyConfiguration();
                    assemblyConfig.Directory = Path.Combine( localTestDir, "Output" );
                    assemblyConfig.AssemblyName = "MyLittleAssembly";
                    if( !Directory.Exists( assemblyConfig.Directory ) ) Directory.CreateDirectory( assemblyConfig.Directory );

                    var config = new StObjEngineConfigurationTest( appDomainConfig, assemblyConfig );

                    AppDomain createdAppDomain = null;
                    Assert.That( StObjContextRoot.Build( config, TestHelper.Logger, x => createdAppDomain = x ), Is.True, "Build process must return true with UseIndependentAppDomain = false;" );

                    Assert.That( File.Exists( Path.Combine( assemblyConfig.Directory, "MyLittleAssembly.dll" ) ), Is.True );
                    Assert.That( createdAppDomain, Is.Null );

                    File.Delete( Path.Combine( assemblyConfig.Directory, "MyLittleAssembly.dll" ) );

                    Assert.That( sw.ToString(), Is.Not.Empty );
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
            //Inject current test dll folder
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            // Code base is like "file:///C:/Users/Spi/Documents/Dev4/CK-Database/Output/Tests/Debug/CK.Setup.SqlServer.Tests.DLL"
            if( !codeBase.StartsWith( "file:///" ) )
                throw new ApplicationException( "Code base must start with file:/// protocol." );
            codeBase = codeBase.Substring( 8 ).Replace( '/', System.IO.Path.DirectorySeparatorChar );

            string binDir = new DirectoryInfo( codeBase ).Parent.FullName;

            string inputDir = Path.Combine( binDir, "Input" );

            try
            {
                GenerateDll( inputDir );
                Assert.That( File.Exists( Path.Combine( inputDir, "AutoGenTestObjBuilder.dll" ) ), Is.True, "Test input dll must be generated" );
                Assert.That( AppDomain.CurrentDomain.GetAssemblies().Where( x => x.FullName.Contains( "AutoGenTestObjBuilder" ) ).Count(), Is.EqualTo( 0 ) );

                AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;
                setup.ApplicationBase = inputDir;
                setup.PrivateBinPathProbe = "*";
                setup.PrivateBinPath = inputDir;
                var appdomain = AppDomain.CreateDomain( "StObjContextRoot.Build.IndependentAppDomain", null, setup );

                File.Copy( Path.Combine( binDir, "CK.StObj.Engine.Tests.dll" ), Path.Combine( inputDir, "CK.StObj.Engine.Tests.dll" ) );

                // EXCEPTION //
                Assert.Throws( typeof( FileNotFoundException ), () => appdomain.Load( "AutoGenTestObjBuilder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" ) );
                // EXCEPTION //

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
            var localTestDir = Path.Combine( TestHelper.TempFolder, "CK.StObj.Engine.Tests.Build" );
            if( !Directory.Exists( localTestDir ) ) Directory.CreateDirectory( localTestDir );

            try
            {
                StObjBuilderAppDomainConfiguration appDomainConfig = new StObjBuilderAppDomainConfiguration();
                appDomainConfig.UseIndependentAppDomain = true;

                StObjFinalAssemblyConfiguration assemblyConfig = new StObjFinalAssemblyConfiguration();
                assemblyConfig.Directory = Path.Combine( localTestDir, "Output" );
                assemblyConfig.AssemblyName = "MyLittleAssembly";
                if( !Directory.Exists( assemblyConfig.Directory ) ) Directory.CreateDirectory( assemblyConfig.Directory );

                var config = new StObjEngineConfigurationTest( appDomainConfig, assemblyConfig );

                string inputDir = Path.Combine( localTestDir, "Input" );
                GenerateDll( inputDir );
                Assert.That( File.Exists( Path.Combine( inputDir, "AutoGenTestObjBuilder.dll" ) ), Is.True, "Test input dll must be generated" );

                appDomainConfig.ProbePaths.Add( inputDir );
                appDomainConfig.ProbePaths.Add( TestHelper.BinFolder );

                config.RunHook = config.BuildRunHook;

                using( StringWriter sw = new StringWriter() )
                {
                    ((IDefaultActivityLogger)TestHelper.Logger).Register( new ActivityLoggerTextWriterSink( sw ) );
                    AppDomain createdAppDomain = null;
                    Assert.That( StObjContextRoot.Build( config, TestHelper.Logger, x => createdAppDomain = x ), Is.True, "Build process must return true with UseIndependentAppDomain = true;" );
                    Assert.That( File.Exists( Path.Combine( assemblyConfig.Directory, "MyLittleAssembly.dll" ) ), Is.True, "Build must generate dll" );

                    Assert.That( createdAppDomain, Is.Not.Null );
                    Assert.That( createdAppDomain, Is.Not.EqualTo( AppDomain.CurrentDomain ) );

                    var o = (AppDomainAnalyzer)Activator.CreateInstance( createdAppDomain, typeof( AppDomainAnalyzer ).Assembly.FullName, typeof( AppDomainAnalyzer ).FullName ).Unwrap();
                    var assembly = o.GetAllAssemblyNames();
                    Assert.That( assembly.Where( x => x.Contains( "AutoGenTestObjBuilder" ) ).Count(), Is.GreaterThan( 0 ) );
                    Assert.That( AppDomain.CurrentDomain.GetAssemblies().Where( x => x.FullName.Contains( "AutoGenTestObjBuilder" ) ).Count(), Is.EqualTo( 0 ) );

                    Assert.That( createdAppDomain.BaseDirectory, Is.EqualTo( Directory.GetParent( TestHelper.TempFolder ).FullName ) );

                    foreach( var item in appDomainConfig.ProbePaths )
                    {
                        Assert.That( createdAppDomain.SetupInformation.PrivateBinPath, Is.StringContaining( item ) );
                    }

                    Assert.That( sw.ToString(), Is.Not.Empty );
                }
            }
            finally
            {
                Directory.Delete( localTestDir, true );
            }
        }

        void GenerateDll( string inputDir )
        {
            CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateExecutable = false;
            parameters.OutputAssembly = Path.Combine( inputDir, "AutoGenTestObjBuilder.dll" );
            parameters.ReferencedAssemblies.Add( "CK.Core.dll" );
            parameters.ReferencedAssemblies.Add( "CK.Reflection.dll" );
            parameters.ReferencedAssemblies.Add( "CK.StObj.Model.dll" );
            if( !Directory.Exists( inputDir ) ) Directory.CreateDirectory( inputDir );

            DirectoryInfo dinfo = Directory.CreateDirectory( inputDir );
            Assert.That( dinfo.Exists );
            if( !File.Exists( dinfo.FullName + "\\CK.Core.dll" ) )
                File.Copy( @"D:\Dev\Dev4\ck-database\Output\Tests\Debug\CK.Core.dll", dinfo.FullName + "\\CK.Core.dll", true );
            if( !File.Exists( dinfo.FullName + "\\CK.Reflection.dll" ) )
                File.Copy( @"D:\Dev\Dev4\ck-database\Output\Tests\Debug\CK.Reflection.dll", dinfo.FullName + "\\CK.Reflection.dll", true );
            if( !File.Exists( dinfo.FullName + "\\CK.StObj.Model.dll" ) )
                File.Copy( @"D:\Dev\Dev4\ck-database\Output\Tests\Debug\CK.StObj.Model.dll", dinfo.FullName + "\\CK.StObj.Model.dll", true );

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
            foreach( CompilerError item in r.Errors )
            {
                Console.WriteLine( item.ErrorText );
            }
            Assert.That( r.Errors.Count, Is.EqualTo( 0 ) );
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
            internal StObjEngineConfigurationTest( StObjBuilderAppDomainConfiguration appDomainConfig, StObjFinalAssemblyConfiguration assemblyConfig )
            {
                StObjBuilderAppDomainConfiguration = appDomainConfig;
                StObjFinalAssemblyConfiguration = assemblyConfig;
            }

            public string BuilderAssemblyQualifiedName
            {
                get { return typeof( StObjEngineBuilderTest ).AssemblyQualifiedName; }
            }

            public StObjBuilderAppDomainConfiguration StObjBuilderAppDomainConfiguration { get; private set; }

            public StObjFinalAssemblyConfiguration StObjFinalAssemblyConfiguration { get; private set; }

            public Action<StObjEngineBuilderTest> RunHook { get; set; }

            public void BuildRunHook( StObjEngineBuilderTest test )
            {
                AppDomain.CurrentDomain.Load( "AutoGenTestObjBuilder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" );
            }
        }

        public class StObjEngineBuilderTest : IStObjBuilder
        {
            internal IActivityLogger Logger;
            internal StObjEngineConfigurationTest Config;

            public StObjEngineBuilderTest( IActivityLogger logger, IStObjEngineConfiguration engineConfig )
            {
                Logger = logger;
                Config = (StObjEngineConfigurationTest)engineConfig;
            }

            public void Run()
            {
                if( Config.RunHook != null ) Config.RunHook( this );
                StObjCollector collector = new StObjCollector( Logger );
                collector.RegisterClass( typeof( TestObjBuilder.B ) );
                collector.RegisterClass( typeof( TestObjBuilder.D ) );
                collector.DependencySorterHookInput = TestHelper.Trace;
                collector.DependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );
                var r = collector.GetResult();
                Assert.That( r.HasFatalError, Is.False );
                // Null as directory => use CK.StObj.Model folder.
                r.GenerateFinalAssembly( Logger, Config.StObjFinalAssemblyConfiguration );
            }
        }


        public class TestObjBuilder
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
    }
}
