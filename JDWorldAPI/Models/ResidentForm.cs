using System;
using System.ComponentModel.DataAnnotations;

namespace JDWorldAPI.Models
{
    public class ResidentForm
    {
        [Required]
        [Display(Name = "worldUserEmail", Description = "World User Email")]
        public string WorldUserEmail { get; set; }

        [Required]
        [Display(Name = "worldUserRole", Description = "World User Role")]
        public string WorldUserRole { get; set; }
    }
}
