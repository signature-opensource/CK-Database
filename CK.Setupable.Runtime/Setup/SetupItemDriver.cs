using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Generic driver for <see cref="IDependentItem"/> that also handles the composite <see cref="IDependentItemGroup"/>.
    /// </summary>
    public class SetupItemDriver : DriverBase
    {
        List<ISetupHandler> _handlers;
        internal readonly DriverBase Head;

        /// <summary>
        /// Encapsulates construction information for <see cref="SetupItemDriver"/> objects.
        /// This is an opaque parameter (except the <see cref="Engine"/> property) that enables the abstract 
        /// DriverBase to be correctly intialized.
        /// </summary>
        public sealed class BuildInfo
        {
            internal BuildInfo( ISetupEngine engine, ISortedItem<ISetupItem> sortedItem, VersionedName externalVersion )
            {               
                Head = null;
                Engine = engine;
                SortedItem = sortedItem;
                ExternalVersion = externalVersion;
            }

            internal BuildInfo( DriverBase head, ISortedItem<ISetupItem> sortedItem )
            {
                Head = head;
                Engine = head.Engine;
                ExternalVersion = head.ExternalVersion;
                SortedItem = sortedItem;
            }

            /// <summary>
            /// Gets the <see cref="ISetupEngine"/>.
            /// </summary>
            public ISetupEngine Engine { get; set; }

            internal readonly ISortedItem<ISetupItem> SortedItem;
            internal readonly VersionedName ExternalVersion;
            internal readonly DriverBase Head;
        }

        /// <summary>
        /// Initializes a new <see cref="SetupItemDriver"/>.
        /// </summary>
        /// <param name="info">Opaque parameter built by the framework.</param>
        public SetupItemDriver( BuildInfo info )
            : base( info.Engine, info.SortedItem, info.ExternalVersion )
        {
            Debug.Assert( info.Head == null || info.SortedItem.FullName + ".Head" == info.Head.FullName );
            Head = info.Head;
        }

        internal override bool IsGroupHead => false; 

        /// <summary>
        /// Gets whether this <see cref="SetupItemDriver"/> is associated to a group or a container.
        /// </summary>
        public bool IsGroup => Head != null; 

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
            if( !Init( true ) ) return false;
            if( _handlers != null )
            {
                for( int i = 0; i < _handlers.Count; ++i )
                {
                    if( !_handlers[i].Init( this ) ) return false;
                }
            }
            return Init( false );
        }

        internal override bool ExecuteInit()
        {
            if( !IsGroup ) return ExecuteHeadInit();
            // If the item is not a Group or a Container, InitContent is not called.
            if( !InitContent( true ) ) return false;
            if( _handlers != null )
            {
                for( int i = 0; i < _handlers.Count; ++i )
                {
                    if( !_handlers[i].InitContent( this ) ) return false;
                }
            }
            return InitContent( false );
        }

        internal bool ExecuteHeadInstall()
        {
            if( !Install( true ) ) return false;
            if( _handlers != null )
            {
                for( int i = 0; i < _handlers.Count; ++i )
                {
                    if( !_handlers[i].Install( this ) ) return false;
                }
            }
            return Install( false );
        }

        internal override bool ExecuteInstall()
        {
            if( !IsGroup ) return ExecuteHeadInstall();
            // If the item is not a Group or a Container, InstallContent is not called.
            if( !InstallContent( true ) ) return false;
            if( _handlers != null )
            {
                for( int i = 0; i < _handlers.Count; ++i )
                {
                    if( !_handlers[i].InstallContent( this ) ) return false;
                }
            }
            return InstallContent( false );
        }

        internal bool ExecuteHeadSettle()
        {
            if( !Settle( true ) ) return false;
            if( _handlers != null )
            {
                for( int i = 0; i < _handlers.Count; ++i )
                {
                    if( !_handlers[i].Settle( this ) ) return false;
                }
            }
            return Settle( false );
        }

        internal override bool ExecuteSettle()
        {
            if( !IsGroup ) return ExecuteHeadSettle();
            // If the item is not a Group or a Container, SettleContent is not called.
            if( !SettleContent( true ) ) return false;
            if( _handlers != null )
            {
                for( int i = 0; i < _handlers.Count; ++i )
                {
                    if( !_handlers[i].SettleContent( this ) ) return false;
                }
            }
            return SettleContent( false );
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

        public void AddInitHandler( Func<SetupItemDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.Init ) );
        }

        public void AddInitContentHandler( Func<SetupItemDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.InitContent ) );
        }

        public void AddInstallHandler( Func<SetupItemDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.Install ) );
        }

        public void AddInstallContentHandler( Func<SetupItemDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.InstallContent ) );
        }

        public void AddSettleHandler( Func<SetupItemDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.Settle ) );
        }

        public void AddSettleContentHandler( Func<SetupItemDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.SettleContent ) );
        }

        #endregion

        /// <summary>
        /// Does nothing (always returns true).
        /// </summary>
        /// <param name="beforeHandlers">
        /// True when handlers associated to this driver have not been called yet.
        /// False when their <see cref="ISetupHandler.Init"/> method have been called.
        /// </param>
        /// <returns>Always true.</returns>
        internal protected virtual bool Init( bool beforeHandlers )
        {
            return true;
        }

        /// <summary>
        /// Called, only if <see cref="IsGroup"/> is true, after <see cref="Init"/> (and <see cref="InitContent"/> for groups 
        /// or containers) have been called on all the contained items.
        /// Does nothing (always returns true).
        /// </summary>
        /// <param name="beforeHandlers">
        /// True when handlers associated to this driver have not been called yet.
        /// False when their <see cref="ISetupHandler.InitContent"/> method have been called.
        /// </param>
        /// <returns>Always true.</returns>
        protected virtual bool InitContent( bool beforeHandlers )
        {
            return true;
        }

        /// <summary>
        /// Does nothing (always returns true).
        /// </summary>
        /// <param name="beforeHandlers">
        /// True when handlers associated to this driver have not been called yet.
        /// False when their <see cref="ISetupHandler.Install"/> method have been called.
        /// </param>
        /// <returns>Always true.</returns>
        internal protected virtual bool Install( bool beforeHandlers )
        {
            return true;
        }

        /// <summary>
        /// Called, only if <see cref="IsGroup"/> is true, after <see cref="Install"/> (and <see cref="InstallContent"/> for groups 
        /// or containers) have been called on all the contained items.
        /// Does nothing (always returns true).
        /// </summary>
        /// <param name="beforeHandlers">
        /// True when handlers associated to this driver have not been called yet.
        /// False when their <see cref="ISetupHandler.InstallContent"/> method have been called.
        /// </param>
        protected virtual bool InstallContent( bool beforeHandlers )
        {
            return true;
        }

        /// <summary>
        /// Does nothing (always returns true).
        /// </summary>
        /// <param name="beforeHandlers">
        /// True when handlers associated to this driver have not been called yet.
        /// False when their <see cref="ISetupHandler.Settle"/> method have been called.
        /// </param>
        /// <returns>Always true.</returns>
        internal protected virtual bool Settle( bool beforeHandlers )
        {
            return true;
        }

        /// <summary>
        /// Called, only if <see cref="IsGroup"/> is true, after <see cref="Settle"/> (and <see cref="SettleContent"/> for groups 
        /// or containers) have been called on all the contained items.
        /// Does nothing (always returns true).
        /// </summary>
        /// <param name="beforeHandlers">
        /// True when handlers associated to this driver have not been called yet.
        /// False when their <see cref="ISetupHandler.SettleContent"/> method have been called.
        /// </param>
        protected virtual bool SettleContent( bool beforeHandlers )
        {
            return true;
        }
    }
}
