using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RserveLink;

namespace RServeClient
{
    public class RServeClient
    {
        public void Run()
        {
            using (var rconnection = new Rconnection("127.0.0.1", 6311))
            {
                rconnection.Connect();
                string printStatement = $@"print(""imran"")";

                var result = rconnection.Eval(printStatement);

                rconnection.Disconnect();
            }
        }
    }
}
