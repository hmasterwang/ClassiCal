using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ClassiCal
{
    public class ForumThread
    {
        ForumMessage Topic { get; set; }
        ObservableCollection<ForumMessage> FollowUps { get; set; }
    }

    public class ForumMessage
    {
        string title { get; set; }
        string content { get; set; }
        string user { get; set; }
        DateTime datetime { get; set; }
        UInt64 id { get; set; }
        UInt64 parentId { get; set; }
        UInt32 upvote { get; set; }
    }
}
