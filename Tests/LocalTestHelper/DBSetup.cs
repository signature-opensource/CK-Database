using CK.Core;
using NUnit.Framework;
using System.Reflection;
using static CK.Testing.CKDatabaseLocalTestHelper;

namespace LocalTestHelper
{
    [TestFixture]
    public abstract class DBSetup : CK.DB.Tests.DBSetup
    {

        [Test]
        [Explicit]
        public void delete_netcore_published_folders()
        {
            TestHelper.LogToConsole = true;
            TestHelper.DeleteAllLocalComponentsPublishedFolders();
        }
    }
}
