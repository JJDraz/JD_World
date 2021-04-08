using System;

namespace JDWorldAPI.Models
{
    public class ResidentDto 
    {  
	
        public Guid Id { get; set; }

        public string WorldName { get; set; }

        public string WorldUserEmail { get; set; }

        public string WorldRole { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset ModifiedAt { get; set; }

    }

}
