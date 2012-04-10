using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup.Database
{
    /// <summary>
    /// A <see cref="ParsedFileName"/> associated to a way to 
    /// read its content and a script type.
    /// </summary>
    public interface ISetupScript
    {
        ParsedFileName Name { get; }

        string ScriptType { get; }

        string GetScript();
    }
}
