using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class SetupDriver : SetupDriverBase
    {
        List<ISetupHandler> _handlers;

        public class BuildInfo
        {
            internal BuildInfo( SetupEngine engine, ISortedItem sortedItem, VersionedName externalVersion )
            {
                Engine = engine;
                SortedItem = sortedItem;
                ExternalVersion = externalVersion;
            }

            internal readonly SetupEngine Engine;
            internal readonly ISortedItem SortedItem;
            internal readonly VersionedName ExternalVersion;
        }

        public SetupDriver( BuildInfo info )
            : base( info.Engine, info.SortedItem, info.ExternalVersion, null )
        {
        }

        public override sealed bool IsContainerHead
        {
            get { return false; }
        }

        internal override sealed bool ExecuteInit()
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

        internal override sealed bool ExecuteInstall()
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

        internal override sealed bool ExecuteSettle()
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

        public void AddHandler( ISetupHandler handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            if( _handlers == null ) _handlers = new List<ISetupHandler>();
            _handlers.Add( handler );
        }

        public void AddInitHandler( Func<SetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerAdapter( handler, SetupStep.Init ) );
        }

        public void AddInstallHandler( Func<SetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerAdapter( handler, SetupStep.Install ) );
        }

        public void AddSettleHandler( Func<SetupDriver, bool> handler )
        {
            if( handler == null ) throw new ArgumentNullException( "handler" );
            AddHandler( new SetupHandlerAdapter( handler, SetupStep.Settle ) );
        }

        protected virtual bool Init()
        {
            return true;
        }

        protected virtual bool Install()
        {
            return true;
        }

        protected virtual bool Settle()
        {
            return true;
        }

    }
}
