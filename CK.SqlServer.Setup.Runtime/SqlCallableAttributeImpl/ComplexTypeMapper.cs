using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Reflection;
using CK.Text;
using CK.CodeGen;
using CK.CodeGen.Abstractions;

namespace CK.SqlServer.Setup
{
    class ComplexTypeMapperModel
    {
        public readonly Type CreatedType;

        class Param
        {
            public readonly string Name;
            public readonly Type Type;
            public readonly int Index;
            public readonly bool IsSimpleType;
            public int InputIndex;

            public Param( string name, Type type, int index )
            {
                Name = name;
                Type = type;
                Index = index;
                InputIndex = -1;
                IsSimpleType = SqlCallableAttributeImpl.IsNetTypeMapped( type );
            }

            public void EmitGetValue( ILGenerator g, Action<int, Type> getValueGenerator )
            {
                getValueGenerator( InputIndex, Type );
            }
        }

        class Mapped
        {
            public readonly IReadOnlyList<Param> Parameters;

            protected Mapped( IReadOnlyList<Param> parameters )
            {
                Parameters = parameters;
            }

            public bool IsInputSatisfied
            {
                get { return Parameters.All( p => p.InputIndex != -1 ); }
            }
        }

        class MappedCtor : Mapped
        {
            public readonly ConstructorInfo Ctor;
            public readonly ParameterInfo[] CtorParameters;

            public MappedCtor( ConstructorInfo c, ParameterInfo[] ctorParameters )
                : base( ctorParameters.Select( ( p, i ) => new Param( p.Name, p.ParameterType, i ) ).ToArray() )
            {
                Ctor = c;
                CtorParameters = ctorParameters;
            }
        }

        class MappedProperty : Mapped
        {
            public readonly PropertyInfo Property;

            public MappedProperty( PropertyInfo info )
                : base( new[] { new Param( info.Name, info.PropertyType, 0 ) } )
            {
                Property = info;
            }
        }

        struct UnmappedInput
        {
            public readonly int Index;
            public readonly string Name;
            public readonly string Type;

            public UnmappedInput( int i, string n, string t )
            {
                Index = i;
                Name = n;
                Type = t;
            }
        }

        IReadOnlyList<MappedCtor> _ctors;
        MappedCtor _selectedCtor;
        IReadOnlyList<MappedProperty> _props;
        List<UnmappedInput> _unmappedInputs;
        HashSet<int> _mappedInputIsSelectedCtor;

        public ComplexTypeMapperModel( Type t )
        {
            CreatedType = t;
            _ctors = t.GetConstructors( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).Select( c => new MappedCtor( c, c.GetParameters() ) ).ToArray();
            _props = t.GetProperties()
                            .Where( p => p.CanWrite && p.GetSetMethod() != null )
                            .Select( p => new MappedProperty( p ) ).ToArray();
        }

        IEnumerable<Mapped> AllMapped { get { return ((IEnumerable<Mapped>)_ctors).Concat( _props ); } }


        /// <summary>
        /// Registers an input. Name or typeMatcher can be null (but not both at the same time). 
        /// </summary>
        /// <param name="index">Zero based index of the input. Must be positive.</param>
        /// <param name="name">Name of th input (can be null).</param>
        /// <param name="typeMatcher">
        /// Type matcher for the input (can be null).
        /// When the actual type is known (inputType), it is typically the revert of <see cref="T:Type.IsAssignableFrom"/>, this lambda is fine: t => t.IsAsignableFrom( inputType ).
        /// </param>
        /// <param name="inputTypeName">Optional string with the type name used for warnings and errors.</param>
        /// <param name="shouldBeMapped">False to state that the input is not considered sensitive regarding the mapping.</param>
        /// <returns>True if the input has been mapped at least once.</returns>
        public bool AddInput( int index, string name, Func<Type,bool> typeMatcher, string inputTypeName, bool shouldBeMapped = true )
        {
            Debug.Assert( index >= 0 );
            if( DoAdd( index, name, typeMatcher, AllMapped ) ) return true;
            if( shouldBeMapped )
            {
                if( _unmappedInputs == null ) _unmappedInputs = new List<UnmappedInput>();
                _unmappedInputs.Add( new UnmappedInput( index, name, inputTypeName ) );
            }
            return false;
        }


        /// <summary>
        /// Registers an input. Name or type can be null (but not both at the same time). 
        /// </summary>
        /// <param name="index">Zero based index of the input. Must be positive.</param>
        /// <param name="name">Name of th input (can be null).</param>
        /// <param name="type">Type matcher for the input (can be null).</param>
        /// <param name="shouldBeMapped">False to state that the input is not considered sensitive regarding the mapping.</param>
        /// <returns>True if the input has been mapped at least once.</returns>
        public bool AddInput( int index, string name, Type type, bool shouldBeMapped = true )
        {
            Func<Type,bool> matcher = type != null 
                                        ? delegate( Type t ) { return t.IsAssignableFrom( type ); }
                                        : (Func<Type,bool>)null;
            return AddInput( index, name, matcher, type != null ? type.Name : null, shouldBeMapped );
        }

