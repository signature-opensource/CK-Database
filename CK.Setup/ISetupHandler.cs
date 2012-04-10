using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface ISetupHandler
    {
        bool Init( SetupDriver d );

        bool Install( SetupDriver d );

        bool Settle( SetupDriver d );
    }
}
