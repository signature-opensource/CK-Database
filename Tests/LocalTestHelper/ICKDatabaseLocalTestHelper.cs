using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Testing;

namespace CK.Testing
{
    public interface ICKDatabaseLocalTestHelper : IMixinTestHelper, IDBSetupTestHelper, CKDatabaseLocal.ICKDatabaseLocalTestHelperCore
    {
    }
}
