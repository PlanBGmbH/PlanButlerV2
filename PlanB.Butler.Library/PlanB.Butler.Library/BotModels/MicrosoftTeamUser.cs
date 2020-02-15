namespace BotLibraryV2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class MicrosoftTeamUser
    {
        public string ObjectId { get; set; }

        public string GivenName { get; set; }

        public string Surname { get; set; }

        public string Email { get; set; }

        public string UserPrincipalName { get; set; }

        public string TenantId { get; set; }
    }
}
