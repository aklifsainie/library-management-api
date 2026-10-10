using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace library.management.infrastructure.ApiClientAuthentication
{
    public sealed record GeneratedApiKey(string RawKey, string KeyPrefix, string KeyHash);
}
