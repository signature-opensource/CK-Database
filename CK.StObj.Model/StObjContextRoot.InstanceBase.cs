#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObjContextRoot.InstanceBase.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection.Emit;
using System.Threading;
using System.Diagnostics;

namespace CK.Core
{
    public abstract partial class StObjContextRoot : IStObjMap
    {
        readonly StObjContext _defaultContext;
        readonly StObjContext[] _contexts;
        readonly IReadOnlyCollection<StObjContext> _contextsEx;
        readonly IStObjRuntimeBuilder _runtimeBuilder;

        internal readonly StructuredObjectCache SingletonCache;
        internal readonly IActivityMonitor Logger;
        internal readonly object[] BuilderValues;
        internal readonly StObj[] StObjs;
        internal readonly int SpecializationCount;

        /// <summary>
        /// Initializes a new <see cref="StObjContextRoot"/>. Dynamically generated concrete contexts use
        /// this during their load.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="runtimeBuilder">The object builder.</param>
        /// <param name="allTypes">All concrete types in the context.</param>
        protected StObjContextRoot( IActivityMonitor monitor, IStObjRuntimeBuilder runtimeBuilder, Type[] allTypes )
            : this( monitor, runtimeBuilder, allTypes, null )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="StObjContextRoot"/>. Dynamically generated concrete contexts use
        /// this during build (<paramref name="resources"/> is provided) or during load (the resources are extracted
        /// from the assembly itself).
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="runtimeBuilder">The object builder.</param>
        /// <param name="allTypes">All concrete types in the context.</param>
        /// <param name="resources">Resources stream when building.</param>
        protected StObjContextRoot( IActivityMonitor monitor, IStObjRuntimeBuilder runtimeBuilder, Type[] allTypes, Stream resources )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            if( runtimeBuilder == null ) throw new ArgumentNullException( "runtimeBuilder" );
            if( allTypes == null ) throw new ArgumentNullException( "allTypes" );
            Logger = monitor;
            _runtimeBuilder = runtimeBuilder;
            StObjs = new StObj[allTypes.Length];
            for( int i = 0; i < allTypes.Length; ++i )
            {
                StObjs[i] = new StObj( this, allTypes[i] );
            }
            // Resources stream is explicitly provided when instanciating objects from the dynamic assembly
            // since GetManifestResourceStream is NOT supported on a dynamic assembly...
            using( Stream s = resources ?? GetType().Assembly.GetManifestResourceStream( RootContextTypeName + ".Data" ) )
            {
                BinaryReader reader = new BinaryReader( s );

                _contexts = new StObjContext[reader.ReadInt32()];
                _contextsEx = new CKReadOnlyListOnIList<StObjContext>( _contexts );
                _defaultContext = ReadContexts( reader );

                BinaryFormatter formatter = new BinaryFormatter();
                BuilderValues = (object[])formatter.Deserialize( s );
                
                SpecializationCount = reader.ReadInt32();
                SingletonCache = new StructuredObjectCache( SpecializationCount );
                foreach( var o in StObjs )
                {
                    o.Initialize( reader );
                }
                // Singleton creation.
                foreach( var o in StObjs )
                {
                    object instance = SingletonCache.Get( o.CacheIndex );
                    if( instance == null ) SingletonCache.Set( o.CacheIndex, instance = _runtimeBuilder.CreateInstance( o.LeafSpecialization.ObjectType ) );
                    o.CallConstruct( monitor, idx => SingletonCache.Get( StObjs[idx].CacheIndex ), instance );
                }
                // Setting post build properties.
                foreach( var o in StObjs )
                {
                    if( o.Specialization == null )
                    {
                        object instance = SingletonCache.Get( o.CacheIndex );
                        o.SetPostBuilProperties( idx => SingletonCache.Get( StObjs[idx].CacheIndex ), instance );
                    }
                }
            }
        }

        private StObjContext ReadContexts( BinaryReader reader )
        {
            StObjContext def = null;
            for( int i = 0; i < _contexts.Length; ++i )
            {
                string name = reader.ReadString();

                int typeMappingCount = reader.ReadInt32();
                Dictionary<Type,int> mappings = new Dictionary<Type, int>( typeMappingCount );
                while( --typeMappingCount >= 0 )
                {
                    mappings.Add( SimpleTypeFinder.Default.ResolveType( reader.ReadString(), true ), reader.ReadInt32() );
                }
                _contexts[i] = new StObjContext( this, name, mappings );
                if( name.Length == 0 ) def = _contexts[i];
            }
            return def;
        }

        internal StObjContext DoFindContext( string context )
        {
            foreach( var c in _contexts )
            {
                if( c.Context == context ) return c;
            }
            return null;
        }

        /// <summary>
        /// Gets the default context.
        /// </summary>
        public IContextualStObjMap Default
        {
            get { return _defaultContext; }
        }

        /// <summary>
        /// Gets all the contexts.
        /// </summary>
        public IReadOnlyCollection<IContextualStObjMap> Contexts
        {
            get { return _contextsEx; }
        }

        /// <summary>
        /// Finds a context by its name.
        /// </summary>
        /// <param name="context">Name of the context.</param>
        /// <returns>A context or null.</returns>
        public IContextualStObjMap FindContext( string context )
        {
            return DoFindContext( context );
        }

        /// <summary>
        /// Gets all type to object mappings.
        /// </summary>
        public IEnumerable<StObjMapMapping> AllMappings
        {
            get { return _contexts.SelectMany( c => c.Types, (c, t) => new StObjMapMapping( t, c.Context, c.Obtain( t ) ) ); }
        }
    }
}
