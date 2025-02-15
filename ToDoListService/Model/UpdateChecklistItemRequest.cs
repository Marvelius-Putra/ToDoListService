using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoListService.Model
{
    public class UpdateChecklistItemRequest
    {
        public int Id { get; set; }  
        public string Name { get; set; }
    }
}
