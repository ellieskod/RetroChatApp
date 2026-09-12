using ChatApp.DataService;
using ChatApp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatApp.Hubs
{
    public class ChatHub : Hub
    {
        private readonly SharedDb _sharedDb;
        private readonly IConfiguration _config;

        public ChatHub(SharedDb sharedDb, IConfiguration config)
        {
            _sharedDb = sharedDb;
            _config = config;
        }

        public async Task JoinChatRoom(string userName, string chatRoom, string role, string password)
        {
            if (role == "Teacher")
            {
                var correctPassword = _config["TeacherPassword"];
                if (password != correctPassword)
                {
                    await Clients.Caller.SendAsync("JoinFailed", "Incorrect teacher password.");
                    return;
                }
            }
            else
            {
                role = "Student";
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, chatRoom);
            _sharedDb.Connection[Context.ConnectionId] = new UserConnection
            {
                UserName = userName,
                ChatRoom = chatRoom,
                Role = role
            };

            await Clients.Group(chatRoom).SendAsync("ReceiveMessage", "admin", $"{userName} ({role}) has joined {chatRoom}");
        }

        private string CensorMessage(string message)
        {
            var words = message.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (_sharedDb.ForbiddenWords.Contains(words[i].ToLower()))
                {
                    words[i] = new string('*', words[i].Length);
                }
            }
            return string.Join(' ', words);
        }

        public async Task SendMessage(string chatRoom, string userName, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            message = CensorMessage(message);

            await Clients.Group(chatRoom).SendAsync("ReceiveMessage", userName, message);
        }

        private const string AnnouncementGroup = "AnnouncementChannel";

        public async Task JoinAnnouncementChannel()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AnnouncementGroup);
        }

        public async Task SendAnnouncement(string userName, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (_sharedDb.Connection.TryGetValue(Context.ConnectionId, out var user) && user.Role == "Teacher")
            {
                message = CensorMessage(message);
                await Clients.Group(AnnouncementGroup).SendAsync("ReceiveAnnouncement", userName, message);
            }
        }

        public async Task GetForbiddenWords()
        {
            if (_sharedDb.Connection.TryGetValue(Context.ConnectionId, out var user) && user.Role == "Teacher")
            {
                await Clients.Caller.SendAsync("ReceiveForbiddenWords", _sharedDb.ForbiddenWords);
            }
        }

        public async Task RemoveForbiddenWord(string word)
        {
            if (_sharedDb.Connection.TryGetValue(Context.ConnectionId, out var user) && user.Role == "Teacher")
            {
                _sharedDb.ForbiddenWords.Remove(word.ToLower());
                await Clients.Caller.SendAsync("ReceiveForbiddenWords", _sharedDb.ForbiddenWords);
            }
        }

        public async Task AddForbiddenWord(string word)
        {
            if (_sharedDb.Connection.TryGetValue(Context.ConnectionId, out var user) && user.Role == "Teacher")
            {
                if (!string.IsNullOrWhiteSpace(word) && !_sharedDb.ForbiddenWords.Contains(word.ToLower()))
                {
                    _sharedDb.ForbiddenWords.Add(word.ToLower());
                }
                await Clients.Caller.SendAsync("ReceiveForbiddenWords", _sharedDb.ForbiddenWords);
            }
        }
    }
}