using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using System.Text.RegularExpressions;

namespace CK.Setup.Database.SqlServer
{
    public class SqlObjectBuilder : ISqlObjectBuilder
    {

        static Regex _rSqlObject = new Regex( @"(create|alter)\s+(?<1>proc(?:edure)?|function|view)\s+(\[?(?<2>\w+)]?\.)?\[?(?<3>\w+)]?",
                                            RegexOptions.CultureInvariant
                                            | RegexOptions.IgnoreCase
                                            | RegexOptions.ExplicitCapture );

        public SqlObjectPreParse PreParse( IActivityLogger logger, string text )
        {
            Match mSqlObject = _rSqlObject.Match( text );
            if( mSqlObject.Success )
            {
                return new SqlServerObjectPreParsed( text, text.Substring( 0, mSqlObject.Index ), mSqlObject );
            }
            logger.Error( "Unable to detect create or alter statement for view, procedure or function (the object name must be Schema.Name)." );
            return null;
        }

        public ISetupableItem Create( IActivityLogger logger, SqlObjectPreParse preParsed, SetupableItemData setupData )
        {
            SqlServerObjectPreParsed pre = (SqlServerObjectPreParsed)preParsed;
            if( setupData.FullName != pre.FullName )
            {
                logger.Error( "Name from the file is '{0}' whereas content indicates '{1}'. Names must match.", setupData.FullName, pre.FullName );
                return null;
            }
            return new SqlObject( setupData, pre );
        }

    }
}
