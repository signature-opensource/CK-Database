using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    public class SetupDriverContainer : SetupDriverBase
    {
        List<ISetupHandlerContainer> _handlers;

        public class BuildInfo
        {
            internal BuildInfo( SetupDriverHead head, ISortedItem sortedItem )
            {
                Head = head;
                SortedItem = sortedItem;
            }

            internal readonly SetupDriverHead Head;
            internal readonly ISortedItem SortedItem;
        }

        public SetupDriverContainer( BuildInfo info )
            : base( info.Head.SetupCenter, info.SortedItem, info.Head.ExternalVersion, info.Head.DirectDependencies )
        {
            Debug.Assert( info.SortedItem.IsContainer && info.SortedItem.FullName + ".Head" == info.Head.FullName );
            Head = info.Head;
        }

        public override bool IsContainerHead
        {
            get { return false; }
        }

        public readonly SetupDriverHead Head;

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

        public void AddHandler( ISetupHandlerContainer handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            if( _handlers == null ) _handlers = new List<ISetupHandlerContainer>();
            _handlers.Add( handler );
        }

        public void AddInitHandler( Func<SetupDriverContainer, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerContainerAdapter( handler, SetupCallContainerStep.Init ) );
        }

        public void AddInitContentHandler( Func<SetupDriverContainer, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerContainerAdapter( handler, SetupCallContainerStep.InitContent ) );
        }

        public void AddInstallHandler( Func<SetupDriverContainer, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerContainerAdapter( handler, SetupCallContainerStep.Install ) );
        }

        public void AddInstallContentHandler( Func<SetupDriverContainer, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerContainerAdapter( handler, SetupCallContainerStep.InstallContent ) );
        }

        public void AddSettleHandler( Func<SetupDriverContainer, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerContainerAdapter( handler, SetupCallContainerStep.Settle ) );
        }

        public void AddSettleContentHandler( Func<SetupDriverContainer, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerContainerAdapter( handler, SetupCallContainerStep.SettleContent ) );
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
