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
        readonly int _maxObjectCount;
        List<BestCreator> _theBest;

        /// <summary>
        /// Internal interface that enables code factorization between SetupObjectItemAttributeImplBase
        /// that handles multiple objects and SetupObjectItemMemberAttributeImplBase that handles only one
        /// object: both implement it and the SetupObjectItemAttributeRegisterer, created internally uses it. 
        /// </summary>
        internal interface ISetupItemCreator : IStObjSetupDynamicInitializer
        {
            IContextLocNaming BuildFullName( ISetupItem container, SetupObjectItemBehavior b, string name );

            /// <summary>
            /// Must create the object.
            /// </summary>
            /// <param name="r">The registerer that exposes context informations.</param>
            /// <param name="firstContainer">
            /// The first container in which the item has been defined.
            /// When there is no replacement, this is the same as <see cref="SetupObjectItemAttributeRegisterer.Container"/>.
            /// </param>
            /// <param name="name">The name of the object to create.</param>
            /// <param name="transformArgument">
            /// The transformation target if this setup item is a transformer.
            /// </param>
            /// <returns>The newly created item.</returns>
            SetupObjectItem CreateSetupObjectItem( SetupObjectItemAttributeRegisterer r, IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument );

            string GetDetailedName( ISetupItem container, string name );
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
        }

        /// <summary>
        /// Initializes a new SetupObjectItemAttributeBase, with an optional maximal number of
        /// objects that can be defined by the attribute.
        /// </summary>
        /// <param name="a">The attribute.</param>
        /// <param name="maxObjectCount">The maximal number of objects (by default multiple objects are allowed).</param>
        protected SetupObjectItemAttributeImplBase( SetupObjectItemAttributeBase a, int maxObjectCount = 0 )
        {
            _attribute = a;
            _maxObjectCount = maxObjectCount;
        }

        /// <summary>
        /// Gets the attribute.
        /// </summary>
        protected SetupObjectItemAttributeBase Attribute => _attribute; 

        void IStObjSetupDynamicInitializer.DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjResult stObj )
        {
            var r = new SetupObjectItemAttributeRegisterer( state, item, stObj, this );
            if( _maxObjectCount == 1 ) Register( Attribute.NameOrCommaSeparatedObjectNames, r, item, state );
            else
            {
                var names = Attribute.NameOrCommaSeparatedObjectNames.Split( ',' );
                if( _maxObjectCount != 0 && names.Length > _maxObjectCount )
                {
                    state.Monitor.Error( $"At most {_maxObjectCount} names allowed:  '{GetDetailedName(r.Container, Attribute.NameOrCommaSeparatedObjectNames)}'." );
                    return;
                }
                HashSet<string> already = new HashSet<string>();
                foreach( var n in Attribute.NameOrCommaSeparatedObjectNames.Split( ',' ) )
                {
                    Register( n, r, item, state, already );
                }
            }
            if( !r.HasError && _theBest != null )
            {
                state.PushAction( DynamicItemCreateAfterFollowing );
            }
        }

        void Register( string n, SetupObjectItemAttributeRegisterer r, IMutableSetupItem item, IStObjSetupDynamicInitializerState state, HashSet<string> already = null )
        {
            string nTrimmed = n.Trim();
            if( nTrimmed.Length > 0 )
            {
                SetupObjectItemBehavior behavior;
                nTrimmed = ExtractBehavior( out behavior, nTrimmed );
                if( already == null || already.Add( nTrimmed ) )
                {
                    var best = r.Register( behavior, nTrimmed );
                    if( best != null )
                    {
                        if( _theBest == null ) _theBest = new List<BestCreator>();
                        _theBest.Add( best );
                    }
                }
                else state.Monitor.Warn( $"Duplicate name '{nTrimmed}' in SqlObjectItem attribute of '{item.FullName}'."  );
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
            var r = new SetupObjectItemAttributeRegisterer( state, item, stObj, this );
            for( int i = _theBest.Count - 1; i >= 0 && !r.HasError; --i )
            {
                var best = _theBest[i];
                if( !r.PostponeFinalizeRegister( best ) ) _theBest.RemoveAt( i );
            }
            if( !r.HasError && _theBest.Count > 0 ) state.PushAction( DynamicItemCreateAfterFollowing );
        }

        string ISetupItemCreator.GetDetailedName( ISetupItem container, string name ) => GetDetailedName( container, name );

        IContextLocNaming ISetupItemCreator.BuildFullName( ISetupItem container, SetupObjectItemBehavior b, string name )
        {
            return BuildFullName( container, b, name );
        }

        SetupObjectItem ISetupItemCreator.CreateSetupObjectItem( SetupObjectItemAttributeRegisterer r, IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument )
        {
            return CreateSetupObjectItem( r, firstContainer, name, transformArgument );
        }

        /// <summary>
        /// Helper method used by the kernel that generates a clear string that gives  
        /// detailed information about the location of the name beeing processed like
        /// '{name} in {Attribute} attribute of {container.FullName}'.
        /// This is exposed as a protected method so that specialized classes can easily emit log messages.
        /// </summary>
        /// <param name="container">The container that attempts to register the object.</param>
        /// <param name="name">The object's name that is processed.</param>
        /// <returns>Detailed information.</returns>
        protected virtual string GetDetailedName( ISetupItem container, string name )
        {
            return $"'{name}' in {Attribute.GetShortTypeName()} attribute of '{container.FullName}'"; 
        }

        /// <summary>
        /// Must build the <see cref="IContextLocNaming"/> name of the future <see cref="SetupObjectItem"/> with the help of the owner object and the name in the attribute.
        /// This is called for each name in <see cref="SetupObjectItemAttributeBase.NameOrCommaSeparatedObjectNames"/>.
        /// </summary>
        /// <param name="container">Container object.</param>
        /// <param name="b">Registration behavior.</param>
        /// <param name="name">The raw name.</param>
        /// <returns>The name of the SetupObjectItem.</returns>
        protected abstract IContextLocNaming BuildFullName( ISetupItem container, SetupObjectItemBehavior b, string name );

        /// <summary>
        /// Must create the <see cref="SetupObjectItem"/>.
        /// This is called for each name in <see cref="SetupObjectItemAttributeBase.NameOrCommaSeparatedObjectNames"/>
        /// after <see cref="BuildFullName"/> has been called.
        /// </summary>
        /// <param name="r">Registerer context object.</param>
        /// <param name="firstContainer">
        /// The first container in which the item has been defined.
        /// When there is no replacement, this is the same as <see cref="SetupObjectItemAttributeRegisterer.Container"/>.
        /// </param>
        /// <param name="name">The name from <see cref="BuildFullName"/> method.</param>
        /// <param name="transformArgument">
        /// The transformation target if this setup item is a transformer.
        /// </param>
        /// <returns>
        /// A new SetupObject or null if it can not be created. If an error occurred, it must 
        /// be logged to the monitor.
        /// </returns>
        protected abstract SetupObjectItem CreateSetupObjectItem( SetupObjectItemAttributeRegisterer r, IMutableSetupItem firstContainer, IContextLocNaming name, SetupObjectItem transformArgument );


    }

}