        bool DoAdd( int index, string name, Func<Type, bool> typeMatcher, IEnumerable<Mapped> mappings )
        {
            Debug.Assert( typeMatcher != null || name != null );
            bool found = false;
            foreach( var m in mappings )
            {
                foreach( var p in m.Parameters )
                {
                    if( typeMatcher == null || typeMatcher( p.Type ) )
                    {
                        if( name == null || StringComparer.OrdinalIgnoreCase.Equals( p.Name, name ) )
                        {
                            found = true;
                            p.InputIndex = index;
                        }
                    }
                }
            }
            return found;
        }

        bool ChooseCtor( IActivityMonitor monitor )
        {
            Debug.Assert( _selectedCtor == null );
            foreach( var c in _ctors )
            {
                if( c.IsInputSatisfied )
                {
                    if( _selectedCtor == null ) _selectedCtor = c;
                    else
                    {
                        if( _selectedCtor.Parameters.Count < c.Parameters.Count )
                        {
                            _selectedCtor = c;
                        }
                        else if( _selectedCtor.Parameters.Count == c.Parameters.Count )
                        {
                            monitor.Error( $"Ambiguous constructors: both '{SqlCallableAttributeImpl.DumpParameters( _selectedCtor.CtorParameters, true )}' and '{SqlCallableAttributeImpl.DumpParameters( c.CtorParameters, true )}' are satisfied." );
                            return false;
                        }
                    }
                }
            }
            if( _selectedCtor == null )
            {
                monitor.Error( "Unable to find a constructor." );
                return false;
            }
            _mappedInputIsSelectedCtor = new HashSet<int>();
            foreach( var p in _selectedCtor.Parameters )
            {
                if( p.InputIndex != -1 ) _mappedInputIsSelectedCtor.Add( p.InputIndex );
            }
            return true;
        }

        public bool CheckValidity( IActivityMonitor monitor )
        {
            if( !ChooseCtor( monitor ) ) return false;
            var unmappedProperties = _props.Where( p => !p.IsInputSatisfied ).ToList();
            if( unmappedProperties.Count != 0 &&_unmappedInputs != null )
            {
                using( monitor.OpenWarn( $"There are {unmappedProperties.Count} unmapped property(ie)s and {_unmappedInputs.Count} unmapped input(s)." ) )
                {
                    foreach( var p in unmappedProperties )
                    {
                        monitor.Info( $"Property '{p.Property.Name}.{p.Property.PropertyType.Name}' is not mapped." );
                    }
                    foreach( var i in _unmappedInputs )
                    {
                        if( i.Type == null )
                        {
                            monitor.Info( $"Input n°{i.Index} named '{i.Name}' is not mapped." );
                        }
                        else
                        {
                            monitor.Info( $"Input n°{i.Index} named '{i.Name}' of type '{i.Type}' is not mapped." );
                        }
                    }
                }
            }
            return true;
        }

        public void EmitNewObj( ILGenerator g, Action<int, Type> getValueGenerator )
        {
            Debug.Assert( _selectedCtor != null && _selectedCtor.IsInputSatisfied );
            foreach( var pCtor in _selectedCtor.Parameters )
            {
                pCtor.EmitGetValue( g, getValueGenerator );
            }
            g.Emit( OpCodes.Newobj, _selectedCtor.Ctor );
        }

        public void EmitPropertiesSet( ILGenerator g, Action<int, Type> getValueGenerator )
        {
            foreach( var pProp in _props )
            {
                if( pProp.IsInputSatisfied && !_mappedInputIsSelectedCtor.Contains( pProp.Parameters[0].InputIndex ) )
                {
                    g.Emit( OpCodes.Dup );
                    pProp.Parameters[0].EmitGetValue( g, getValueGenerator );
                    g.Emit( OpCodes.Callvirt, pProp.Property.GetSetMethod() );
                }
            }
        }

        public void EmitFullInitialization( ILGenerator g, Action<int, Type> getValueGenerator )
        {
            EmitNewObj( g, getValueGenerator );
            EmitPropertiesSet( g, getValueGenerator );
        }



        public string EmitFullInitialization( ICodeWriter b, Func<int, Type, string> getValueGenerator )
        {
            Debug.Assert( _selectedCtor != null && _selectedCtor.IsInputSatisfied );

            var ctorVariableNames = _selectedCtor.Parameters
                                                    .Select( pCtor => getValueGenerator( pCtor.InputIndex, pCtor.Type ) )
                                                    .ToArray();

            b.Append( "var oR = new " ).AppendCSharpName( CreatedType ).Append( "(" )
                .Append( ctorVariableNames )
                .Append( ");" )
                .NewLine();
            foreach( var pProp in _props )
            {
                if( pProp.IsInputSatisfied && !_mappedInputIsSelectedCtor.Contains( pProp.Parameters[0].InputIndex ) )
                {
                    var param = pProp.Parameters[0];
                    string varName = getValueGenerator( param.InputIndex, param.Type );
                    b.Append( "oR." ).Append( pProp.Property.Name).Append( " = " ).Append( varName ).Append( ";" ).NewLine();
                }
            }
            return "oR";
        }

    }

}
