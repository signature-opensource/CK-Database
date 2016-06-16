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
    public abstract class SetupObjectItemAttributeImplBase : IStObjSetupDynamicInitializer
    {
        readonly SetupObjectItemAttributeBase _attribute;
        ISetupEngineAspectProvider _aspectProvider;
        List<BestCreator> _theBest;

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

        /// <summary>
        /// Must build the <see cref="IContextLocNaming"/> name of the future <see cref="SetupObjectItem"/> with the help of the owner object and the name in the attribute.
        /// This is called for each name in <see cref="SetupObjectItemAttributeBase.CommaSeparatedObjectNames"/>.
        /// </summary>
        /// <param name="ownerItem">Owner item.</param>
        /// <param name="ownerStObj">Owner object StObj information.</param>
        /// <param name="attributeName">Name as it appears in the attribute.</param>
        /// <returns>The name of the SetupObjectItem.</returns>
        protected abstract IContextLocNaming BuildFullName( IMutableSetupItem ownerItem, IStObjResult ownerStObj, string attributeName );

        /// <summary>
        /// Must create the <see cref="SetupObjectItem"/>.
        /// This is called for each name in <see cref="SetupObjectItemAttributeBase.CommaSeparatedObjectNames"/>.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="ownerItem">Owner item.</param>
        /// <param name="ownerStObj">Owner object StObj information.</param>
        /// <param name="name">The name from <see cref="BuildFullName"/> method.</param>
        /// <returns>A new SetupObject.</returns>
        protected abstract SetupObjectItem CreateSetupObjectItem( IActivityMonitor monitor, IMutableSetupItem ownerItem, IStObjResult ownerStObj, IContextLocNaming name );

        /// <summary>
        /// This is used both for the key and the value.
        /// This secures the key in the IStObjSetupDynamicInitializerState.Memory dictionary: only an internal BestInitializer can be equal to a BestInitializer.
        /// </summary>
        internal class BestCreator
        {
            int _hash;

            public BestCreator( IContextLocNaming name )
            {
                Name = name;
                _hash = name.FullName.GetHashCode();
            }

            public override bool Equals( object obj )
            {
                BestCreator x = obj as BestCreator;
                return x != null && x.Name.FullName == Name.FullName;
            }

            public override int GetHashCode()
            {
                return _hash;
            }

            public readonly IContextLocNaming Name;

            public IStObjSetupDynamicInitializer Creator;

            public SetupObjectItem Item;

            public IStObjSetupDynamicInitializer FirstCreator;

            public SetupObjectItem FirstItem;

            public IMutableSetupItem LastPackagesSeen;
        }

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            _aspectProvider = state.AspectProvider;
            HashSet<string> already = new HashSet<string>();
            foreach( var n in Attribute.CommaSeparatedObjectNames.Split( ',' ) )
            {
                string nTrimmed = n.Trim();
                if( nTrimmed.Length > 0 )
                {
                    if( already.Add( nTrimmed ) )
                    {
                        IContextLocNaming name = BuildFullName( item, stObj, nTrimmed );
                        if( name == null )
                        {
                            state.Monitor.Error().Send( "Invalid object name '{0}' in {2} attribute of '{1}'.", nTrimmed, item.FullName, Attribute.GetShortTypeName() );
                        }
                        else
                        {
                            if( _theBest == null ) _theBest = new List<BestCreator>();
                            var meBest = AssumeBestCreator( state, name, this );
                            if( meBest.FirstCreator == this )
                            {
                                meBest.FirstItem = CreateSetupObjectItem( state.Monitor, item, stObj, name );
                                meBest.LastPackagesSeen = item;
                            }
                            _theBest.Add( meBest );
                        }
                    }
                    else state.Monitor.Warn().Send( "Duplicate name '{0}' in SqlObjectItem attribute of '{1}'.", nTrimmed, item.FullName );
                }
            }
            if( _theBest != null )
            {
                state.PushAction( DynamicItemCreateAfterFollowing );
            }
        }

        internal static BestCreator AssumeBestCreator( IStObjSetupDynamicInitializerState state, IContextLocNaming name, IStObjSetupDynamicInitializer candidate )
        {
            var meBest = new BestCreator( name );
            BestCreator theBest = (BestCreator)state.Memory[meBest];
            if( theBest == null )
            {
                state.Memory[meBest] = theBest = meBest;
                meBest.FirstCreator = candidate;
            }
            Debug.Assert( theBest.Name.FullName == name.FullName );
            // Override any previous configurations: candidate is the best so far.
            theBest.Creator = candidate;
            return theBest;
        }

        void DynamicItemCreateAfterFollowing( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            foreach( var best in _theBest )
            {
                // If we are the best, our resource wins.
                if( best.Creator == this )
                {
                    Debug.Assert( best.Item == null, "We are the only winner (the last one)." );
                    if( best.FirstCreator == this )
                    {
                        best.Item = best.FirstItem;
                    }
                    else
                    {
                        // When multiples members exist bound to the same object, this avoids 
                        // to load the same resource multiple times: only the first occurence per owner is considered.
                        if( best.LastPackagesSeen != item )
                        {
                            best.Item = CreateSetupObjectItem( state.Monitor, item, stObj, best.Name );
                            best.FirstItem.ReplacedBy = best.Item;
                            best.LastPackagesSeen = item;
                        }
                    }
                }
            }
        }

    }

}
