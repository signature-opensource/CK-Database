#if NET461
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;

namespace CK.Core
{
    partial class AppSettings
    {
        void DoDefaultInitialize()
        {
            _initializedGetObject = _getObject = key => ConfigurationManager.AppSettings[key];
        }
    }
}
#endif
