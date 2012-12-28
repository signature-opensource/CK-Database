using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using CK.Setup;

namespace CK.Database.Setup
{
    public interface ISqlObjectParser
    {
        IDependentProtoItem Create( IActivityLogger logger, IContextLocNaming externalName, string text );
    }
}
