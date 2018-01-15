using NUnit.Framework;
using static CK.Testing.CKDatabaseLocalTestHelper;

namespace LocalTestHelper
{
    [TestFixture]
    public class DBSetup : CK.DB.Tests.DBSetup
    {

        [Explicit]
        [Test]
        public void delete_netcore_published_folders()
        {
            TestHelper.LogToConsole = true;
            TestHelper.DeleteAllLocalComponentsPublishedFolders();
        }
    }
}
