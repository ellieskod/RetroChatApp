using ChatApp.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ChatApp.DataService
{
    public class SharedDb
    {
        private readonly ConcurrentDictionary<string, UserConnection> _connection = new();
        private readonly List<string> _forbiddenWords = new();

        public ConcurrentDictionary<string, UserConnection> Connection => _connection;
        public List<string> ForbiddenWords => _forbiddenWords;
    }
}