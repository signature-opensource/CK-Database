#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Runtime\SqlObject\SqlObjectProtoItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using CK.Core;
using CK.Setup;
using CK.SqlServer.Parser;

namespace CK.SqlServer.Setup
{
    public class SqlObjectProtoItem : ISetupObjectProtoItem
    {
        static public readonly string TypeView = "View";
        static public readonly string TypeProcedure = "Procedure";
        static public readonly string TypeFunction = "Function";

        /// <summary>
        /// Exposes the <see cref="SqlContextLocName"/> of this proto item.
        /// </summary>
        public readonly SqlContextLocName ContextLocName;

        IContextLocNaming ISetupObjectProtoItem.ContextLocName
        {
            get { return ContextLocName; }
        }

        /// <summary>
        /// Gets whether missing dependency must be considered an error.
        /// </summary>
        public bool? MissingDependencyIsError { get; private set; }

        /// <summary>
        /// Gets the header (text before the object declaration).
        /// </summary>
        public string Header { get; private set; }

        /// <summary>
        /// Can be empty but not null.
        /// </summary>
        public string PhysicalDatabaseName { get; private set; }

        public string Container { get; private set; }

        /// <summary>
        /// Gets the version (null when '*' is used).
        /// </summary>
        public Version Version { get; private set; }
        
        public string ItemType { get; private set; }
        public DependentItemKind ItemKind { get { return DependentItemKind.Item; } }
        public string Generalization { get { return null; } }
        public IEnumerable<string> Groups { get; private set; }
        public IEnumerable<string> Requires { get; private set; }
        public IEnumerable<string> RequiredBy { get; private set; }
        public IEnumerable<string> Children { get { return null; } }
        public IEnumerable<VersionedName> PreviousNames { get; private set; }
            
        public string FullOriginalText { get; private set; }
            
        public string TextAfterName { get; private set; }

        internal SqlObjectProtoItem(
                    IContextLocNaming externalName,
                    string itemType,
                    string physicalDatabaseName,
                    string schema,
                    string name,
                    string header,
                    Version v,
                    string packageName,
                    bool? missingDependencyIsError,
                    IEnumerable<string> requires,
                    IEnumerable<string> requiredBy,
                    IEnumerable<string> groups,
                    IEnumerable<VersionedName> prevNames,
                    string textAfterName,
                    string fullOriginalText )
        {
            Debug.Assert( externalName != null );
            Debug.Assert( schema == null || schema.Length > 0, "Schema exists or is unknown, not empty." );
            
            // The fact that external.Name must be equal to this Name (based on the content) is checked
            // by the caller (it can then give more information such as the resource location).
            ContextLocName = new SqlContextLocName( externalName, schema, name );
            ItemType = itemType;
            PhysicalDatabaseName = physicalDatabaseName;
            Header = header;
            Version = v;
            Container = packageName;
            Requires = requires;
            RequiredBy = requiredBy;
            Groups = groups;
            PreviousNames = prevNames;
            TextAfterName = textAfterName;
            FullOriginalText = fullOriginalText;
            MissingDependencyIsError = missingDependencyIsError;
        }

        public SqlProcedureItem CreateProcedureItem( IActivityMonitor monitor )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            if( ItemType != SqlObjectProtoItem.TypeProcedure ) throw new InvalidOperationException( "Not a procedure." );
            try
            {
                SqlExprStStoredProc sp;
                var error = SqlAnalyser.ParseStatement( out sp, FullOriginalText );
                error.LogOnError( monitor );
                return new SqlProcedureItem( this, sp );
            }
            catch( Exception ex )
            {
                using( monitor.OpenError().Send( ex, "While parsing {0}.", ContextLocName.FullName ) )
                {
                    monitor.Info().Send( FullOriginalText );
                }
                return null;
            }
        }

        public SqlObjectItem CreateItem( IActivityMonitor monitor, bool defaultMissingDependencyIsError, SqlPackageBaseItem package = null )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            SqlObjectItem result = null;
            if( ItemType == SqlObjectProtoItem.TypeProcedure )
            {
                result = CreateProcedureItem( monitor );
            }
            else if( ItemType == SqlObjectProtoItem.TypeView )
            {
                result = new SqlViewObjectItem( this );
            }
            else if( ItemType == SqlObjectProtoItem.TypeFunction )
            {
                result = new SqlFunctionItem( this );
            }
            else
            {
                monitor.Error().Send( "Unable to create item for '{0}', type '{1}' is unknown.", ContextLocName.FullName, ItemType ); 
            }
            if( result != null )
            {
                if( !result.MissingDependencyIsError.HasValue ) result.MissingDependencyIsError = defaultMissingDependencyIsError;
                if( package != null ) package.EnsureObjectsPackage().Children.Add( result );
            }
            return result;
        }
    }
}
