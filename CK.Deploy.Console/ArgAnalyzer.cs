#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Deploy.Console\ArgAnalyzer.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CK.Deploy.Console
{
    public class V1Args
    {
        public string FilePath { get; set; }
        public string ConnectionString { get; set; }
    }

    [Serializable]
    public class V2Args : MarshalByRefObject
    {
        public V2Args()
        {
            RelativeFilePaths = new List<string>();
            RelativeDllPaths = new List<string>();
            AssemblyNames = new List<string>();
        }
        public string AbsoluteRootPath { get; set; }
        public List<string> RelativeFilePaths { get; set; }
        public List<string> RelativeDllPaths { get; set; }
        public List<string> AssemblyNames { get; set; }
        public string ConnectionString { get; set; }
        public string LogPath { get; set; }
    }

    public class ArgsAnalyzer
    {
        private readonly string[] _originalArgs;

        public ArgsAnalyzer( string[] args )
        {
            _originalArgs = args;
        }

        public string[] ParsePath( string arg, bool changeSlash )
        {
            List<string> s = new List<string>();
            var paths = arg.Split( ';' );
            foreach( var item in paths )
            {
                var temp = item.Trim( '"' );
                if( changeSlash ) temp = temp.Replace( '/', System.IO.Path.DirectorySeparatorChar );
                if( !string.IsNullOrEmpty( temp ) ) s.Add( temp );
            }
            return s.ToArray();
        }

        public string Analyze()
        {
            StringBuilder result = new StringBuilder();

            if( _originalArgs.Length > 0 && _originalArgs[0] == "-v2" ) IsV2 = true;

            if( IsV2 )
            {
                if( _originalArgs.Length >= 6 )
                {
                    var args = new V2Args()
                    {
                        AbsoluteRootPath = _originalArgs[1].Trim( '"' ).Replace( '/', System.IO.Path.DirectorySeparatorChar ),
                        ConnectionString = _originalArgs[5].Trim( '"' )
                    };
                    args.RelativeFilePaths.AddRange( ParsePath( _originalArgs[2].Replace( "fPath=", "" ), true ) );
                    args.RelativeDllPaths.AddRange( ParsePath( _originalArgs[3].Replace( "dllPath=", "" ), true ) );
                    args.AssemblyNames.AddRange( ParsePath( _originalArgs[4], false ) );

                    if( _originalArgs.Length == 7 )
                        args.LogPath = _originalArgs[6].Trim( '"' ).Replace( '/', System.IO.Path.DirectorySeparatorChar );
                    else
                    {
                        args.LogPath = Path.Combine( Environment.CurrentDirectory, "Logs" );
                    }
                    if( !string.IsNullOrEmpty( args.AbsoluteRootPath ) &&
                        !string.IsNullOrEmpty( args.ConnectionString ) &&
                        (args.RelativeFilePaths.Count > 0 || (args.AssemblyNames.Count > 0 && args.RelativeDllPaths.Count > 0)) )
                    {
                        V2Args = args;
                        IsValid = true;
                    }
                    else
                    {
                        if( args.RelativeFilePaths.Count == 0 && args.RelativeDllPaths.Count == 0 )
                        {
                            result.AppendLine( "Invalide argument fPath or dllPath : there is no fPath or dllPath in args" );
                        }
                        if( args.AssemblyNames.Count > 0 && args.RelativeDllPaths.Count == 0 )
                        {
                            result.AppendLine( "Invalide argument dllPath or dllAssembly : there is some dllAssembly but no dllPath in args" );
                        }
                        if( string.IsNullOrEmpty( args.AbsoluteRootPath ) )
                        {
                            result.AppendLine( "Invalide argument <rootAbsolutePath>" );
                        }
                        if( string.IsNullOrEmpty( args.ConnectionString ) )
                        {
                            result.AppendLine( "Invalide argument <ConnectionString>" );
                        }
                    }
                }
                else
                {
                    IsValid = false;
                    result.AppendLine( string.Format( "Not enough arguments, only {0} but need 6 arguments", _originalArgs.Length ) );
                }
            }
            else
            {
                if( _originalArgs.Length != 2 )
                {
                    var args = new V1Args()
                    {
                        FilePath = _originalArgs[0].Trim( '"' ).Replace( '/', System.IO.Path.DirectorySeparatorChar ),
                        ConnectionString = _originalArgs[1].Trim( '"' )
                    };
                    if( !string.IsNullOrEmpty( args.FilePath ) && !string.IsNullOrEmpty( args.ConnectionString ) )
                    {
                        V1Args = args;
                        IsValid = true;
                    }
                    else if( string.IsNullOrEmpty( args.FilePath ) )
                    {
                        result.AppendLine( "Invalide argument : <path> is null or empty" );
                    }
                    else if( string.IsNullOrEmpty( args.ConnectionString ) )
                    {
                        result.AppendLine( "Invalide argument : <connectionString> is null or empty" );
                    }
                }
                else
                {
                    result.AppendLine( string.Format( "Not enough arguments, only {0} but need 2 arguments", _originalArgs.Length ) );
                }
            }

            return result.ToString();
        }

        public bool IsValid { get; private set; }
        public bool IsV2 { get; private set; }
        public V1Args V1Args { get; private set; }
        public V2Args V2Args { get; private set; }
    }

}
