using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface ISetupHandlerContainer
    {
        bool Init( SetupDriverContainer d );

        bool Install( SetupDriverContainer d );

        bool Settle( SetupDriverContainer d );
        
        bool InitContent( SetupDriverContainer d );

        bool InstallContent( SetupDriverContainer d );

        bool SettleContent( SetupDriverContainer d );
    }
}
