using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using CK.Core;

namespace CK.Setup
{

    internal class AmbientPropertyOrContractInfo
    {
        readonly PropertyInfo _p;
        int _index;
        readonly bool _isOptionalDefined;
        bool _isOptional;

        internal AmbientPropertyOrContractInfo( PropertyInfo p, bool isOptionalDefined, bool isOptional, int definerSpecializationDepth, int index )
        {
            _p = p;
            _isOptionalDefined = isOptionalDefined;
            _isOptional = isOptional;
            DefinerSpecializationDepth = definerSpecializationDepth;
            Index = index;
            ContextAttribute c = (ContextAttribute)Attribute.GetCustomAttribute( p, typeof( ContextAttribute ), false );
            if( c != null ) Context = c.Context;
        }

        public string Name { get { return _p.Name; } }
        public Type PropertyType { get { return _p.PropertyType; } }
        public Type DeclaringType { get { return _p.DeclaringType; } }
        public PropertyInfo PropertyInfo { get { return _p; } }

        /// <summary>
        /// This is settable in order for base class property's context to be used if it is not explicitely defined
        /// by the specialized property.
        /// </summary>
        public string Context { get; private set; }

        /// <summary>
        /// This is settable in order for final AmbientPropertyInfo at a given specialization level to 
        /// record the level of the first ancestor that defined it (regardless of the property type that - because
        /// of covariance support - can change from level to level).
        /// </summary>
        public int DefinerSpecializationDepth { get; private set; }

        /// <summary>
        /// Gets the index of this <see cref="AmbientPropertyInfo"/> inside any <see cref="StObjTypeInfo"/>.<see cref="AmbientProperties"/> into which it appears.
        /// </summary>
        public int Index { get { return _index; } private set { _index = value; } }

        public bool IsOptional { get { return _isOptional; } private set { _isOptional = value; } }

        /// <summary>
        /// An ambient property must be public or protected in order to be "specialized" either by overriding (for virtual ones)
        /// or by masking ('new' keyword in C#), typically to support covariance return type.
        /// The "Property Covariance" trick can be supported here because ambient properties are conceptually "read only" properties:
        /// they must be settable only to enable the framework (and no one else) to actually set their values.
        /// </summary>
        static public void CreateAmbientPropertyListForExactType( 
            IActivityLogger logger, 
            Type t, 
            int definerSpecializationDepth, 
            List<StObjPropertyInfo> stObjProperties, 
            out IList<AmbientPropertyInfo> apListResult )
        {
            Debug.Assert( stObjProperties != null );
            
            var properties = t.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly ).Where( p => !p.Name.Contains( '.' ) );
            apListResult = null;
            foreach( var p in properties )
            {
                StObjPropertyAttribute stObjAttr = (StObjPropertyAttribute)Attribute.GetCustomAttribute( p, typeof( StObjPropertyAttribute ), false );
                if( stObjAttr != null )
                {
                    string nP = String.IsNullOrEmpty( stObjAttr.PropertyName ) ? p.Name : stObjAttr.PropertyName;
                    Type tP = stObjAttr.PropertyType == null ? p.PropertyType : stObjAttr.PropertyType;
                    if( stObjProperties.Find( sp => sp.Name == nP ) != null )
                    {
                        logger.Error( "StObj property named '{0}' for '{1}' is defined more than once. It should be declared only once.", p.Name, p.DeclaringType.FullName );
                    }
                    else
                    {
                        stObjProperties.Add( new StObjPropertyInfo( nP, tP, p ) );
                    }
                    // Continue to detect Ambient properties. Properties that are both Ambient and StObj must be detected.
                }
                IAmbientPropertyOrContractAttribute ap = (AmbientPropertyAttribute)Attribute.GetCustomAttribute( p, typeof( AmbientPropertyAttribute ), false );
                IAmbientPropertyOrContractAttribute ac = (AmbientContractAttribute)Attribute.GetCustomAttribute( p, typeof( AmbientContractAttribute ), false );
                if( ac == null && ap == null ) continue;
                if( stObjAttr != null || (ac != null && ap != null) )
                {
                    logger.Error( "Property named '{0}' for '{1}' can not be both an Ambient (Contract or Property) and a StObj property.", p.Name, p.DeclaringType.FullName );
                    continue;
                }
                IAmbientPropertyOrContractAttribute attr = ac ?? ap;
                var mSet = p.GetSetMethod( true );
                var mGet = p.GetGetMethod( true );
                if( (mSet == null || mSet.IsPrivate) || (mGet == null || mGet.IsPrivate) )
                {
                    // Warning: not a "Property Covariance" compliant property since
                    // specialized classes will not be able to "override" its signature.
                    // Even if it is not the "Property Covariance" that is targeted, a private get or set
                    // implies a "slicing" of the (base) type that defeats the specialization paradigm (with Covariance).
                    string typeName = attr.IsAmbientProperty ? "Property" : "Contract";
                    logger.Error( "Property '{0}' of '{1}' can not be considered as an Ambient {2}. An Ambient {2} must be readable and writeable (no private setter or getter). Did you forget to make it public?", p.Name, p.DeclaringType.FullName, typeName );
                }
                else
                {
                    if( apListResult == null ) apListResult = new List<AmbientPropertyInfo>();
                    Debug.Assert( apListResult.Any( a => a.Name == p.Name ) == false, "No homonym properties in .Net framework." );
                    apListResult.Add( new AmbientPropertyInfo( p, attr.IsOptionalDefined, attr.IsOptional, definerSpecializationDepth, apListResult.Count ) );
                }
            }
        }

