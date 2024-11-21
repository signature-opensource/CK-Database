using CK.Core;
using System.Diagnostics;
using static CK.Setup.SetupObjectItemAttributeImplBase;

namespace CK.Setup;

/// <summary>
/// Stateless object that handles object initialization across multiple 
/// <see cref="IStObjSetupDynamicInitializer"/> in the context of <see cref="SetupObjectItemAttributeImplBase"/>.
/// It only exposes contextual information to actual initializers: most of the job is done internally.
/// </summary>
public class SetupObjectItemAttributeRegisterer
{
    readonly IStObjSetupDynamicInitializerState _state;
    readonly ISetupItemCreator _candidate;

    internal SetupObjectItemAttributeRegisterer(
        IStObjSetupDynamicInitializerState state,
        IMutableSetupItem item,
        IStObjResult stObj,
        ISetupItemCreator candidate )
    {
        _state = state;
        Container = item;
        _candidate = candidate;
    }

    /// <summary>
    /// Gets the monitor to use.
    /// </summary>
    public IActivityMonitor Monitor => _state.Monitor;

    /// <summary>
    /// Gets the container that attempts to register the item.
    /// </summary>
    public readonly IMutableSetupItem Container;

    /// <summary>
    /// Gets whether <see cref="SetError(string)"/> has been called on any
    /// registerer.
    /// </summary>
    public bool HasError => _state.Memory[typeof( SetupObjectItemAttributeRegisterer )] != null;

    /// <summary>
    /// Sets an error that can be null (no error is traced but HasError becomes true).
    /// Memory is marked as having processed the object.
    /// </summary>
    /// <param name="msg">Optional message to trace as error.</param>
    /// <returns>Always null.</returns>
    SetupObjectItemDynamicResource SetError( string msg )
    {
        if( msg != null ) _state.Monitor.Error( msg );
        _state.Memory[typeof( SetupObjectItemAttributeRegisterer )] = typeof( SetupObjectItemAttributeRegisterer );
        return null;
    }

    internal SetupObjectItemDynamicResource Register( SetupObjectItemBehavior b, string name )
    {
        var n = _candidate.BuildFullName( Container, b, name );
        if( n == null )
        {
            return SetError( "Invalid name: " + _candidate.GetDetailedName( Container, name ) );
        }
        bool replace = b == SetupObjectItemBehavior.Replace;
        var key = new SetupObjectItemDynamicResource( n );
        SetupObjectItemDynamicResource best = (SetupObjectItemDynamicResource)_state.Memory[key];
        if( best == null )
        {
            if( replace )
            {
                return SetError( $"Object {_candidate.GetDetailedName( Container, name )} is not defined. It can not be replaced." );
            }
            SetupObjectItemDynamicResource bestT = null;
            string transformArg = n.TransformArg;
            if( transformArg != null )
            {
                var nT = new ContextLocName( transformArg );
                var keyT = new SetupObjectItemDynamicResource( nT );
                bestT = (SetupObjectItemDynamicResource)_state.Memory[keyT];
                if( bestT == null )
                {
                    return SetError( $"Transformer {_candidate.GetDetailedName( Container, name )}: target {nT} is not defined." );
                }
            }
            _state.Memory[key] = best = key;
            best.LastDefiner = _candidate;
            best.TransformTarget = bestT;
            best.LastContainerSeen = best.FirstContainer = Container;
        }
        else
        {
            if( replace )
            {
                // Replace from another package: memorize it as the best one so far, 
                // otherwise skip it, except if it has been defined in the same package..
                if( best.LastContainerSeen != Container )
                {
                    best.LastDefiner = _candidate;
                    best.LastContainerSeen = Container;
                }
                else if( best.FirstContainer == Container )
                {
                    return SetError( $"Object {_candidate.GetDetailedName( Container, name )} is both defined and replaced by the same package." );
                }
            }
            else
            {
                // Defining from another package than the first one is an error.
                // Otherwise, we keep the candidate. 
                if( best.LastContainerSeen != Container )
                {
                    return SetError( $"Object {_candidate.GetDetailedName( Container, name )} is already defined." );
                }
            }
        }
        return best;
    }

    internal bool PostponeFinalizeRegister( SetupObjectItemDynamicResource best )
    {
        if( best.LastDefiner == _candidate )
        {
            SetupObjectItem tArg = null;
            if( best.TransformTarget != null && (tArg = best.TransformTarget.Item) == null )
            {
                return !HasError;
            }
            Debug.Assert( best.Item == null, "We are the only winner (the last one)." );
            best.Item = DoCreateSetupObjectItem( best.FirstContainer, best.Name, tArg );
        }
        return false;
    }

    SetupObjectItem DoCreateSetupObjectItem( IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument )
    {
        SetupObjectItem o;
        using( _state.Monitor.OpenInfo( $"Handling: {_candidate.GetDetailedName( Container, name.FullName )}" ) )
        using( _state.Monitor.OnError( () => SetError( null ) ) )
        {
            o = _candidate.CreateSetupObjectItem( this, firstContainer, name, transformArgument );
            if( o == null && HasError == false )
            {
                SetError( "Unable to create setup object." );
            }
        }
        return HasError ? null : o;
    }

}
