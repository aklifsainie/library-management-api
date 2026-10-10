using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace library.management.domain.Models.ViewModels.AuthenticationVM
{
    public class LoginResponseVM
    {
        public string AccessToken { get; init; } = string.Empty;
        public int ExpiresInSeconds { get; init; }
        public bool RequiresTwoFactor { get; init; }
        public bool IsEmailConfirmed { get; init; }
    }
}
