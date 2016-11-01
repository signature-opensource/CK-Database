using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Base implementation for <see cref="SetupObjectItemAttributeBase"/> attributes declared on a class 
    /// that dynamically define one or more <see cref="SetupObjectItem"/>.
    /// Multiples object names like "sUserCreate, sUserDestroy, AnotherSchema.sUserUpgrade, CK.sUserRun" can be defined.
    /// </summary>
    public abstract class SetupObjectItemAttributeImplBase : SetupObjectItemAttributeImplBase.ISetupItemCreator
    {
        readonly SetupObjectItemAttributeBase _attribute;
        ISetupEngineAspectProvider _aspectProvider;
        List<BestCreator> _theBest;

        internal interface ISetupItemCreator : IStObjSetupDynamicInitializer
        {
            IContextLocNaming BuildFullName( Registerer r, SetupObjectItemBehavior b, string name );

            /// <summary>
            /// Must create the object.
            /// </summary>
            /// <param name="r">The registerer that exposes context informations.</param>
            /// <param name="firstContainer">
            /// The first container in which the item has been defined.
            /// When there is no replacement, this is the same as <see cref="Registerer.Container"/>.
            /// </param>
            /// <param name="name">The name of the object to create.</param>
            /// <param name="transformArgument">
            /// The transformation target if this setup item is a transformer.
            /// </param>
            /// <returns>The newly created item.</returns>
            SetupObjectItem CreateSetupObjectItem( Registerer r, IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument );

            string GetDetailedName( Registerer r, string name );
        }

        /// <summary>
        /// This is used both for the key and the value.
        /// This secures the key in the IStObjSetupDynamicInitializerState.Memory dictionary: only an internal 
        /// BestCreator can be equal to a BestCreator.
        /// </summary>
        internal class BestCreator
        {
            int _hash;

            internal BestCreator( IContextLocNaming name )
            {
                Name = name;
                _hash = name.FullName.GetHashCode();
            }

            public override bool Equals( object obj )
            {
                BestCreator x = obj as BestCreator;
                return x != null && x.Name.FullName == Name.FullName;
            }

            public override int GetHashCode() => _hash;

            /// <summary>
            /// Name of the item to create is the key.
            /// </summary>
            public readonly IContextLocNaming Name;

            /// <summary>
            /// The eventually created item.
            /// </summary>
            public SetupObjectItem Item;

            /// <summary>
            /// The last definer is the winner.
            /// </summary>
            public IStObjSetupDynamicInitializer LastDefiner;

            /// <summary>
            /// Keeps the container that has the first definer.
            /// </summary>
            public IMutableSetupItem FirstContainer;

            /// <summary>
            /// Keeping the last container is used to handle multiple definitions
            /// in the same container.
            /// </summary>
            public IMutableSetupItem LastContainerSeen;

            /// <summary>
            /// The transform target creator in Name has a transform argument.
            /// </summary>
            public BestCreator TransformTarget;

            public bool MustWaitForTransformArg => TransformTarget != null && TransformTarget.Item == null;

        }

        /// <summary>
        /// Stateless object that handles object initialization across multiple 
        /// <see cref="IStObjSetupDynamicInitializer"/>.
        /// It only exposes contextual information to actual intializers: most of the job is done internally.
        /// </summary>
        public class Registerer
        {
            readonly IStObjSetupDynamicInitializerState _state;
            readonly ISetupItemCreator _candidate;

            internal Registerer(
                IStObjSetupDynamicInitializerState state,
                IMutableSetupItem item,
                IStObjResult stObj,
                ISetupItemCreator candidate )
            {
                _state = state;
                Container = item;
                StObj = stObj;
                _candidate = candidate;
                HasError = _state.Memory[typeof( Registerer )] != null;
            }

            /// <summary>
            /// Gets the monitor to use.
            /// </summary>
            public IActivityMonitor Monitor => _state.Monitor;

            /// <summary>
            /// Gets the dynamic initializer shared state.
            /// </summary>
            public IStObjSetupDynamicInitializerState State => _state;

            /// <summary>
            /// Gets the container that attempts to register the item.
            /// </summary>
            public readonly IMutableSetupItem Container;

            /// <summary>
            /// Gets the stObj associated to the <see cref="Container"/>.
            /// </summary>
            public readonly IStObjResult StObj;

            public bool HasError { get; private set; }

            BestCreator SetError( string msg )
            {
                if( msg != null ) _state.Monitor.Error().Send( msg );
                HasError = true;
                _state.Memory[typeof( Registerer )] = typeof( Registerer );
                return null;
            }

            internal BestCreator Register( SetupObjectItemBehavior b, string name )
            {
                var n = _candidate.BuildFullName( this, b, name );
                if( n == null )
                {
                    return SetError( "Invalid name: " + _candidate.GetDetailedName( this, name ) );
                }
                bool replace = b == SetupObjectItemBehavior.Replace;
                var key = new BestCreator( n );
                BestCreator best = (BestCreator)_state.Memory[key];
                if( best == null )
                {
                    if( replace )
                    {
                        return SetError( $"Object {_candidate.GetDetailedName( this, name )} is not defined. It can not be replaced." );
                    }
                    BestCreator bestT = null;
                    string transformArg = n.TransformArg;
                    if( transformArg != null )
                    {
                        var nT = new ContextLocName( transformArg );
                        var keyT = new BestCreator( nT );
                        bestT = (BestCreator)_state.Memory[keyT];
                        if( bestT == null )
                        {
                            return SetError( $"Transformer {_candidate.GetDetailedName( this, name )}'s target is not defined." );
                        }
                    }
                    _state.Memory[key] = best = key;
                    best.LastDefiner = _candidate;
                    best.TransformTarget = bestT;
                    best.LastContainerSeen = best.FirstContainer = Container;
                }
                else
                {
                    if( replace  )
                    {
                        // Replace from another package: memorze it as the best one so far, 
                        // otherwise skip it, except if it has been define in the same package..
                        if( best.LastContainerSeen != Container )
                        {
                            best.LastDefiner = _candidate;
                            best.LastContainerSeen = Container;
                        }
                        else if( best.FirstContainer == Container )
                        {
                            return SetError( $"Object {_candidate.GetDetailedName( this, name )} is both defined and replaced by the same package." );
                        }
                    }
                    else 
                    {
                        // Defining from another package than the first one is an error.
                        // Otherwise, we keep the candidate. 
                        if( best.LastContainerSeen != Container )
                        {
                            return SetError( $"Object {_candidate.GetDetailedName( this, name )} is already defined." );
                        }
                    }
                }
                return best;
            }

            internal bool PostponeFinalizeRegister( BestCreator best )
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
                using( _state.Monitor.OpenInfo().Send( "Handling: " + _candidate.GetDetailedName( this, name.FullName ) ) )
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

        protected SetupObjectItemAttributeImplBase( SetupObjectItemAttributeBase a )
        {
            _attribute = a;
        }

        /// <summary>
        /// Gets the attribute.
        /// </summary>
        protected SetupObjectItemAttributeBase Attribute => _attribute; 

        /// <summary>
        /// Gets the aspects provider.
        /// </summary>
        protected ISetupEngineAspectProvider SetupEngineAspectProvider => _aspectProvider; 

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            _aspectProvider = state.AspectProvider;
            var r = new Registerer( state, item, stObj, this );
            HashSet<string> already = new HashSet<string>();
            foreach( var n in Attribute.CommaSeparatedObjectNames.Split( ',' ) )
            {
                string nTrimmed = n.Trim();
                if( nTrimmed.Length > 0 )
                {
                    SetupObjectItemBehavior behavior;
                    nTrimmed = ExtractBehavior( out behavior, nTrimmed );
                    if( already.Add( nTrimmed ) )
                    {
                        var best = r.Register( behavior, nTrimmed );
                        if( best != null )
                        {
                            if( _theBest == null ) _theBest = new List<BestCreator>();
                            _theBest.Add( best );
                        }
                    }
                    else state.Monitor.Warn().Send( "Duplicate name '{0}' in SqlObjectItem attribute of '{1}'.", nTrimmed, item.FullName );
                }
            }
            if( !r.HasError && _theBest != null )
            {
                state.PushAction( DynamicItemCreateAfterFollowing );
            }
        }

        internal static string ExtractBehavior( out SetupObjectItemBehavior b, string name )
        {
            if( name.StartsWith( "replace:" ) )
            {
                b = SetupObjectItemBehavior.Replace;
                return name.Substring( 8 );
            }
            if( name.StartsWith( "transform:" ) )
            {
                b = SetupObjectItemBehavior.Transform;
                return name.Substring( 10 );
            }
            b = SetupObjectItemBehavior.Define;
            return name.StartsWith( "define:" ) ? name.Substring( 7 ) : name;
        }

        void DynamicItemCreateAfterFollowing( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            Debug.Assert( _theBest != null && _theBest.Count > 0 );
            var r = new Registerer( state, item, stObj, this );
            for( int i = _theBest.Count - 1; i >= 0 && !r.HasError; --i )
            {
                var best = _theBest[i];
                if( !r.PostponeFinalizeRegister( best ) ) _theBest.RemoveAt( i );
            }
            if( !r.HasError && _theBest.Count > 0 ) state.PushAction( DynamicItemCreateAfterFollowing );
        }

        string ISetupItemCreator.GetDetailedName( Registerer r, string name ) => GetDetailedName( r, name );

        IContextLocNaming ISetupItemCreator.BuildFullName( Registerer r, SetupObjectItemBehavior b, string name )
        {
            return BuildFullName( r, b, name );
        }

        SetupObjectItem ISetupItemCreator.CreateSetupObjectItem( Registerer r, IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument )
        {
            return CreateSetupObjectItem( r, firstContainer, name, transformArgument );
        }

        /// <summary>
        /// Helper method used by the kernel that generates a clear string that gives  
        /// detailed information about the location of the name beeing processed like
        /// '{name} in {Attribute} attribute of {holding class}'.
        /// This is exposed as a protected method so that specialized classes can easily emit log messages.
        /// </summary>
        /// <param name="r">The current registerer.</param>
        /// <param name="name">The object's name that is processed.</param>
        /// <returns>Detailed information.</returns>
        protected string GetDetailedName( Registerer r, string name )
        {
            return $"'{name}' in {Attribute.GetShortTypeName()} attribute of '{r.Container.FullName}'"; 
        }

        /// <summary>
        /// Must build the <see cref="IContextLocNaming"/> name of the future <see cref="SetupObjectItem"/> with the help of the owner object and the name in the attribute.
        /// This is called for each name in <see cref="SetupObjectItemAttributeBase.CommaSeparatedObjectNames"/>.
        /// </summary>
        /// <param name="Registerer">Registerer context object.</param>
        /// <param name="b">Registration behavior.</param>
        /// <param name="name">The raw name.</param>
        /// <returns>The name of the SetupObjectItem.</returns>
        protected abstract IContextLocNaming BuildFullName( Registerer r, SetupObjectItemBehavior b, string name );

        /// <summary>
        /// Must create the <see cref="SetupObjectItem"/>.
        /// This is called for each name in <see cref="SetupObjectItemAttributeBase.CommaSeparatedObjectNames"/>
        /// after <see cref="BuildFullName"/> has been called.
        /// </summary>
        /// <param name="Registerer">Registerer context object.</param>
        /// <param name="firstContainer">
        /// The first container in which the item has been defined.
        /// When there is no replacement, this is the same as <see cref="Registerer.Container"/>.
        /// </param>
        /// <param name="name">The name from <see cref="BuildFullName"/> method.</param>
        /// <param name="transformArgument">
        /// The transformation target if this setup item is a transformer.
        /// </param>
        /// <returns>
        /// A new SetupObject or null if it can not be created. If an errr occurred, it must 
        /// be logged to the monitor.
        /// </returns>
        protected abstract SetupObjectItem CreateSetupObjectItem( Registerer r, IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument );


    }

}
