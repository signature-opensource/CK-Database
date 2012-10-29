using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.Database
{
    public interface ISqlObjectBuilder
    {
        IDependentProtoItem Create( IActivityLogger logger, string text );
    }
}
