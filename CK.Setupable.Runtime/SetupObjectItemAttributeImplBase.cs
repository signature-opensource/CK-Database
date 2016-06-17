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

            SetupObjectItem CreateSetupObjectItem( Registerer r, IContextLocNaming name );

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

            public readonly IContextLocNaming Name;

            public SetupObjectItem Item;

            public IStObjSetupDynamicInitializer LastDefiner;

            public IMutableSetupItem FirstPackagesSeen;

            public IMutableSetupItem LastPackagesSeen;
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
                Item = item;
                StObj = stObj;
                _candidate = candidate;
            }


            public readonly IMutableSetupItem Item;

            public readonly IStObjResult StObj;

            public IActivityMonitor Monitor => _state.Monitor;

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
                    best.LastPackagesSeen = best.FirstPackagesSeen = Item;
                }
                else
                {
                    if( replace  )
                    {
                        // Replace from another package: memorze it as the best one so far, 
                        // otherwise skip it, except if it has been define in the same package..
                        if( best.LastPackagesSeen != Item )
                        {
                            best.LastDefiner = _candidate;
                            best.LastPackagesSeen = Item;
                        }
                        else if( best.FirstPackagesSeen == Item )
                        {
                            _state.Monitor.Error().Send( "Object {0} is both defined and replaced by the same package.", _candidate.GetDetailedName( this, name ) );
                            return SetError();
                        }
                    }
                    else 
                    {
                        // Defining from another package than the first one is an error.
                        // Otherwise, we keep the candidate. 
                        if( best.LastPackagesSeen != Item )
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
                    best.Item = DoCreateSetupObjectItem( best.Name );
                }
                return best.Item != null;
            }

            SetupObjectItem DoCreateSetupObjectItem( IContextLocNaming name )
            {
                SetupObjectItem o;
                using( _state.Monitor.OnError( () => HaseError = true ) )
                {
                    o = _candidate.CreateSetupObjectItem( this, name );
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
            return $"'{name}' in {Attribute.GetShortTypeName()} attribute of '{r.Item.FullName}' already exists."; 
        }

        IContextLocNaming ISetupItemCreator.BuildFullName( Registerer r, SetupObjectItemBehavior b, string name )
        {
            return BuildFullName( r, b, name );
        }

        SetupObjectItem ISetupItemCreator.CreateSetupObjectItem( Registerer r, IContextLocNaming name )
        {
            return CreateSetupObjectItem( r, name );
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
        /// <param name="name">The name from <see cref="BuildFullName"/> method.</param>
        /// <returns>
        /// A new SetupObject or null if it can not be created. If an errr occurred, it must 
        /// be logged to the monitor.
        /// </returns>
        protected abstract SetupObjectItem CreateSetupObjectItem( Registerer r, IContextLocNaming name );


    }

}
