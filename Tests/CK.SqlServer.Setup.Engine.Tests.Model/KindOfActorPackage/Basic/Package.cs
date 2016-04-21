#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests.Model\KindOfActorPackage\Basic\Package.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using CK.Core;
using CK.Setup;
using CK.SqlServer;
using CK.SqlServer.Setup;

namespace SqlActorPackage.Basic
{

    [SqlPackage( Schema = "CK", Database = typeof( SqlDefaultDatabase ), ResourcePath = "Res" ), Versions( "2.11.25" )]
    [SqlObjectItem( "OneActorView" )]
    public abstract class Package : SqlPackage, IKnowTheConnectionString
    {
        [InjectContract]
        public UserHome UserHome { get; protected set; }
        
        [InjectContract]
        public GroupHome GroupHome { get; protected set; }

        [SqlProcedure( "sBasicSimpleProcedure" )]
        public abstract SqlCommand SimpleProcedureNaked( int index, string name, out string result );

       
        #region Command injection (Connection & transaction)

        /// <summary>
        /// When a SqlConnection appears, it is set onto the SqlCommand.
        /// Only the first SqlConnection is considered: if two connection parameters exist, this will raise an error since we return a
        /// simple SqlCommand object.
        /// </summary>
        [SqlProcedure( "sBasicSimpleProcedure" )]
        public abstract SqlCommand SimpleProcedureWithConnection( SqlConnection c, int index, string name, out string result );

        /// <summary>
        /// When a SqlTransaction appears, it is set onto the SqlCommand, and its Connection is also automatically sets onto the SqlCommand.
        /// Only the first SqlTransaction is considered: if two transaction parameters exist, this will raise an error since we return a
        /// simple SqlCommand object.
        /// </summary>
        [SqlProcedure( "sBasicSimpleProcedure" )]
        public abstract SqlCommand SimpleProcedureWithTransaction( SqlTransaction t, int index, string name, out string result );

        /// <summary>
        /// The transaction and/or connection can appear anywhere: only the first of them are set onto the SqlCommand.
        /// When the connection parameter is null, the one of the transaction is automatically used.
        /// If both are null, the SqlCommand.Connection and Transaction properties are let to null (and set to null if the SqlCommand 
        /// is the first parameter by reference).
        /// </summary>
        [SqlProcedure( "sBasicSimpleProcedure" )]
        public abstract SqlCommand SimpleProcedureWithConnectionAndTransaction( int index, SqlTransaction t, string name, out string result, SqlConnection c );

        #endregion

        string IKnowTheConnectionString.GetConnectionString()
        {
            return Database.ConnectionString;
        }

        #region Command Wrapper

        public class SimplestScalarCmd<T> : IDisposable
        {
            readonly SqlCommand _cmd;

            public SimplestScalarCmd( SqlCommand cmd )
            {
                _cmd = cmd;
            }

            public T Execute()
            {
                return (T)_cmd.ExecuteScalar();
            }

            public void Dispose()
            {
                _cmd.Dispose();
            }
        }

        public class ScalarCmdWithAccessToHome<T> : IDisposable
        {
            readonly SqlCommand _cmd;
            readonly Package _package;

            public ScalarCmdWithAccessToHome( SqlCommand cmd, Package package )
            {
                _cmd = cmd;
                _package = package;
            }

            public T Execute()
            {
                using( var c = new SqlConnection( _package.Database.ConnectionString ) )
                {
                    c.Open();
                    _cmd.Connection = c;
                    return (T)_cmd.ExecuteScalar();
                }
            }

            public void Dispose()
            {
                _cmd.Dispose();
            }
        }

        public class ScalarCmdWithAccessToABaseOfTheHome<T> : IDisposable
        {
            readonly SqlCommand _cmd;
            readonly SqlPackageBase _package;

            public ScalarCmdWithAccessToABaseOfTheHome( SqlCommand cmd, SqlPackageBase package )
            {
                _cmd = cmd;
                _package = package;
            }

