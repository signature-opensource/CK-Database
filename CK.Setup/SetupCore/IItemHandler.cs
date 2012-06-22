using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface IItemHandler
    {
        bool Init( ItemDriver d );

        bool Install( ItemDriver d );

        bool Settle( ItemDriver d );
    }
}
