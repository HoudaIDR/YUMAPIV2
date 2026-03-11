using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YUMAPI.Models
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; } // Hashé en MD5
        public List<MealListItem> Favoris { get; set; } = new List<MealListItem>();
    }
}
