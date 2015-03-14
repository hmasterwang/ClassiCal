using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace ClassiCal
{
    public class ChatRoomViewModel : VMBase
    {
        private ObservableCollection<ChatContent> _chatHistory = new ObservableCollection<ChatContent>();
        private ChatRoomModel _chatroomModel;
        public ObservableCollection<ChatContent> ChatHistory { get { return _chatHistory; } }
        public CoreDispatcher Dispatcher { get; set; }
        private ChatRoomModel.ConnectionState _connectionState = ChatRoomModel.ConnectionState.Disconnected;
        public ChatRoomModel.ConnectionState ConnectionState
        {
            get { return _connectionState; }
            private set { SetProperty(ref _connectionState, value); }
        }

        private string _username;

        public ChatRoomViewModel(string classID, string username, CoreDispatcher uiDispatcher = null)
        {
            Dispatcher = uiDispatcher;
            _username = username;
            _chatroomModel = new ChatRoomModel(classID, username);
            _chatroomModel.MessageArrived += _chatroomModel_MessageArrived;
            _chatroomModel.ServerConnectionStateChanged += _chatroomModel_ServerConnectionStateChanged;
        }

        async void _chatroomModel_ServerConnectionStateChanged(object sender, ChatRoomConnectionStateChangedArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ConnectionState = e.State;
            });
        }

        async void _chatroomModel_MessageArrived(object sender, ChatRoomMessageEventArgs e)
        {
            ChatContent content = e.Content;
            content.SentRecvTime = DateTime.Now;

            if (Dispatcher != null)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ChatHistory.Add(content);
                });

            }
        }

        public async Task SendMessage(string content)
        {
            var chatContent = new ChatContent()
            {
                Content = content,
                Sender = _username,
                SentRecvTime = DateTime.Now,
                IsMe = true,
                Sent = false,
            };
            if (Dispatcher != null)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ChatHistory.Add(chatContent);
                });
            }
            await _chatroomModel.SendMessage(chatContent);
        }

        public async Task ResendMessage(ChatContent chatContent)
        {
            await _chatroomModel.SendMessage(chatContent);
        }

        public async void TryConnect()
        {
            await _chatroomModel.Connect();
        }
    }

}
