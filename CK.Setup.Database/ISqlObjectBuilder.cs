using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup.Database
{
    public interface ISqlObjectBuilder
    {
        SqlObjectPreParse PreParse( IActivityLogger logger, string text );

        ISetupableItem Create( IActivityLogger logger, SqlObjectPreParse preParsed, SetupableItemData data );
    }
}