            public T Execute()
            {
                using( var c = new SqlConnection( _package.Database.ConnectionString ) )
                {
                    c.Open();
                    _cmd.Connection = c;
                    return (T)_cmd.ExecuteScalar();
                }
            }

            public void Dispose()
            {
                _cmd.Dispose();
            }
        }

        public class ScalarCmdWithAccessToInterfaceOnHome<T> : IDisposable
        {
            readonly SqlCommand _cmd;
            readonly IKnowTheConnectionString _c;
            
            public readonly int Timeout;

            public readonly string LogMessage1;
            public readonly string LogMessage2;

            public ScalarCmdWithAccessToInterfaceOnHome( SqlCommand cmd, IKnowTheConnectionString c )
            {
                _cmd = cmd;
                _c = c;
                Timeout = -1;
            }

            public ScalarCmdWithAccessToInterfaceOnHome( SqlCommand cmd, IKnowTheConnectionString c, int msTimeout )
            {
                _cmd = cmd;
                _c = c;
                Timeout = msTimeout;
            }

            public ScalarCmdWithAccessToInterfaceOnHome( SqlCommand cmd, IKnowTheConnectionString c, int msTimeout, string logMsg1, string logMsg2 )
            {
                _cmd = cmd;
                _c = c;
                Timeout = msTimeout;
                LogMessage1 = logMsg1;
                LogMessage2 = logMsg2;
            }

            public T Execute()
            {
                using( var c = new SqlConnection( _c.GetConnectionString() ) )
                {
                    c.Open();
                    _cmd.Connection = c;
                    return (T)_cmd.ExecuteScalar();
                }
            }

            public void Dispose()
            {
                _cmd.Dispose();
            }
        }

        /// <summary>
        /// The SqlConnection is set onto the SqlCommand.
        /// </summary>
        [SqlProcedure( "sBasicSimpleScalar" )]
        public abstract SimplestScalarCmd<string> SimplestScalar( SqlConnection c, int index, string name );

        /// <summary>
        /// The SqlConnection is built by the wrapper bases on the Package.Database.ConnectionString property.
        /// </summary>
        [SqlProcedure( "sBasicSimpleScalar" )]
        public abstract ScalarCmdWithAccessToHome<string> SimplestScalarWithAccessToHome( int index, string name );

        /// <summary>
        /// Base classes are compatible (here again the SqlConnection is built by the wrapper bases on the SqlPackageBase.Database.ConnectionString property).
        /// </summary>
        [SqlProcedure( "sBasicSimpleScalar" )]
        public abstract ScalarCmdWithAccessToABaseOfTheHome<string> SimplestScalarAccessToABaseOfTheHome( int index, string name );

        /// <summary>
        /// Supported interfaces are compatible (here again the SqlConnection is built by the wrapper bases on the IKnowTheConnectionString interface that is implemented by this package).
        /// </summary>
        [SqlProcedure( "sBasicSimpleScalar" )]
        public abstract ScalarCmdWithAccessToInterfaceOnHome<string> SimplestScalarAccessToInterfaceHome( int index, string name );

        /// <summary>
        /// Parameters that do not match the stored procedures are mapped (if possible) to the wrapper constructor.
        /// Name is used to disambiguate only if needed. Here, there is only one 'int' in the longest constructor, so exact naming is not required (this only generates a warning). 
        /// </summary>
        [SqlProcedure( "sBasicSimpleScalar" )]
        public abstract ScalarCmdWithAccessToInterfaceOnHome<string> SimplestScalarWithTimeout( int index, string name, int noAmbiguityTimeoutIsNamedAsYouLike );

