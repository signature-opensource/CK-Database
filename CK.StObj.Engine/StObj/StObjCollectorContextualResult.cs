#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\StObjCollectorContextualResult.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Encapsulates information for one context in <see cref="StObjCollectorResult"/>.
    /// </summary>
    public class StObjCollectorContextualResult : IContextualResult
    {
        readonly AmbientContractCollectorContextualResult<StObjContextualMapper,StObjTypeInfo,MutableItem> _contractResult;
        readonly internal MutableItem[] _specializations;
        bool _fatalError;

        internal StObjCollectorContextualResult( AmbientContractCollectorContextualResult<StObjContextualMapper,StObjTypeInfo, MutableItem> contractResult )
        {
            _contractResult = contractResult;
            _specializations = new MutableItem[_contractResult.ConcreteClasses.Count];
        }

        /// <summary>
        /// Gets the context name. <see cref="String.Empty"/> for the default context.
        /// </summary>
        public string Context
        {
            get { return _contractResult.Context; }
        }

        /// <summary>
        /// Gets whether this result can be used or not.
        /// </summary>
        public bool HasFatalError
        {
            get { return _fatalError || _contractResult.HasFatalError; }
        }

        /// <summary>
        /// Gets the <see cref="IContextualStObjMapRuntime"/> that exposes structured objects.
        /// </summary>
        public IContextualStObjMapRuntime StObjMap
        {
            get { return (IContextualStObjMapRuntime)_contractResult.Mappings; }
        }

        /// <summary>
        /// Gets the <see cref="IContextualStObjMap"/> that exposes structured objects.
        /// </summary>
        internal StObjContextualMapper InternalMapper
        {
            get { return _contractResult.Mappings; }
        }

        internal AmbientContractCollectorContextualResult<StObjContextualMapper,StObjTypeInfo, MutableItem> AmbientContractResult
        {
            get { return _contractResult; }
        }

        internal void SetFatal()
        {
            _fatalError = true;
        }

    }
}
