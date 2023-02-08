using System;
using System.Collections.Generic;
using System.Text;

namespace MedFasee.Repository
{
    public abstract class Database 
    {
        public string Ip { get; }
        public string User { get; }
        public string Pass { get; }

        public Database(string ip, string user, string pass)
        {
            Ip = ip;
            User = user;
            Pass = pass;
        }
    }
}