        /// <summary>
        /// Parameters that do not match the stored procedures are mapped (if possible) to the wrapper constructor.
        /// Extra parameters (the ones that are not mapped to sql stored procedure) can appear in any order. 
        /// Only sql parameters must respect the order stored procedure definition. 
        /// </summary>
        [SqlProcedure( "sBasicSimpleScalar" )]
        public abstract ScalarCmdWithAccessToInterfaceOnHome<string> SimplestScalarWithLogParams( string logMsg1, int index, string name, int msTimeout, string logMsg2 );


        #endregion

        #region SqlCallContext

        public interface IAmHereToTestPropertyMasking
        {
            int ActorId { get; set; }
        }

        public interface IBasicAuthContext : IAmHereToTestPropertyMasking, IDisposable
        {
            SqlConnectionProvider GetProvider( string connectionString );

            new int ActorId { get; set; }
        }

        public interface IAuthContext : IBasicAuthContext
        {
            int SecurityZoneId { get; set; }
        }

        public class BasicAuthContext : IAuthContext
        {
            object _cache;

            public int ActorId { get; set; }
            
            public int SecurityZoneId { get; set; }

            #region ISqlCallContext Members

            public SqlConnectionProvider GetProvider( string connectionString )
            {
                SqlConnectionProvider c;
                if( _cache == null )
                {
                    c = new SqlConnectionProvider( connectionString );
                    _cache = c;
                    return c;
                }
                SqlConnectionProvider newC;
                c = _cache as SqlConnectionProvider;
                if( c != null )
                {
                    if( c.ConnectionString == connectionString ) return c;
                    newC = new SqlConnectionProvider( connectionString );
                    _cache = new SqlConnectionProvider[] { c, newC };
                }
                else
                {
                    SqlConnectionProvider[] cache = (SqlConnectionProvider[])_cache;
                    for( int i = 0; i < cache.Length; i++ )
                    {
                        c = cache[i];
                        if( c.ConnectionString == connectionString ) return c;
                    }
                    SqlConnectionProvider[] newCache = new SqlConnectionProvider[cache.Length + 1];
                    Array.Copy( cache, newCache, cache.Length );
                    newC = new SqlConnectionProvider( connectionString );
                    newCache[cache.Length] = newC;
                    _cache = newCache;
                }
                return newC;
            }

            public void Dispose()
            {
                if( _cache != null )
                {
                    SqlConnectionProvider c = _cache as SqlConnectionProvider;
                    if( c != null ) c.Dispose();
                    else
                    {
                        SqlConnectionProvider[] cache = _cache as SqlConnectionProvider[];
                        for( int i = 0; i < cache.Length; ++i ) cache[i].Dispose();
                    }
                    _cache = null;
                }
            }
            
            #endregion
        }

        public class OutputCmd<T> : IDisposable
        {
            readonly SqlCommand _cmd;
            readonly SqlPackageBase _p;

            public OutputCmd( SqlCommand cmd, SqlPackageBase p )
            {
                _cmd = cmd;
                _p = p;
            }

            public T Call()
            {
                using( var c = new SqlConnection( _p.Database.ConnectionString ) )
                {
                    c.Open();
                    _cmd.Connection = c;
                    _cmd.ExecuteNonQuery();
                    var outP = _cmd.Parameters.Cast<SqlParameter>().Single( p => p.Direction == System.Data.ParameterDirection.Output );
                    return (T)outP.Value;
                }
            }

            public void Dispose()
            {
                _cmd.Dispose();
            }
        }


        [SqlProcedure( "sBasicProcedureWithAuth" )]
        public abstract OutputCmd<string> CallWithAuth( [ParameterSource]IAuthContext c, int index, string name, out string result );

        [SqlProcedure( "sBasicProcedureWithAuth" )]
        public abstract OutputCmd<string> CallWithAuth( [ParameterSource]IBasicAuthContext c, int index, string name );

        [SqlProcedure( "sBasicProcedureWithAuth" )]
        public abstract OutputCmd<T> CallWithAuth<T>( [ParameterSource]IAuthContext c, int index, string name, out string result );

        #endregion
    }
}
