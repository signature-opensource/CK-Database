using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Driver for <see cref="IDependentItemContainer"/>.
    /// </summary>
    public class ContainerDriver : DriverBase
    {
        List<IContainerHandler> _handlers;

        public class BuildInfo
        {
            internal BuildInfo( ContainerHeadDriver head, ISortedItem sortedItem )
            {
                Head = head;
                SortedItem = sortedItem;
            }

            internal readonly ContainerHeadDriver Head;
            internal readonly ISortedItem SortedItem;
        }

        public ContainerDriver( BuildInfo info )
            : base( info.Head.Engine, info.SortedItem, info.Head.ExternalVersion, info.Head.DirectDependencies )
        {
            Debug.Assert( info.SortedItem.IsContainer && info.SortedItem.FullName + ".Head" == info.Head.FullName );
            Head = info.Head;
        }

        /// <summary>
        /// Gets the container to setup.
        /// </summary>
        public new IDependentItemContainer Item
        {
            get { return (IDependentItemContainer)base.Item; }
        }

        public override bool IsContainerHead
        {
            get { return false; }
        }

        public readonly ContainerHeadDriver Head;

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

        public void AddHandler( IContainerHandler handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            if( _handlers == null ) _handlers = new List<IContainerHandler>();
            _handlers.Add( handler );
        }

        public void AddInitHandler( Func<ContainerDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new ContainerHandlerFuncAdapter( handler, SetupCallContainerStep.Init ) );
        }

        public void AddInitContentHandler( Func<ContainerDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new ContainerHandlerFuncAdapter( handler, SetupCallContainerStep.InitContent ) );
        }

        public void AddInstallHandler( Func<ContainerDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new ContainerHandlerFuncAdapter( handler, SetupCallContainerStep.Install ) );
        }

        public void AddInstallContentHandler( Func<ContainerDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new ContainerHandlerFuncAdapter( handler, SetupCallContainerStep.InstallContent ) );
        }

        public void AddSettleHandler( Func<ContainerDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new ContainerHandlerFuncAdapter( handler, SetupCallContainerStep.Settle ) );
        }

        public void AddSettleContentHandler( Func<ContainerDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new ContainerHandlerFuncAdapter( handler, SetupCallContainerStep.SettleContent ) );
        }

        #endregion

        internal protected virtual bool Init()
        {
            return true;
        }

        protected virtual bool InitContent()
        {
            return true;
        }

        internal protected virtual bool Install()
        {
            return true;
        }

        protected virtual bool InstallContent()
        {
            return true;
        }

        internal protected virtual bool Settle()
        {
            return true;
        }

        protected virtual bool SettleContent()
        {
            return true;
        }
    }
}
