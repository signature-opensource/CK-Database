using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{

    public class ScriptTypeManager
    {
        readonly Dictionary<string,ScriptTypeHandler> _handlers;
        readonly Dictionary<string,ScriptSource> _sources;
        IReadOnlyList<ScriptTypeHandler> _sortedHandlers;

        public ScriptTypeManager()
        {
            _handlers = new Dictionary<string, ScriptTypeHandler>();
            _sources = new Dictionary<string, ScriptSource>();
        }

        /// <summary>
        /// Registers a new <see cref="ScriptTypeHandler"/>. Its <see cref="IScriptTypeHandler.ScriptType"/> must not 
        /// be already registered otherwise an exception is thrown.
        /// </summary>
        /// <param name="handler">The handler to register. Must not be null.</param>
        public void Register( ScriptTypeHandler handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            _handlers.Add( handler.HandlerName, handler );
            handler.SetScriptTypeManager( this );
        }

        public ScriptTypeHandler Find( string handlerName )
        {
            return _handlers.GetValueWithDefault( handlerName, null );
        }

        public bool IsRegistered( string handlerName )
        {
            return _handlers.ContainsKey( handlerName );
        }

        internal ScriptSource FindSourceByName( string sourceName )
        {
            return _sources.GetValueWithDefault( sourceName, null );
        }

        internal ScriptSource RegisterSource( ScriptSource s )
        {
            ScriptSource existing;
            if( !_sources.TryGetValue( s.Name, out existing ) ) _sources.Add( s.Name, s );
            return existing;
        }

        class DependencyWrapper : IDependentItem
        {
            public readonly ScriptTypeHandler Handler;

            public DependencyWrapper( ScriptTypeHandler h )
            {
                Handler = h;
            }

            public IDependentItemContainerRef Container
            {
                get { return null; }
            }

            public IEnumerable<IDependentItemRef> Requires
            {
                get { return Handler.InternalRequires != null ? Handler.InternalRequires.Select( s => new NamedDependentItemRef( s ) ) : null; }
            }

            public IEnumerable<IDependentItemRef> RequiredBy
            {
                get { return Handler.InternalRequiredBy != null ? Handler.InternalRequiredBy.Select( s => new NamedDependentItemRef( s ) ) : null; }
            }

            public string FullName
            {
                get { return Handler.HandlerName; }
            }

            public bool Optional
            {
                get { return false; }
            }

            public object StartDependencySort()
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the list of handlers sorted according to the Requires/RequiredBy constraints.
        /// Null if the list can not be obtained.
        /// </summary>
        /// <param name="logger">Logger to use. Any error will be logged.</param>
        /// <returns>Null on error or the sorted list.</returns>
        internal IReadOnlyList<ScriptTypeHandler> GetSortedHandlers( IActivityLogger logger )
        {
            if( _sortedHandlers == null )
            {
                DependencySorterResult r = DependencySorter.OrderItems( _handlers.Values.Select( h => new DependencyWrapper( h ) ), null );
                if( r.IsComplete )
                {
                    _sortedHandlers = r.SortedItems.Select( o => ((DependencyWrapper)o.Item).Handler ).ToReadOnlyList();
                }
                else
                {
                    _sortedHandlers = null;
                    if( r.CycleDetected != null )
                    {
                        logger.Fatal( "Dependency cycle between Script type handlers: " + r.CycleExplainedString );
                    }
                    else
                    {
                        string explain = r.RequiredMissingDependenciesExplained;
                        Debug.Assert( explain.Length > 0, "It can be only a missing dependency since no container and no homonyms exist." );
                        logger.Fatal( "Script type handler(s) required: " + explain );
                    }
                }
            }
            return _sortedHandlers;
        }

    }
}
