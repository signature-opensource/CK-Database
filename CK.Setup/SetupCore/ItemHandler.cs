using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class ItemHandler : IItemHandler
    {
        protected ItemHandler( ItemDriver d )
        {
            Driver = d;
            Driver.AddHandler( this );
        }

        protected ItemDriver Driver { get; private set; }

        void CheckCall( ItemDriver d )
        {
            if( d != Driver ) throw new ArgumentException( "Call mismatch: handler is bound to another driver." );
        }

        bool IItemHandler.Init( ItemDriver d )
        {
            CheckCall( d );
            return Init();
        }

        bool IItemHandler.Install( ItemDriver d )
        {
            CheckCall( d );
            return Install();
        }

        bool IItemHandler.Settle( ItemDriver d )
        {
            CheckCall( d );
            return Settle();
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
