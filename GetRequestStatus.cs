using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_Load_Test
{
    internal class GetRequestStatus
    {
        public List<int> numberOfCodes;

        public GetRequestStatus(List<int> numberOfCodes)
        {
            this.numberOfCodes = numberOfCodes;

        }
        public List<int> DetermineStatus(int statusCode)
        {
            if (statusCode >= 200 && statusCode <300)
            {
                numberOfCodes[0] = numberOfCodes[0] += 1;
                return numberOfCodes;
                
            }
            if (statusCode >= 300 && statusCode < 400)
            {
                numberOfCodes[1] = numberOfCodes[1] += 1;
                return numberOfCodes;
            }
            if (statusCode >= 400 && statusCode < 500)
            {
                numberOfCodes[2] = numberOfCodes[2] += 1;
                return numberOfCodes;
            }
            if (statusCode >= 500 && statusCode < 600)
            {
                numberOfCodes[3] = numberOfCodes[3] += 1;
                return numberOfCodes;
            }
            else
            {
                return numberOfCodes;
            }
        }
    }
}
