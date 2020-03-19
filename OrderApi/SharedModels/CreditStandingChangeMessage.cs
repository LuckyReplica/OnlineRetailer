using System;
using System.Collections.Generic;
using System.Text;

namespace SharedModels
{
    public class CreditStandingChangeMessage
    {
        public int ClientId { get; set; }

        public bool CreditStanding { get; set; }
    }
}
