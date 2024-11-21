using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using CK.Core;
using System.Collections;

namespace CK.Setup;

/// <summary>
/// Generic driver for <see cref="IDependentItem"/> that also handles the composite <see cref="IDependentItemGroup"/>.
/// </summary>
public class SetupItemDriver : DriverBase
{
    List<ISetupHandler> _handlers;
    internal readonly DriverBase Head;

    /// <summary>
    /// Encapsulates construction information for <see cref="SetupItemDriver"/> objects.
    /// This is an opaque parameter (except the <see cref="Drivers"/> and <see cref="AllDrivers"/> properties) that
    /// enables the abstract DriverBase to be correctly initialized.
    /// </summary>
    public sealed class BuildInfo
    {
        internal BuildInfo( IDriverList drivers, IDriverBaseList allDrivers, ISortedItem<ISetupItem> item, VersionedName externalVersion )
        {
            Head = null;
            Drivers = drivers;
            AllDrivers = allDrivers;
            ExternalVersion = externalVersion;
            SortedItem = item;
        }

        internal BuildInfo( DriverBase head, IDriverList drivers, IDriverBaseList allDrivers, ISortedItem<ISetupItem> item )
        {
            Head = head;
            Drivers = drivers;
            AllDrivers = allDrivers;
            ExternalVersion = head.ExternalVersion;
            SortedItem = item;
        }

        internal readonly IDriverList Drivers;
        internal readonly IDriverBaseList AllDrivers;
        internal readonly ISortedItem<ISetupItem> SortedItem;
        internal readonly VersionedName ExternalVersion;
        internal readonly DriverBase Head;
    }

    /// <summary>
    /// Initializes a new <see cref="SetupItemDriver"/>.
    /// </summary>
    /// <param name="info">Opaque parameter built by the framework.</param>
    public SetupItemDriver( BuildInfo info )
        : base( info.Drivers, info.AllDrivers, info.SortedItem, info.ExternalVersion )
    {
        Debug.Assert( info.Head == null || info.SortedItem.FullName + ".Head" == info.Head.FullName );
        Head = info.Head;
        PreviousDrivers = new PreviousDriversImpl( this );
    }

    class PreviousDriversImpl : IDriverList
    {
        readonly DriverBase _d;
        readonly int _count;

        public PreviousDriversImpl( SetupItemDriver d )
        {
            _d = d;
            _count = _d.Drivers.Count;
        }

        public SetupItemDriver this[string fullName]
        {
            get
            {
                var d = _d.Drivers[fullName];
                if( d != null && d.SortedItem.Index >= _d.SortedItem.Index ) d = null;
                return d;
            }
        }

        public SetupItemDriver this[IDependentItem item]
        {
            get
            {
                var d = _d.Drivers[item];
                if( d != null && d.SortedItem.Index >= _d.SortedItem.Index ) d = null;
                return d;
            }
        }

        public SetupItemDriver this[int index]
        {
            get
            {
                if( index >= _count ) throw new ArgumentOutOfRangeException( nameof( index ) );
                return _d.Drivers[index];
            }
        }

        public int Count => _count;

        public IEnumerator<SetupItemDriver> GetEnumerator() => _d.Drivers.Take( _count ).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Gets the ordered list of <see cref="SetupItemDriver"/> indexed by the <see cref="IDependentItem.FullName"/> 
    /// or by the <see cref="IDependentItem"/> object instance itself thar are before this one.
    /// </summary>
    public IDriverList PreviousDrivers { get; }

    /// <summary>
    /// Gets the container driver (or null if the item does not belong to a container).
    /// </summary>
    public SetupItemDriver ContainerDriver => Drivers[SortedItem.Container?.Item];

