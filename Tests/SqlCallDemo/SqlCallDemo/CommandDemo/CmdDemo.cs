using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlCallDemo.CommandDemo
{
    public class CmdDemo
    {
        public int ActorId { get; set; }

        public string CompanyName { get; set; }

        public DateTime LaunchnDate { get; set; }

        public class ResultPOCO
        {
            public int Delay { get; set; }

            public string ActualCompanyName { get; set; }
        }

        public class ResultReadOnly
        {
            public ResultReadOnly( int delay, string actualCompanyName )
            {
                Delay = delay;
                ActualCompanyName = actualCompanyName;
            }

            public int Delay { get; }

            public string ActualCompanyName { get; }
        }
    }
}
