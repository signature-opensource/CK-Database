using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// Generic driver for <see cref="IDependentItem"/> (also handles <see cref="IDependentItemGoup"/>).
    /// </summary>
    public class SetupDriver : DriverBase
    {
        List<ISetupHandler> _handlers;
        internal readonly GroupHeadSetupDriver Head;

        public class BuildInfo
        {
            internal BuildInfo( SetupEngine engine, ISortedItem sortedItem, VersionedName externalVersion )
            {
                Head = null;
                Engine = engine;
                SortedItem = sortedItem;
                ExternalVersion = externalVersion;
            }

            internal BuildInfo( GroupHeadSetupDriver head, ISortedItem sortedItem )
            {
                Head = head;
                Engine = head.Engine;
                ExternalVersion = head.ExternalVersion;
                SortedItem = sortedItem;
            }

            internal readonly SetupEngine Engine;
            internal readonly ISortedItem SortedItem;
            internal readonly VersionedName ExternalVersion;
            internal readonly GroupHeadSetupDriver Head;
        }

        public SetupDriver( BuildInfo info )
            : base( info.Engine, info.SortedItem, info.ExternalVersion, info.Head != null ? info.Head.DirectDependencies : null )
        {
            Debug.Assert( info.Head == null || info.SortedItem.FullName + ".Head" == info.Head.FullName );
            Head = info.Head;
        }

        internal override bool IsGroupHead
        {
            get { return false; }
        }

        public bool IsGroup 
        { 
            get { return Head != null; } 
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

        public void AddHandler( ISetupHandler handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            if( _handlers == null ) _handlers = new List<ISetupHandler>();
            _handlers.Add( handler );
        }

        public void AddInitHandler( Func<SetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.Init ) );
        }

        public void AddInitContentHandler( Func<SetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.InitContent ) );
        }

        public void AddInstallHandler( Func<SetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.Install ) );
        }

        public void AddInstallContentHandler( Func<SetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.InstallContent ) );
        }

        public void AddSettleHandler( Func<SetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.Settle ) );
        }

        public void AddSettleContentHandler( Func<SetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerFuncAdapter( handler, SetupCallGroupStep.SettleContent ) );
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
