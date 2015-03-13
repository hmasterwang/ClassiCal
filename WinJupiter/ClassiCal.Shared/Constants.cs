using System;
using System.Collections.Generic;
using System.Text;

namespace ClassiCal
{
    public sealed class Constants 
    {
        private const string serverAddr = "192.168.1.102:8080";

        public static string ServerAddr { get { return serverAddr; } }

        public static string GetChatroomAddr(string username, string crn)
        {
            return "ws://" + ServerAddr + "/chatserver/" + username + "/" + crn;
        }
    }
}
