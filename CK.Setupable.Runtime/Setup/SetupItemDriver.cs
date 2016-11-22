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

        /// <summary>
        /// Gets the container driver (or null if the item does not belong to a container).
        /// </summary>
        public SetupItemDriver ContainerDriver => Engine.Drivers[SortedItem.Container?.Item];

        /// <summary>
        /// Gets all the container driver, starting with this <see cref="ContainerDriver"/> up to the 
        /// root container driver.
        /// </summary>
        public IEnumerable<SetupItemDriver> ContainerDrivers
        {
            get
            {
                var i = this;
                for( ;;)
                {
                    var c = ContainerDriver;
                    if( c == null ) break;
                    yield return c;
                    i = c;
                }
            }
        }

        internal override bool IsGroupHead => false; 

        /// <summary>
        /// Gets whether this <see cref="SetupItemDriver"/> is associated to a group or a container.
        /// </summary>
        public bool IsGroup => Head != null;

        /// <summary>
        /// Very first method called after all driver have been created.
        /// Any <see cref="ISetupItemDriverAware.OnDriverPreInitialized(SetupItemDriver)"/> on setup items
        /// are called right after.
        /// Does nothing by default (always return true).
        /// </summary>
        /// <returns>True on success, false to stop the process.</returns>
        internal protected virtual bool ExecutePreInit() => true;

        internal bool ExecuteHeadInit()
        {
            if( !Init( true ) || !OnStep( SetupCallGroupStep.Init, true ) ) return false;
            if( _handlers != null )
            {
                foreach( var h in _handlers )
                {
                    if( !h.Init( this ) || !h.OnStep( this, SetupCallGroupStep.Init ) ) return false;
                }
            }
            return Init( false ) && OnStep( SetupCallGroupStep.Init, false );
        }

        internal override bool ExecuteInit()
        {
            if( !IsGroup ) return ExecuteHeadInit();
            // If the item is not a Group or a Container, InitContent is not called.
            if( !InitContent( true ) || !OnStep( SetupCallGroupStep.InitContent, true ) ) return false;
            if( _handlers != null )
            {
                foreach( var h in _handlers )
                {
                    if( !h.InitContent( this ) || !h.OnStep( this, SetupCallGroupStep.InitContent ) ) return false;
                }
            }
            return InitContent( false ) && OnStep( SetupCallGroupStep.InitContent, false );
        }

        internal bool ExecuteHeadInstall()
        {
            if( !Install( true ) || !OnStep( SetupCallGroupStep.Install, true ) ) return false;
            if( _handlers != null )
            {
                foreach( var h in _handlers )
                {
                    if( !h.Install( this ) || !h.OnStep( this, SetupCallGroupStep.Install ) ) return false;
                }
            }
            return Install( false ) && OnStep( SetupCallGroupStep.Install, false );
        }

        internal override bool ExecuteInstall()
        {
            if( !IsGroup ) return ExecuteHeadInstall();
            // If the item is not a Group or a Container, InstallContent is not called.
            if( !InstallContent( true ) || !OnStep( SetupCallGroupStep.InstallContent, true ) ) return false;
            if( _handlers != null )
            {
                foreach( var h in _handlers )
                {
                    if( !h.InstallContent( this ) || !h.OnStep( this, SetupCallGroupStep.InstallContent ) ) return false;
                }
            }
            return InstallContent( false ) && OnStep( SetupCallGroupStep.InstallContent, false );
        }

        internal bool ExecuteHeadSettle()
        {
            if( !Settle( true ) || !OnStep( SetupCallGroupStep.Settle, true ) ) return false;
            if( _handlers != null )
            {
                foreach( var h in _handlers )
                {
                    if( !h.Settle( this ) || !h.OnStep( this, SetupCallGroupStep.Settle ) ) return false;
                }
            }
            return Settle( false ) && OnStep( SetupCallGroupStep.Settle, false );
        }

        internal override bool ExecuteSettle()
        {
            if( !IsGroup ) return ExecuteHeadSettle();
            // If the item is not a Group or a Container, SettleContent is not called.
            if( !SettleContent( true ) || !OnStep( SetupCallGroupStep.SettleContent, true ) ) return false;
            if( _handlers != null )
            {
                foreach( var h in _handlers )
                {
                    if( !h.SettleContent( this ) || !h.OnStep( this, SetupCallGroupStep.SettleContent ) ) return false;
                }
            }
            return SettleContent( false ) && OnStep( SetupCallGroupStep.SettleContent, false );
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
        internal protected virtual bool Init( bool beforeHandlers ) => true;

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
        protected virtual bool InitContent( bool beforeHandlers ) => true;

        /// <summary>
        /// Does nothing (always returns true).
        /// </summary>
        /// <param name="beforeHandlers">
        /// True when handlers associated to this driver have not been called yet.
        /// False when their <see cref="ISetupHandler.Install"/> method have been called.
        /// </param>
        /// <returns>Always true.</returns>
        internal protected virtual bool Install( bool beforeHandlers ) => true;

        /// <summary>
        /// Called, only if <see cref="IsGroup"/> is true, after <see cref="Install"/> (and <see cref="InstallContent"/> for groups 
        /// or containers) have been called on all the contained items.
        /// Does nothing (always returns true).
        /// </summary>
        /// <param name="beforeHandlers">
        /// True when handlers associated to this driver have not been called yet.
        /// False when their <see cref="ISetupHandler.InstallContent"/> method have been called.
        /// </param>
        protected virtual bool InstallContent( bool beforeHandlers ) => true;

        /// <summary>
        /// Does nothing (always returns true).
        /// </summary>
        /// <param name="beforeHandlers">
        /// True when handlers associated to this driver have not been called yet.
        /// False when their <see cref="ISetupHandler.Settle"/> method have been called.
        /// </param>
        /// <returns>Always true.</returns>
        internal protected virtual bool Settle( bool beforeHandlers ) => true;

        /// <summary>
        /// Called, only if <see cref="IsGroup"/> is true, after <see cref="Settle"/> (and <see cref="SettleContent"/> for groups 
        /// or containers) have been called on all the contained items.
        /// Does nothing (always returns true).
        /// </summary>
        /// <param name="beforeHandlers">
        /// True when handlers associated to this driver have not been called yet.
        /// False when their <see cref="ISetupHandler.SettleContent"/> method have been called.
        /// </param>
        protected virtual bool SettleContent( bool beforeHandlers ) => true;

        /// <summary>
        /// This method is called right after its corresponding dedicated method.
        /// This centralized step based method is easier to use then the different
        /// available overrides when the step actions are structurally the same and
        /// only their actual contents/data is step dependent.
        /// Does nothing (always returns true).
        /// </summary>
        /// <param name="beforeHandlers">
        /// True when handlers associated to this driver have not been called yet.
        /// False when their associated step method have been called.
        /// </param>
        /// <returns>Always true.</returns>
        protected virtual bool OnStep( SetupCallGroupStep step, bool beforeHandlers ) => true;

    }
}
