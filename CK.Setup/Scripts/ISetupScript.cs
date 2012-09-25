using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// A <see cref="ParsedFileName"/> associated to a way to 
    /// read its content and a script type.
    /// </summary>
    /// <remarks>
    /// The <see cref="ScriptSource"/> drives the choice of the <see cref="IScriptTypeHandler"/> that will be used
    /// to execute the script.
    /// </remarks>
    public interface ISetupScript
    {
        ParsedFileName Name { get; }

        string ScriptSource { get; }

        string GetScript();
    }
}
