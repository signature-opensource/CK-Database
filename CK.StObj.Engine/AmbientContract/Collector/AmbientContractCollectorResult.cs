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
        //private AmbientTypeMap<CT> mappings;
        //private Dictionary<Type, T> _collector;

        internal AmbientContractCollectorResult( AmbientTypeMap<CT> mappings, IPocoSupportResult pocoSupport, Dictionary<Type, T> typeInfo )
        {
            _mappings = mappings;
            PocoSupport = pocoSupport;

            #region Unused for the moment
            // readonly Dictionary<Type,T> _typeInfo;
            // readonly IReadOnlyUniqueKeyedCollection<T, Type> _typeInfoEx;

            //      _typeInfo = typeInfo;
            //      _typeInfoEx = new ReadOnlyDictionaryOnDictionary<T, Type>( _typeInfo, a => a.Type );
            #endregion
        }

        #region Unused for the moment
        ///// <summary>
        ///// Gets an indexed collection of <typeparam name="T"/> that are <see cref="AmbientTypeInfo"/>: this provides a central 
        ///// mapping from ambient classes to any associated information.
        ///// </summary>
        //public IReadOnlyUniqueKeyedCollection<T, Type> TypeInformation
        //{
        //    get { return _typeInfoEx; }
        //}
        //
        //class ReadOnlyDictionaryOnDictionary<T, TKey> : IReadOnlyUniqueKeyedCollection<T, TKey>
        //{
        //    readonly Dictionary<TKey,T> _map;
        //    readonly Func<T,TKey> _keyer;

        //    public ReadOnlyDictionaryOnDictionary( Dictionary<TKey, T> map, Func<T, TKey> keySelector = null )
        //    {
        //        _map = map;
        //        _keyer = keySelector;
        //    }

        //    public bool Contains( TKey key )
        //    {
        //        return _map.ContainsKey( key );
        //    }

        //    public T GetByKey( TKey key, out bool exists )
        //    {
        //        T i;
        //        exists = _map.TryGetValue( key, out i );
        //        return i;
        //    }

        //    public bool Contains( object item )
        //    {
        //        if( item is TKey ) return _map.ContainsKey( (TKey)item );
        //        if( item is T )
        //        {
        //            if( _keyer != null ) return _map.ContainsKey( _keyer( (T)item ) );
        //            return _map.ContainsValue( (T)item );
        //        }
        //        return false;
        //    }

        //    public int Count
        //    {
        //        get { return _map.Count; }
        //    }

        //    public IEnumerator<T> GetEnumerator()
        //    {
        //        return _map.Values.GetEnumerator();
        //    }

        //    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        //    {
        //        return _map.Values.GetEnumerator();
        //    }
        //}
        #endregion

        /// <summary>
        /// Gets all the registered Poco information.
        /// </summary>
        public IPocoSupportResult PocoSupport { get; }

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
