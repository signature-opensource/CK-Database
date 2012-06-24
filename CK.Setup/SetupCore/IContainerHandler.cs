using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface IContainerHandler
    {
        bool Init( ContainerDriver d );

        bool Install( ContainerDriver d );

        bool Settle( ContainerDriver d );
        
        bool InitContent( ContainerDriver d );

        bool InstallContent( ContainerDriver d );

        bool SettleContent( ContainerDriver d );
    }
}
