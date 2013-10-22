using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CK.Deploy.Console
{
    public class Ancestor
    {
        public class FinderResult
        {
            public string CommonPath { get; set; }
            public string CodeBaseRelativePath { get; set; }
            public string ProjectRootRelativePath { get; set; }
        }

        public static FinderResult FindCommonAncestor( DirectoryInfo codeBaseDir, DirectoryInfo projectRootDir )
        {
            List<DirectoryInfo> di = new List<DirectoryInfo>();
            var codeParent = codeBaseDir.Parent;
            while( codeParent != null )
            {
                di.Add( codeParent );
                codeParent = codeParent.Parent;
            }

            var projectParent = projectRootDir.Parent;
            while( projectParent != null )
            {
                if( di.Where( x => x.FullName == projectParent.FullName ).Count() != 0 )
                {
                    var common = di.Where( x => x.FullName == projectParent.FullName ).SingleOrDefault();
                    if( common != null )
                    {
                        var codeBase = codeBaseDir.FullName.Substring( common.FullName.Length );
                        var pojDir = projectRootDir.FullName.Substring( common.FullName.Length );
                        if( codeBase[0] == '\\' ) codeBase = codeBase.Substring( 1 );
                        if( pojDir[0] == '\\' ) pojDir = pojDir.Substring( 1 );
                        return new FinderResult()
                        {
                            CommonPath = common.FullName,
                            CodeBaseRelativePath = codeBase,
                            ProjectRootRelativePath = pojDir,
                        };
                    }
                }
                else
                {
                    projectParent = projectParent.Parent;
                }
            }
            return null;
        }
    }
}
