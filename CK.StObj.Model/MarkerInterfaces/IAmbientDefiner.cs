using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Marker interface for a "Definer" type. It acts as a "base type" for a <see cref="IAmbientService"/>
    /// or <see cref="IAmbientObject"/>: it is not itself an ambient type but its specializations
    /// are (unless they also supports this interface).
    /// </summary>
    /// <typeparam name="TDefiner">The type of the marked type itself (type of this: could have named it TThis).</typeparam>
    public interface IAmbientDefiner<TDefiner> where TDefiner : IAmbientDefiner<TDefiner>
    {
    }

}