    /// <summary>
    /// Gets all the container driver, starting with this <see cref="ContainerDriver"/> up to the 
    /// root container driver.
    /// </summary>
    public IEnumerable<SetupItemDriver> ContainerDrivers
    {
        get
        {
            var i = this;
            for(; ; )
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
    /// Any <see cref="ISetupItemDriverAware.OnDriverPreInitialized"/> on setup items
    /// are called right after.
    /// Does nothing by default (always return true).
    /// </summary>
    /// <param name="monitor">Monitor to use.</param>
    /// <returns>True on success, false to stop the process.</returns>
    internal protected virtual bool ExecutePreInit( IActivityMonitor monitor ) => true;

    internal bool ExecuteHeadInit( IActivityMonitor monitor )
    {
        if( !Init( monitor, true ) || !OnStep( monitor, SetupCallGroupStep.Init, true ) ) return false;
        if( _handlers != null )
        {
            foreach( var h in _handlers )
            {
                if( !h.Init( monitor, this ) || !h.OnStep( monitor, this, SetupCallGroupStep.Init ) ) return false;
            }
        }
        return Init( monitor, false ) && OnStep( monitor, SetupCallGroupStep.Init, false );
    }

    internal override bool ExecuteInit( IActivityMonitor monitor )
    {
        if( !IsGroup ) return ExecuteHeadInit( monitor );
        // If the item is not a Group or a Container, InitContent is not called.
        if( !InitContent( monitor, true ) || !OnStep( monitor, SetupCallGroupStep.InitContent, true ) ) return false;
        if( _handlers != null )
        {
            foreach( var h in _handlers )
            {
                if( !h.InitContent( monitor, this ) || !h.OnStep( monitor, this, SetupCallGroupStep.InitContent ) ) return false;
            }
        }
        return InitContent( monitor, false ) && OnStep( monitor, SetupCallGroupStep.InitContent, false );
    }

    internal bool ExecuteHeadInstall( IActivityMonitor monitor )
    {
        if( !Install( monitor, true ) || !OnStep( monitor, SetupCallGroupStep.Install, true ) ) return false;
        if( _handlers != null )
        {
            foreach( var h in _handlers )
            {
                if( !h.Install( monitor, this ) || !h.OnStep( monitor, this, SetupCallGroupStep.Install ) ) return false;
            }
        }
        return Install( monitor, false ) && OnStep( monitor, SetupCallGroupStep.Install, false );
    }

    internal override bool ExecuteInstall( IActivityMonitor monitor )
    {
        if( !IsGroup ) return ExecuteHeadInstall( monitor );
        // If the item is not a Group or a Container, InstallContent is not called.
        if( !InstallContent( monitor, true ) || !OnStep( monitor, SetupCallGroupStep.InstallContent, true ) ) return false;
        if( _handlers != null )
        {
            foreach( var h in _handlers )
            {
                if( !h.InstallContent( monitor, this ) || !h.OnStep( monitor, this, SetupCallGroupStep.InstallContent ) ) return false;
            }
        }
        return InstallContent( monitor, false ) && OnStep( monitor, SetupCallGroupStep.InstallContent, false );
    }

    internal bool ExecuteHeadSettle( IActivityMonitor monitor )
    {
        if( !Settle( monitor, true ) || !OnStep( monitor, SetupCallGroupStep.Settle, true ) ) return false;
        if( _handlers != null )
        {
            foreach( var h in _handlers )
            {
                if( !h.Settle( monitor, this ) || !h.OnStep( monitor, this, SetupCallGroupStep.Settle ) ) return false;
            }
        }
        return Settle( monitor, false ) && OnStep( monitor, SetupCallGroupStep.Settle, false );
    }

    internal override bool ExecuteSettle( IActivityMonitor monitor )
    {
        if( !IsGroup ) return ExecuteHeadSettle( monitor );
        // If the item is not a Group or a Container, SettleContent is not called.
        if( !SettleContent( monitor, true ) || !OnStep( monitor, SetupCallGroupStep.SettleContent, true ) ) return false;
        if( _handlers != null )
        {
            foreach( var h in _handlers )
            {
                if( !h.SettleContent( monitor, this ) || !h.OnStep( monitor, this, SetupCallGroupStep.SettleContent ) ) return false;
            }
        }
        return SettleContent( monitor, false ) && OnStep( monitor, SetupCallGroupStep.SettleContent, false );
    }

    /// <summary>
    /// Adds a <see cref="ISetupHandler"/> in the chain of handlers.
    /// Can be called during any setup phasis (typically in the <see cref="SetupStep.Init"/> phasis): the new handler 
    /// will be appended to the the handlers queue and will be called normally.
    /// </summary>
    /// <param name="handler">The handler to append.</param>
    public void AddHandler( ISetupHandler handler )
    {
        if( handler == null ) throw new ArgumentNullException( nameof( handler ) );
        if( _handlers == null ) _handlers = new List<ISetupHandler>();
        _handlers.Add( handler );
    }

    /// <summary>
    /// Does nothing (always returns true).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="beforeHandlers">
    /// True when handlers associated to this driver have not been called yet.
    /// False when their <see cref="ISetupHandler.Init"/> method have been called.
    /// </param>
    /// <returns>Always true.</returns>
    internal protected virtual bool Init( IActivityMonitor monitor, bool beforeHandlers ) => true;

    /// <summary>
    /// Called, only if <see cref="IsGroup"/> is true, after <see cref="Init"/> (and <see cref="InitContent"/> for groups 
    /// or containers) have been called on all the contained items.
    /// Does nothing (always returns true).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="beforeHandlers">
    /// True when handlers associated to this driver have not been called yet.
    /// False when their <see cref="ISetupHandler.InitContent"/> method have been called.
    /// </param>
    /// <returns>Always true.</returns>
    protected virtual bool InitContent( IActivityMonitor monitor, bool beforeHandlers ) => true;

    /// <summary>
    /// Does nothing (always returns true).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="beforeHandlers">
    /// True when handlers associated to this driver have not been called yet.
    /// False when their <see cref="ISetupHandler.Install"/> method have been called.
    /// </param>
    /// <returns>Always true.</returns>
    internal protected virtual bool Install( IActivityMonitor monitor, bool beforeHandlers ) => true;

    /// <summary>
    /// Called, only if <see cref="IsGroup"/> is true, after <see cref="Install"/> (and <see cref="InstallContent"/> for groups 
    /// or containers) have been called on all the contained items.
    /// Does nothing (always returns true).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="beforeHandlers">
    /// True when handlers associated to this driver have not been called yet.
    /// False when their <see cref="ISetupHandler.InstallContent"/> method have been called.
    /// </param>
    protected virtual bool InstallContent( IActivityMonitor monitor, bool beforeHandlers ) => true;

    /// <summary>
    /// Does nothing (always returns true).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="beforeHandlers">
    /// True when handlers associated to this driver have not been called yet.
    /// False when their <see cref="ISetupHandler.Settle"/> method have been called.
    /// </param>
    /// <returns>Always true.</returns>
    internal protected virtual bool Settle( IActivityMonitor monitor, bool beforeHandlers ) => true;

    /// <summary>
    /// Called, only if <see cref="IsGroup"/> is true, after <see cref="Settle"/> (and <see cref="SettleContent"/> for groups 
    /// or containers) have been called on all the contained items.
    /// Does nothing (always returns true).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="beforeHandlers">
    /// True when handlers associated to this driver have not been called yet.
    /// False when their <see cref="ISetupHandler.SettleContent"/> method have been called.
    /// </param>
    protected virtual bool SettleContent( IActivityMonitor monitor, bool beforeHandlers ) => true;

    /// <summary>
    /// This method is called right after its corresponding dedicated method.
    /// This centralized step based method is easier to use then the different
    /// available overrides when the step actions are structurally the same and
    /// only their actual contents/data is step dependent.
    /// Does nothing (always returns true).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="step">The current step.</param>
    /// <param name="beforeHandlers">
    /// True when handlers associated to this driver have not been called yet.
    /// False when their associated step method have been called.
    /// </param>
    /// <returns>Always true.</returns>
    protected virtual bool OnStep( IActivityMonitor monitor, SetupCallGroupStep step, bool beforeHandlers ) => true;

}