        static public IEnumerable<AmbientPropertyInfo> MergeAboveAmbientProperties( IActivityLogger logger, IEnumerable<AmbientPropertyInfo> above, IList<AmbientPropertyInfo> collector )
        {
            if( collector == null || collector.Count == 0 ) return above ?? ReadOnlyListEmpty<AmbientPropertyInfo>.Empty;
            if( above != null )
            {
                // Adds 'above' into 'collector' before returning it.
                int nbFromAbove = 0;
                foreach( AmbientPropertyInfo a in above )
                {
                    AmbientPropertyInfo exists = null;
                    int idxExists = nbFromAbove;
                    while( idxExists < collector.Count && (exists = collector[idxExists]).Name != a.Name ) ++idxExists;
                    if( idxExists == collector.Count )
                    {
                        collector.Insert( nbFromAbove++, a );
                    }
                    else
                    {
                        // Covariance ?
                        if( exists.PropertyType != a.PropertyType && !a.PropertyType.IsAssignableFrom( exists.PropertyType ) )
                        {
                            logger.Error( "Ambient property '{0}.{1}' type is not compatible with base property '{2}.{1}'.", exists.DeclaringType.FullName, exists.Name, a.DeclaringType.FullName );
                        }
                        // A required property can not become optional.
                        if( exists.IsOptional && !a.IsOptional )
                        {
                            if( exists._isOptionalDefined )
                            {
                                logger.Error( "Ambient property '{0}.{1}' states that it is optional but base property '{2}.{1}' is required.", exists.DeclaringType.FullName, exists.Name, a.DeclaringType.FullName );
                            }
                            exists._isOptional = false;
                        }
                        // Context inheritance (if not defined).
                        if( exists.Context == null )
                        {
                            exists.Context = a.Context;
                        }
                        // Propagates the top first definer level.
                        exists.DefinerSpecializationDepth = a.DefinerSpecializationDepth;
                        // Captures the Generalization.
                        // We keep the fact that this property overrides one above (errors have been logged if conflict/incoherency occur).
                        // We can keep the Generalization but not a reference to the specialization since we are 
                        // not Contextualized here, but only on a pure Type level.
                        exists.Generalization = a;
                        //
                        collector.RemoveAt( idxExists );
                        exists.Index = nbFromAbove;
                        collector.Insert( nbFromAbove++, exists );

                    }
                }
            }
            return collector;
        }

        /// <summary>
        /// Recursive function to collect Ambient Properties on base types 
        /// </summary>
        static public IEnumerable<AmbientPropertyInfo> CreateAllAmbientPropertyList( Type type, int specializationLevel, IActivityLogger logger, List<StObjPropertyInfo> stObjProperties )
        {
            if( type == typeof( object ) ) return null;
            IList<AmbientPropertyInfo> apCollector;
            CreateAmbientPropertyListForExactType( logger, type, specializationLevel, stObjProperties, out apCollector );
            return MergeAboveAmbientProperties( logger, CreateAllAmbientPropertyList( type.BaseType, specializationLevel - 1, logger, stObjProperties ), apCollector );
        }

    }

}
