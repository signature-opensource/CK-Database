using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Reflection;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    public partial class SqlProcedureAttributeImpl
    {
        class SqlCallContextInfo
        {
            readonly List<Property> _props;

            public class Property
            {
                public readonly ParameterInfo Parameter;
                public readonly PropertyInfo Prop;

                public Property( ParameterInfo param, PropertyInfo prop )
                {
                    Parameter = param;
                    Prop = prop;
                }

                internal bool Match( SqlExprParameter sqlP, IActivityMonitor monitor )
                {
                    if( StringComparer.OrdinalIgnoreCase.Equals( '@' + Prop.Name, sqlP.Variable.Identifier.Name ) )
                    {
                        if( CheckParameterType( Prop.PropertyType, sqlP, monitor ) )
                        {
                            monitor.Info().Send( "Sql Parameter '{0}' will take its value from the SqlCallContext parameter '{1}' property '{2}'.", sqlP.ToStringClean(), Parameter.Name, Prop.Name );
                            return true;
                        }
                    }
                    return false;
                }
            }

            public SqlCallContextInfo()
            {
                _props = new List<Property>();
            }

            public bool Add( ParameterInfo param, IActivityMonitor monitor )
            {
                if( param.ParameterType.IsInterface )
                {
                    _props.AddRange( ReflectionHelper.GetFlattenProperties( param.ParameterType ).Select( p => new Property( param, p ) ) );
                }
                else _props.AddRange( param.ParameterType.GetProperties().Select( p => new Property( param, p ) ) );
                return true;
            }

            public bool FindMatchingProperty( SqlParametersSetter.Setter setter, IActivityMonitor monitor )
            {
                foreach( var p in _props )
                {
                    if( p.Match( setter.SqlExprParam, monitor ) )
                    {
                        setter.SetParameterMapping( p );
                        return true;
                    }
                }
                return false;
            }

            static public bool IsSqlCallContext( ParameterInfo mP )
            {
                Type t = mP.ParameterType;
                return typeof( ISqlCallContext ).IsAssignableFrom( t ) || Attribute.IsDefined( t, typeof( SqlCallContextAttribute ), true );
            }
        }

    }
}
