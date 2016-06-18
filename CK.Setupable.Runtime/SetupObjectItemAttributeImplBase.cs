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
            /// <returns>The newly created item.</returns>
            SetupObjectItem CreateSetupObjectItem( Registerer r, IMutableSetupItem firstContainer, IContextLocNaming name );

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
            /// Rhe eventually created item.
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
        }

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
            /// Gets the stObj associated to the <see cref="Container"/>.
            /// </summary>
            public readonly IStObjResult StObj;


            public bool HaseError { get; private set; }

            BestCreator SetError()
            {
                HaseError = true;
                return null;
            }

            internal BestCreator Register( SetupObjectItemBehavior b, string name )
            {
                var n = _candidate.BuildFullName( this, b, name );
                if( n == null )
                {
                    _state.Monitor.Error().Send( "Invalid name: "+ _candidate.GetDetailedName( this, name ) );
                    return SetError();
                }
                bool replace = b == SetupObjectItemBehavior.Replace;
                var key = new BestCreator( n );
                BestCreator best = (BestCreator)_state.Memory[key];
                if( best == null )
                {
                    if( replace )
                    {
                        _state.Monitor.Error().Send( "Object {0} is not defined. It can not be replaced.", _candidate.GetDetailedName( this, name ) );
                        return SetError();
                    }
                    _state.Memory[key] = best = key;
                    best.LastDefiner = _candidate;
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
                            _state.Monitor.Error().Send( "Object {0} is both defined and replaced by the same package.", _candidate.GetDetailedName( this, name ) );
                            return SetError();
                        }
                    }
                    else 
                    {
                        // Defining from another package than the first one is an error.
                        // Otherwise, we keep the candidate. 
                        if( best.LastContainerSeen != Container )
                        {
                            _state.Monitor.Error().Send( "Object {0} is already defined.", _candidate.GetDetailedName( this, name ) );
                            return SetError();
                        }
                    }
                }
                return best;
            }

            internal bool FinalizeRegister( BestCreator best )
            {
                if( best.LastDefiner == _candidate )
                {
                    Debug.Assert( best.Item == null, "We are the only winner (the last one)." );
                    best.Item = DoCreateSetupObjectItem( best.FirstContainer, best.Name );
                }
                return best.Item != null;
            }

            SetupObjectItem DoCreateSetupObjectItem( IMutableSetupItem firstContainer, IContextLocNaming name )
            {
                SetupObjectItem o;
                using( _state.Monitor.OnError( () => HaseError = true ) )
                {
                    o = _candidate.CreateSetupObjectItem( this, firstContainer, name );
                    if( o == null && HaseError == false )
                    {
                        _state.Monitor.Error().Send( "Unable to create setup object: " + _candidate.GetDetailedName( this, name.FullName ) );
                    }
                }
                return HaseError ? null : o;
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
            if( !r.HaseError && _theBest != null )
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
            var r = new Registerer( state, item, stObj, this );
            foreach( var best in _theBest )
            {
                r.FinalizeRegister( best );
            }
        }

        string ISetupItemCreator.GetDetailedName( Registerer r, string name )
        {
            return $"'{name}' in {Attribute.GetShortTypeName()} attribute of '{r.Container.FullName}'"; 
        }

        IContextLocNaming ISetupItemCreator.BuildFullName( Registerer r, SetupObjectItemBehavior b, string name )
        {
            return BuildFullName( r, b, name );
        }

        SetupObjectItem ISetupItemCreator.CreateSetupObjectItem( Registerer r, IMutableSetupItem firstContainer, IContextLocNaming name )
        {
            return CreateSetupObjectItem( r, firstContainer, name );
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
        /// <returns>
        /// A new SetupObject or null if it can not be created. If an errr occurred, it must 
        /// be logged to the monitor.
        /// </returns>
        protected abstract SetupObjectItem CreateSetupObjectItem( Registerer r, IMutableSetupItem firstContainer, IContextLocNaming name );


    }

}
