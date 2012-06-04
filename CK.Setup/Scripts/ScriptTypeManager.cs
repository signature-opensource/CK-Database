using System;
using System.Collections.Generic;
using System.Linq;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{

    public class ScriptTypeManager
    {
        Dictionary<string,IScriptTypeHandler> _handlers;
        IReadOnlyList<IScriptTypeHandler> _sortedHandlers;

        public ScriptTypeManager()
        {
            _handlers = new Dictionary<string, IScriptTypeHandler>();
        }

        /// <summary>
        /// Registers a new <see cref="IScriptTypeHandler"/>. Its <see cref="IScriptTypeHandler.ScriptType"/> must not 
        /// be already registered otherwise an exception is thrown.
        /// </summary>
        /// <param name="handler">The handler to register. Must not be null.</param>
        public void Register( IScriptTypeHandler handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            if( String.IsNullOrWhiteSpace( handler.ScriptType ) ) throw new ArgumentException( "ScriptType can not be empty.", "handler" );
            _handlers.Add( handler.ScriptType, handler );
        }

        public IScriptTypeHandler Find( string scriptType )
        {
            return _handlers.GetValueWithDefault( scriptType, null );
        }

        public bool IsRegistered( string scriptType )
        {
            return _handlers.ContainsKey( scriptType );
        }

        class DependencyWrapper : IDependentItem
        {
            public readonly IScriptTypeHandler Handler;

            public DependencyWrapper( IScriptTypeHandler h )
            {
                Handler = h;
            }

            public IDependentItemContainerRef Container
            {
                get { return null; }
            }

            public IEnumerable<string> Requires
            {
                get { return Handler.Requires; }
            }

            public IEnumerable<string> RequiredBy
            {
                get { return Handler.RequiredBy; }
            }

            public string FullName
            {
                get { return Handler.ScriptType; }
            }

            public bool Optional
            {
                get { return false; }
            }
        }

        /// <summary>
        /// Gets the list of handlers sorted according to the Requires/RequiredBy constraints.
        /// Null if the list can not be obtained.
        /// </summary>
        /// <param name="logger">Logger to use. Any error will be logged.</param>
        /// <returns>Null on error or the sorted list.</returns>
        public IReadOnlyList<IScriptTypeHandler> GetSortedHandlers( IActivityLogger logger )
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
