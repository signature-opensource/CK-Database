using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class SqlObjectProtoItem : IDependentProtoItem
    {
        static public readonly string TypeView = "View";
        static public readonly string TypeProcedure = "Procedure";
        static public readonly string TypeFunction = "Function";

        /// <summary>
        /// Can be empty but not null.
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// Can be empty but not null.
        /// </summary>
        public string Schema { get; private set; }
        
        /// <summary>
        /// Never null nor empty.
        /// </summary>
        public string Name { get; private set; }           
        
        public string Header { get; private set; }

        public string FullName { get { return Schema + '.' + Name; } }
        public string Container { get; private set; }
        public Version Version { get; private set; }
        public string ItemType { get; private set; }
        public DependentItemType ItemKind { get { return DependentItemType.SimpleItem; } }
        public string Generalization { get { return null; } }
        public IEnumerable<string> Groups { get; private set; }
        public IEnumerable<string> Requires { get; private set; }
        public IEnumerable<string> RequiredBy { get; private set; }
        public IEnumerable<string> Children { get { return null; } }
        public IEnumerable<VersionedName> PreviousNames { get; private set; }
            
        public string TextAfterName { get; private set; }
            
        internal SqlObjectProtoItem(
                    string itemType,
                    string databaseName,
                    string schema,
                    string name,
                    string header,
                    Version v,
                    string packageName,
                    IEnumerable<string> requires,
                    IEnumerable<string> requiredBy,
                    IEnumerable<string> groups,
                    IEnumerable<VersionedName> prevNames,
                    string textAfterName )
        {
            ItemType = itemType;
            DatabaseName = databaseName;
            Schema = schema;
            Name = name;
            Header = header;
            Version = v;
            Container = packageName;
            Requires = requires;
            RequiredBy = requiredBy;
            Groups = groups;
            PreviousNames = prevNames;
            TextAfterName = textAfterName;
        }

        public SqlObjectItem CreateItem( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            SqlObjectItem result = null;
            if( ItemType == SqlObjectProtoItem.TypeProcedure )
            {
                result = new SqlProcedureItem( this );
            }
            else if( ItemType == SqlObjectProtoItem.TypeView )
            {
                result = new SqlViewItem( this );
            }
            else if( ItemType == SqlObjectProtoItem.TypeFunction )
            {
                result = new SqlFunctionItem( this );
            }
            else
            {
                logger.Error( "Unable to create item for '{0}', type '{1}' is unknown.", FullName, ItemType ); 
            }
            return result;
        }
    }
}
