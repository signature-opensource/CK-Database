using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace CK.Setup
{
    public sealed partial class SetupEngine
    {
        static internal T GetSetupEngineAspect<T>( IReadOnlyList<ISetupEngineAspect> aspects, bool required = true ) where T : class
        {
            T a = aspects.OfType<T>().FirstOrDefault();
            if( a == null && required ) throw new CKException( "Aspect '{0}' is required. Did you forget to register an aspect configuration in the SetupEngineConfiguration.Aspects list?", typeof( T ).FullName );
            return a;
        }

        bool CreateEngineAspectsFromConfiguration()
        {
            bool success = true;
            using( _monitor.OpenTrace().Send( "Creating and configuring {0} aspect(s).", _config.Aspects.Count ) )
            {
                var aspectsType = new HashSet<Type>();
                foreach( var c in _config.Aspects )
                {
                    string name = null;
                    try
                    {
                        name = c.AspectType;
                        Type t = SimpleTypeFinder.WeakDefault.ResolveType( c.AspectType, true );
                        if( !aspectsType.Add( t ) )
                        {
                            success = false;
                            _monitor.Error().Send( "Aspect '{0}' occurs more than once in configuration." );
                        }
                        else _startConfiguration.AddAspect( (ISetupEngineAspect)Activator.CreateInstance( t, this, c ) );
                    }
                    catch( Exception ex )
                    {
                        success = false;
                        _monitor.Error().Send( ex, "While creating aspect '{0}'.", name );
                    }
                }
                if( success )
                {
                    foreach( var a in _startConfiguration.Aspects )
                    {
                        using( _monitor.OpenTrace().Send( "Configuring aspect '{0}'.", a.GetType().FullName ) )
                        {
                            try
                            {
                                success |= a.Configure();
                            }
                            catch( Exception ex )
                            {
                                success = false;
                                _monitor.Error().Send( ex );
                                break;
                            }
                        }
                    }
                }
            }
            return success;
        }

        void DisposeDisposableAspects()
        {
            foreach( var aspect in _startConfiguration.Aspects.OfType<IDisposable>() )
            {
                try
                {
                    aspect.Dispose();
                }
                catch( Exception ex )
                {
                    _monitor.Error().Send( ex, "While disposing Aspect '{0}'.", aspect.GetType().AssemblyQualifiedName );
                }
            }
        }

    }
}
