using System;
using System.Collections.Generic;

namespace GenericDataPlatform.API.Models.Auth
{
    public class AuthResponse
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<string> Roles { get; set; }
        public string Token { get; set; }
        public DateTime TokenExpiration { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}
