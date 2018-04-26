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
        public void Run(string script)
        {
            try
            {
                using (var rconnection = new Rconnection("127.0.0.1", 6311))
                {
                    rconnection.Connect();

                    var result = rconnection.Eval(script);

                    rconnection.Disconnect();
                }
            }
            catch (RserveLink.RconnectionException myException)
            {
                var message = myException.Message;
                throw;
            }
        }
    }
}
