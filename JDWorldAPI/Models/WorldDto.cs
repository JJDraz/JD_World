using System;

namespace JDWorldAPI.Models
{
    public class WorldDto
    {
        public Guid Id { get; set; }

        public string WorldName { get; set; }

		public string TenantName { get; set; }

        public string ServerIP { get; set; }

        public string VoiceIP { get; set; }

    }
}
