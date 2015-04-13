#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\Scripts\ScriptTypeHandler.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;
using System.Collections.Generic;
using System;

namespace CK.Setup
{
    public abstract class ScriptTypeHandler
    {
        readonly string _name;
        readonly List<ScriptSource> _sources;
        readonly IReadOnlyList<ScriptSource> _sourcesEx;
        ScriptTypeManager _manager;
        List<string> _requires;
        List<string> _requiredBy;

        public ScriptTypeHandler()
        {
            _name = GetType().Name;
            if( !_name.EndsWith( "ScriptTypeHandler", System.StringComparison.Ordinal ) )
            {
                throw new CKException( "A ScriptTypeHandler class name must end with 'ScriptTypeHandler'." );
            }
            _name = _name.Remove( _name.Length - 17 );
            _sources = new List<ScriptSource>();
            _sourcesEx = new CKReadOnlyListOnIList<ScriptSource>( _sources );
        }

        /// <summary>
        /// Gets the type of scripts that this script handler manages.
        /// It is automatically computed from the name of the class: for "SqlScriptTypeHandler", the HandlerName is "Sql".
        /// </summary>
        public string HandlerName { get { return _name; } }

        /// <summary>
        /// Gets the <see cref="ScriptSource"/> that this <see cref="ScriptTypeHandler"/> handles.
        /// Script sources are ordered by priority in ascending order.
        /// </summary>
        public IReadOnlyList<ScriptSource> Sources
        {
            get { return _sourcesEx; }
        }

        /// <summary>
        /// Registers a new <see cref="ScriptSource"/> (its name must be unique).
        /// Last registered source takes precedence over previously registered sources.
        /// </summary>
        /// <param name="name">Name must be not null, empty, white space and must not already exists.</param>
        /// <returns>The created source object.</returns>
        public ScriptSource RegisterSource( string name )
        {
            if( string.IsNullOrWhiteSpace( name ) ) throw new ArgumentException( "name" );
            if( _sources.Exists( e => StringComparer.InvariantCultureIgnoreCase.Equals( e.Name, name ) ) ) 
            {
                throw new InvalidOperationException( String.Format( "Source '{0}' already exists in '{1}'.", name, _name ) );
            }
            var s = new ScriptSource( this, name );
            _sources.Add( s );
            if( _manager != null ) RegisterSourceInManager( s );
            return s;
        }

        /// <summary>
        /// Creates the object that will be in charge of script execution.
        /// </summary>
        /// <param name="_monitor">Monitor to use.</param>
        /// <param name="driver">
        /// The item driver for which an executor must be created. 
        /// It can be ignored by an implementation that may choose, for instance, to use the same object regardless of the specific object
        /// to setup but based on some other properties.
        /// </param>
        /// <returns>A <see cref="IScriptExecutor"/> object that should be <see cref="ReleaseExecutor">released</see> once useless.</returns>
        protected internal abstract IScriptExecutor CreateExecutor( IActivityMonitor monitor, GenericItemSetupDriver driver );

        /// <summary>
        /// Called by the framework to indicate that a <see cref="IScriptExecutor"/> is no longer needed.
        /// </summary>
        /// <param name="_monitor">Monitor to use.</param>
        /// <param name="executor">The useless executor.</param>
        protected internal abstract void ReleaseExecutor( IActivityMonitor monitor, IScriptExecutor executor );

        /// <summary>
        /// Gets script type names that must be executed before this one. 
        /// Use '?' prefix to specify that the handler is not required (like "?Sql").
        /// </summary>
        public List<string> Requires { get { return _requires ?? (_requires = new List<string>()); } }

        /// <summary>
        /// Gets names of revert dependencies: scripts for this handler 
        /// will be executed before scripts for them. 
        /// </summary>
        public List<string> RequiredBy { get { return _requiredBy ?? (_requiredBy = new List<string>()); } }

        internal List<string> InternalRequires { get { return _requires; } }
        internal List<string> InternalRequiredBy { get { return _requiredBy; } }
        internal void SetScriptTypeManager( ScriptTypeManager m )
        {
            if( _manager != null ) throw new InvalidOperationException( "ScriptTypeHandler is already registered in a ScriptTypeManager." );
            _manager = m;
            foreach( var source in _sources ) RegisterSourceInManager( source );
        }

        private void RegisterSourceInManager( ScriptSource source )
        {
            var existing = _manager.RegisterSource( source );
            if( existing != null )
            {
                throw new InvalidOperationException( String.Format( "ScriptSource '{0}' can not be registered in '{1}'. It is already registered in handler '{2}'.", source.Name, HandlerName, existing.Handler.HandlerName ) );
            }
        }
    }
}
