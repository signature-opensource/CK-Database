using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface IStObjExternalConfigurator
    {
        void Configure( IStObjMutableItem o );
    }
}
