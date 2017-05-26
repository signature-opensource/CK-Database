#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\AmbientContract\Collector\AmbientContractCollectorResult.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    public class AmbientContractCollectorResult<CT,T,TC> : MultiContextualResult<AmbientContractCollectorContextualResult<CT,T,TC>>
        where CT : AmbientContextualTypeMap<T,TC>
        where T : AmbientTypeInfo
        where TC : AmbientContextualTypeInfo<T,TC>
    {
        readonly AmbientTypeMap<CT> _mappings;

        internal AmbientContractCollectorResult( AmbientTypeMap<CT> mappings, IPocoSupportResult pocoSupport, Dictionary<Type, T> typeInfo, ISet<System.Reflection.Assembly> assemblies )
        {
            _mappings = mappings;
            PocoSupport = pocoSupport;
            Assemblies = assemblies;
        }

        /// <summary>
        /// Gets all the registered Poco information.
        /// </summary>
        public IPocoSupportResult PocoSupport { get; }

        /// <summary>
        /// Gets the set of asssemblies for which at least one type has been registered.
        /// </summary>
        public ISet<System.Reflection.Assembly> Assemblies { get; }

        public override bool HasFatalError => PocoSupport == null || base.HasFatalError;

        /// <summary>
        /// Logs detailed information about discovered ambient contracts for all discovered contexts.
        /// </summary>
        /// <param name="monitor">Logger (must not be null).</param>
        public void LogErrorAndWarnings( IActivityMonitor monitor )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            using( monitor.OpenTrace().Send( $"Ambient Contract discovering: {Contexts.Count} context(s)."  ) )
            {
                if( PocoSupport == null )
                {
                    monitor.Fatal().Send( $"Poco support failed!" );
                }
                Foreach( r => r.LogErrorAndWarnings( monitor ) );
            }
        }

        /// <summary>
        /// Gets the type mapper for the multiple existing contexts.
        /// </summary>
        public IContextualRoot<IContextualTypeMap> Mappings => _mappings; 

    }
}
