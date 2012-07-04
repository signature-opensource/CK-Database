using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace CK.Core
{
    public class AmbiantContractCollectorResult : IReadOnlyCollection<AmbiantContractCollectorContextResult>
    {
        ListDictionary _contextResults;

        internal AmbiantContractCollectorResult()
        {
            _contextResults = new ListDictionary();
        }

        public int Count
        {
            get { return _contextResults.Count; }
        }

        public AmbiantContractCollectorContextResult Default
        {
            get { return (AmbiantContractCollectorContextResult)_contextResults[typeof(AmbiantTypeMapper)]; }
        }

        public AmbiantContractCollectorContextResult this[Type context]
        {
            get { return (AmbiantContractCollectorContextResult)_contextResults[context ?? typeof(AmbiantTypeMapper)]; }
        }

        public IEnumerator<AmbiantContractCollectorContextResult> GetEnumerator()
        {
            return _contextResults.Values.Cast<AmbiantContractCollectorContextResult>().GetEnumerator();
        }
        
        public bool CheckErrorAndWarnings( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            bool result = true;
            using( logger.OpenGroup( LogLevel.Trace, "Ambiant Contract discovering: {0} context(s).", _contextResults.Count ) )
            {
                AmbiantContractCollectorContextResult cDef = Default;
                if( cDef != null ) result &= cDef.CheckErrorAndWarnings( logger );
                foreach( AmbiantContractCollectorContextResult c in _contextResults.Values )
                {
                    if( c.Context != null ) result &= c.CheckErrorAndWarnings( logger );
                }
            }
            return result;
        }

        internal void Add( AmbiantContractCollectorContextResult c )
        {
            _contextResults.Add( c.Context ?? typeof(AmbiantTypeMapper), c );
        }

        bool IReadOnlyCollection<AmbiantContractCollectorContextResult>.Contains( object item )
        {
            AmbiantContractCollectorContextResult c = item as AmbiantContractCollectorContextResult;
            return c != null ? _contextResults.Contains( c.Context ?? typeof(AmbiantTypeMapper) ) : false;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _contextResults.Values.GetEnumerator();
        }

    }
}
