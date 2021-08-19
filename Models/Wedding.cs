using System;
using System.Collections.Generic; 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WeddingPlanner.Models
{
    public class Wedding
    {
        [Key]
        public int WeddingId { get; set; }
        [Required]
        [MinLength(2)]
        public string Wedder1 {get; set;}
        [Required]
        public string Wedder2 {get; set;}

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Date {get; set;}
        [Required]
        public string Address {get; set;}

        public int UserId {get; set;}
        public User Planner {get; set;}
        public List<Response> Responses {get; set;}
    }
}

