#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\SetupDriver.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Generic driver for <see cref="IDependentItem"/> (also handles the composite <see cref="IDependentItemGroup"/>).
    /// </summary>
    public class DependentItemSetupDriver : DriverBase
    {
        List<ISetupHandler> _handlers;
        internal readonly DriverBase Head;

        /// <summary>
        /// Encapsulates construction information for <see cref="DependentItemSetupDriver"/> objects.
        /// This is an opaque parameter that enables the abstract base SetupDriver to be correclty intialized.
        /// </summary>
        public class BuildInfo
        {
            internal BuildInfo( ISetupEngine engine, ISortedItem sortedItem, VersionedName externalVersion )
            {               
                Head = null;
                Engine = engine;
                SortedItem = sortedItem;
                ExternalVersion = externalVersion;
            }

            internal BuildInfo( DriverBase head, ISortedItem sortedItem )
            {
                Head = head;
                Engine = head.Engine;
                ExternalVersion = head.ExternalVersion;
                SortedItem = sortedItem;
            }

            internal readonly ISetupEngine Engine;
            internal readonly ISortedItem SortedItem;
            internal readonly VersionedName ExternalVersion;
            internal readonly DriverBase Head;
        }

        /// <summary>
        /// Initializes a new <see cref="DependentItemSetupDriver"/>.
        /// </summary>
        /// <param name="info">Opaque parameter built by the framework.</param>
        public DependentItemSetupDriver( BuildInfo info )
            : base( info.Engine, info.SortedItem, info.ExternalVersion, info.Head != null ? info.Head.DirectDependencies : null )
        {
            Debug.Assert( info.Head == null || info.SortedItem.FullName + ".Head" == info.Head.FullName );
            Head = info.Head;
        }

        internal override bool IsGroupHead
        {
            get { return false; }
        }

        /// <summary>
        /// Gets whether this <see cref="DependentItemSetupDriver"/> is associated to a group or a container.
        /// </summary>
        public bool IsGroup 
        { 
            get { return Head != null; } 
        }

        /// <summary>
        /// Provides a way for this driver to load scripts (<see cref="ISetupScript"/> abstraction) from any storage 
        /// and to register them in the given <see cref="IScriptCollector"/>.
        /// </summary>
        /// <param name="scripts">Collector for scripts.</param>
        /// <returns>True on success. False when an error occured that must stop the setup process.</returns>
        protected internal virtual bool LoadScripts( IScriptCollector scripts )
        {
            return true;
        }

        internal bool ExecuteHeadInit()
        {
            if( !Init() ) return false;
            if( _handlers != null )
            {
                for( int i = 0; i < _handlers.Count; ++i )
                {
                    if( !_handlers[i].Init( this ) ) return false;
                }
            }
            return true;
        }

        internal override bool ExecuteInit()
        {
            if( !IsGroup && !ExecuteHeadInit() ) return false;
            if( !InitContent() ) return false;
            if( _handlers != null )
            {
                for( int i = 0; i < _handlers.Count; ++i )
                {
                    if( !_handlers[i].InitContent( this ) ) return false;
                }
            }
            return true;
        }

        internal bool ExecuteHeadInstall()
        {
            if( !Install() ) return false;
            if( _handlers != null )
            {
                for( int i = 0; i < _handlers.Count; ++i )
                {
                    if( !_handlers[i].Install( this ) ) return false;
                }
            }
            return true;
        }

        internal override bool ExecuteInstall()
        {
            if( !IsGroup && !ExecuteHeadInstall() ) return false;
            if( !InstallContent() ) return false;
            if( _handlers != null )
            {
                for( int i = 0; i < _handlers.Count; ++i )
                {
                    if( !_handlers[i].InstallContent( this ) ) return false;
                }
            }
            return true;
        }

        internal bool ExecuteHeadSettle()
        {
            if( !Settle() ) return false;
            if( _handlers != null )
            {
                for( int i = 0; i < _handlers.Count; ++i )
                {
                    if( !_handlers[i].Settle( this ) ) return false;
                }
            }
            return true;
        }

        internal override bool ExecuteSettle()
        {
            if( !IsGroup && !ExecuteHeadSettle() ) return false;
            if( !SettleContent() ) return false;
            if( _handlers != null )
            {
                for( int i = 0; i < _handlers.Count; ++i )
                {
                    if( !_handlers[i].SettleContent( this ) ) return false;
                }
            }
            return true;
        }

        #region Handler management

        /// <summary>
        /// Adds a <see cref="ISetupHandler"/> in the chain of handlers.
        /// Can be called during any setup phasis (typically in the <see cref="SetupStep.Init"/> phasis): the new handler 
        /// will be appended to the the handlers queue and will be called normally.
        /// </summary>
        /// <param name="handler">The handler to append.</param>
        public void AddHandler( ISetupHandler handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            if( _handlers == null ) _handlers = new List<ISetupHandler>();
            _handlers.Add( handler );
        }

        public void AddInitHandler( Func<DependentItemSetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.Init ) );
        }

        public void AddInitContentHandler( Func<DependentItemSetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.InitContent ) );
        }

        public void AddInstallHandler( Func<DependentItemSetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.Install ) );
        }

        public void AddInstallContentHandler( Func<DependentItemSetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.InstallContent ) );
        }

        public void AddSettleHandler( Func<DependentItemSetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.Settle ) );
        }

        public void AddSettleContentHandler( Func<DependentItemSetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.SettleContent ) );
        }

        #endregion

        /// <summary>
        /// Does nothing (always returns true).
        /// </summary>
        /// <returns>Always true.</returns>
        internal protected virtual bool Init()
        {
            return true;
        }

        /// <summary>
        /// Does nothing (always returns true).
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool InitContent()
        {
            return true;
        }

        /// <summary>
        /// Does nothing (always returns true).
        /// </summary>
        /// <returns>Always true.</returns>
        internal protected virtual bool Install()
        {
            return true;
        }

        /// <summary>
        /// Does nothing (always returns true).
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool InstallContent()
        {
            return true;
        }

        /// <summary>
        /// Does nothing (always returns true).
        /// </summary>
        /// <returns>Always true.</returns>
        internal protected virtual bool Settle()
        {
            return true;
        }

        /// <summary>
        /// Does nothing (always returns true).
        /// </summary>
        /// <returns>Always true.</returns>
        protected virtual bool SettleContent()
        {
            return true;
        }
    }
}
