#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\StObj\Impl\TypeInfo\CovariantPropertyInfo.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CK.Core;
using System.Diagnostics;

namespace CK.Setup
{
    internal class CovariantPropertyInfo : INamedPropertyInfo
    {
        public readonly static string KindName = "[Covariant]";

        readonly PropertyInfo _p;
        int _index;
        int _definerSpecializationDepth;
        PropertyInfo _settablePropertyInfo;

        internal CovariantPropertyInfo( PropertyInfo p, int definerSpecializationDepth, int index )
        {
            Debug.Assert( p != null );
            _p = p;
            _definerSpecializationDepth = definerSpecializationDepth;
            _index = index;
        }

        public string Name { get { return _p.Name; } }
        public Type PropertyType { get { return _p.PropertyType; } }
        public Type DeclaringType { get { return _p.DeclaringType; } }
        public PropertyInfo PropertyInfo { get { return _p; } }

        /// <summary>
        /// Gets the index of this <see cref="AmbientPropertyInfo"/> inside the <see cref="StObjTypeInfo"/> collection into which it appears.
        /// </summary>
        public int Index { get { return _index; } }

        /// <summary>
        /// This is settable in order for final AmbientPropertyInfo at a given specialization level to 
        /// record the level of the first ancestor that defined it (regardless of the property type that - because
        /// of covariance support - can change from level to level).
        /// </summary>
        public int DefinerSpecializationDepth { get { return _definerSpecializationDepth; } }

        /// <summary>
        /// Gets the property to use to set the value (it corresponds to the top definer).
        /// </summary>
        public PropertyInfo SettablePropertyInfo { get { return _settablePropertyInfo; } }

        public virtual string Kind
        {
            get { return KindName; }
        }

        void SetTopDefinerSettablePropertyInfo( IActivityMonitor monitor )
        {
            var mSet = _p.GetSetMethod( true );
            if( mSet == null )
            {
                monitor.Error().Send( "Property '{0}.{1}' must have a setter (since it is the first declaration of the property).", DeclaringType.FullName, Name );
            }
            _settablePropertyInfo = _p;
        }

        protected virtual void SetGeneralizationInfo( IActivityMonitor monitor, CovariantPropertyInfo gen )
        {
            // Covariance ?
            if( PropertyType != gen.PropertyType && !gen.PropertyType.IsAssignableFrom( PropertyType ) )
            {
                monitor.Error().Send( "Property '{0}.{1}' type is not compatible with base property '{2}.{1}'.", DeclaringType.FullName, Name, gen.DeclaringType.FullName );
            }
            else if( _p.GetSetMethod( true ) != null )
            {
                monitor.Warn().Send( "Property '{0}.{1}' should not have a setter (there should only be a getter that casts the base property).", DeclaringType.FullName, Name, gen.DeclaringType.FullName );
            }
            // Propagates the top first definer level.
            _definerSpecializationDepth = gen.DefinerSpecializationDepth;
            _settablePropertyInfo = gen._settablePropertyInfo;
        }

        static public IReadOnlyList<T> MergeWithAboveProperties<T>( IActivityMonitor monitor, IReadOnlyList<T> above, IList<T> collector ) where T : CovariantPropertyInfo
        {
            if( collector == null || collector.Count == 0 ) return above ?? CKReadOnlyListEmpty<T>.Empty;
            int nbFromAbove = 0;
            if( above != null )
            {
                // Adds 'above' into 'collector' before returning it.
                foreach( T a in above )
                {
                    T exists = null;
                    int idxExists = nbFromAbove;
                    while( idxExists < collector.Count && (exists = collector[idxExists]).Name != a.Name ) ++idxExists;
                    if( idxExists == collector.Count )
                    {
                        collector.Insert( nbFromAbove++, a );
                    }
                    else
                    {
                        exists.SetGeneralizationInfo( monitor, a );
                        collector.RemoveAt( idxExists );
                        exists._index = nbFromAbove;
                        collector.Insert( nbFromAbove++, exists );
                    }
                }
            }
            for( int i = nbFromAbove; i < collector.Count; ++i )
            {
                collector[i].SetTopDefinerSettablePropertyInfo( monitor );
            }
            return collector.ToReadOnlyList();
        }
    }
}
