using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ClassiCal
{
    [DataContract]
    public class ChatContent : VMBase
    {
        private bool _sent = false;
        private bool _sendFailed = false;

        public bool IsMe { get; set; }
        public bool Sent { get { return _sent; } set { SetProperty(ref _sent, value); } }
        public bool SendFailed { get { return _sendFailed; } set { SetProperty(ref _sendFailed, value); } }
        
        [DataMember(Name = "user", Order = 0)]
        public string Sender { get; set; }

        public DateTime SentRecvTime { get; set; }

        [DataMember(Name = "content", Order = 1)]
        public string Content { get; set; }

        [DataMember(Name = "id", Order = 2)]
        UInt64 id { get; set; }

        [DataMember(Name = "parentId", Order = 3)]
        UInt64 parentID { get; set; }
    }
}
