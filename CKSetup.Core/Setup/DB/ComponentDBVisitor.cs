using System;
using System.Collections.Generic;
using System.Text;

namespace CKSetup
{
    /// <summary>
    /// Visitor implementation that supports mutations.
    /// </summary>
    public class ComponentDBVisitor
    {
        /// <summary>
        /// Visits a component database.
        /// </summary>
        /// <param name="db">The component database to visit.</param>
        /// <returns>The original <paramref name="db"/> or a new one if a mutation occurred.</returns>
        public virtual ComponentDB Visit( ComponentDB db )
        {
            List<Component> newComps = null;
            for( int i = 0; i < db.Components.Count; ++i )
            {
                var c = db.Components[i];
                UpdateList( ref newComps, db.Components, i, c, VisitComponent( db, i, c ) );
            }
            return newComps == null ? db : new ComponentDB( db, newComps ); 
        }

        /// <summary>
        /// Visits a component by visiting its dependencies, emmbedded components and files.
        /// </summary>
        /// <param name="db">The component database.</param>
        /// <param name="idxComponent">Index of the visited component.</param>
        /// <param name="c">The component to visit.</param>
        /// <returns>The original <paramref name="c"/>, null to remove it, or a new one if a mutation occurred.</returns>
        protected virtual Component VisitComponent( ComponentDB db, int idxComponent, Component c )
        {
            var newDependencies = VisitComponentDependencies( db, idxComponent, c );
            var newEmbedded = VisitComponentEmbedded( db, idxComponent, c );
            var newFiles = VisitComponentFiles( db, idxComponent, c );
            return newDependencies == c.Dependencies && newEmbedded == c.Embedded && newFiles == c.Files
                        ? c
                        : new Component( c.ComponentKind, c.GetRef(), newDependencies, newEmbedded, newFiles );
        }

        /// <summary>
        /// Visits the dependencies of a component.
        /// </summary>
        /// <param name="db">The component database.</param>
        /// <param name="idxComponent">Index of the visited component.</param>
        /// <param name="c">The visited component.</param>
        /// <returns>The <see cref="Component.Dependencies"/> or a new one if needed.</returns>
        protected virtual IReadOnlyList<ComponentDependency> VisitComponentDependencies( ComponentDB db, int idxComponent, Component c )
        {
            return c.Dependencies;
        }

        /// <summary>
        /// Visits the embedded components of a component.
        /// </summary>
        /// <param name="db">The component database.</param>
        /// <param name="idxComponent">Index of the visited component.</param>
        /// <param name="c">The visited component.</param>
        /// <returns>The <see cref="Component.Embedded"/> list or a new one if needed.</returns>
        protected virtual IReadOnlyList<ComponentRef> VisitComponentEmbedded( ComponentDB db, int idxComponent, Component c )
        {
            return c.Embedded;
        }

        /// <summary>
        /// Visits the files of a component.
        /// </summary>
        /// <param name="db">The component database.</param>
        /// <param name="idxComponent">Index of the visited component.</param>
        /// <param name="c">The visited component.</param>
        /// <returns>The <see cref="Component.Files"/> list or a new one if needed.</returns>
        protected virtual IReadOnlyList<ComponentFile> VisitComponentFiles( ComponentDB db, int idxComponent, Component c )
        {
            return c.Files;
        }

        /// <summary>
        /// Helper method for list mutation.
        /// </summary>
        /// <typeparam name="T">Type of the item list.</typeparam>
        /// <param name="newOne">Mutable list (initially null and remains null as long as no change occurred).</param>
        /// <param name="original">The original list.</param>
        /// <param name="i">The current index of the item.</param>
        /// <param name="o">Original item.</param>
        /// <param name="oVisited">Visited item: unchanged <paramref name="o"/>, null to remove it, or a new one.</param>
        protected static void UpdateList<T>( ref List<T> newOne, IReadOnlyList<T> original, int i, T o, T oVisited ) where T : class
        {
            if( oVisited != o )
            {
                if( newOne == null )
                {
                    newOne = new List<T>( original.Count );
                    for( int j = 0; j < i; ++j ) newOne.Add( original[j] );
                }
                if( oVisited != null ) newOne.Add( oVisited );
            }
            else if( newOne != null ) newOne.Add( o );
        }

    }
}
