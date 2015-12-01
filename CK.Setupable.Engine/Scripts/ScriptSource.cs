#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\Scripts\ScriptSource.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Identifies a source for scripts (any script must expose its originating source with <see cref="ISetupScript.ScriptSource"/>).
    /// A source must be <see cref="ScriptTypeHandler.RegisterSource">registered</see> in a <see cref="ScriptTypeHandler"/> and
    /// is registered with an <see cref="Index"/>: if more than one script share the same <see cref="ISetupScript.Name"/>, the one that 
    /// is executed is the one with the greater source's index.
    /// </summary>
    public class ScriptSource
    {
        readonly int _index;
        readonly string _name;
        readonly ScriptTypeHandler _handler;

        internal ScriptSource( ScriptTypeHandler h, string name )
        {
            _handler = h;
            _index = h.Sources.Count;
            _name = name;
        }

        internal ScriptTypeHandler Handler
        {
            get { return _handler; }
        }

        /// <summary>
        /// Gets the priority of this source among other sources from the same <see cref="ScriptTypeHandler"/>.
        /// Larger the index is, the higher the priority source.
        /// </summary>
        public int Index
        {
            get { return _index; }
        }

        /// <summary>
        /// Gets the name that uniquely identifies this source in its <see cref="ScriptTypeHandler"/> <see cref="ScriptTypeManager"/>.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

    }
}
