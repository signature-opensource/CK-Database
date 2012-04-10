using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using Microsoft.Practices.Unity;

namespace CK.Setup.Database.SqlServer.Tests
{
    [TestFixture]
    public class FileBasedDBSetup
    {
        [Test]
        public void InstallFromScratch()
        {
            var logs = new StringImpl();
            IActivityLogger logger = new DefaultActivityLogger().Register( logs );

            DatabaseExecutor db = new DatabaseExecutor( "Server=.;Database=Test;Integrated Security=SSPI;", logger );
            PackageScriptCollector scripts = new PackageScriptCollector();
            FileDiscoverer discoverer = new FileDiscoverer( new SqlObjectBuilder(), logger );
            SqlVersionedItemRepository versions = new SqlVersionedItemRepository( db );

            UnityContainer container = new UnityContainer();
            container.RegisterInstance<IActivityLogger>( logger );
            container.RegisterInstance<IDatabaseExecutor>( db );

            SetupCenter center = new SetupCenter( versions, logger, new UnitySetupDriverFactory( container ) );
            ScriptHandlerBuilder scriptHandlerBuilder = new ScriptHandlerBuilder( center, scripts, db );

            discoverer.DiscoverPackages( TestHelper.GetScriptsFolder( "InstallFromScratch" ) );
            discoverer.DiscoverSqlFiles( TestHelper.GetScriptsFolder( "InstallFromScratch" ), scripts );

            Assert.That( center.State, Is.EqualTo( SetupCenterState.None ) );
            center.Register( discoverer );
            Assert.That( center.Drivers["Test.sOneStoredProcedure"], Is.Not.Null );

            Assert.That( center.State, Is.EqualTo( SetupCenterState.Registered ) );
            center.RunInit();
            Assert.That( center.State, Is.EqualTo( SetupCenterState.Initialized ) );
            center.RunInstall();
            Assert.That( center.State, Is.EqualTo( SetupCenterState.Installed ) );
            center.RunSettle();
            Assert.That( center.State, Is.EqualTo( SetupCenterState.Settled ) );

            Console.WriteLine( logs.GetText() );
        }

        [Test]
        public void InstallMKS()
        {
            var logs = new StringImpl();
            IActivityLogger logger = new DefaultActivityLogger().Register(logs);

            DatabaseExecutor db = new DatabaseExecutor("Server=.;Database=MKSM;Integrated Security=SSPI;", logger);
            PackageScriptCollector scripts = new PackageScriptCollector();
            FileDiscoverer discoverer = new FileDiscoverer(new SqlObjectBuilder(), logger);
            SqlVersionedItemRepository versions = new SqlVersionedItemRepository(db);

            UnityContainer container = new UnityContainer();
            container.RegisterInstance<IActivityLogger>(logger);
            container.RegisterInstance<IDatabaseExecutor>(db);

            SetupCenter center = new SetupCenter(versions, logger, new UnitySetupDriverFactory(container));
            ScriptHandlerBuilder scriptHandlerBuilder = new ScriptHandlerBuilder(center, scripts, db);

            discoverer.DiscoverPackages(TestHelper.GetMKSScriptsFolder(""));
            discoverer.DiscoverSqlFiles(TestHelper.GetMKSScriptsFolder(""), scripts);

            Assert.That(center.State, Is.EqualTo(SetupCenterState.None));
            center.Register(discoverer);

            Assert.That(center.State, Is.EqualTo(SetupCenterState.Registered));
            center.RunInit();
            Assert.That(center.State, Is.EqualTo(SetupCenterState.Initialized));
            center.RunInstall();
            Assert.That(center.State, Is.EqualTo(SetupCenterState.Installed));
            center.RunSettle();
            Assert.That(center.State, Is.EqualTo(SetupCenterState.Settled));

            Console.WriteLine(logs.GetText());
        }
    }
}
