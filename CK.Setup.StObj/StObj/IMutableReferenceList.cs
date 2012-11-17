using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public interface IMutableReferenceList : IReadOnlyList<IMutableReference>
    {
        IMutableReference AddNew( Type t, string context = null );
    }

}
