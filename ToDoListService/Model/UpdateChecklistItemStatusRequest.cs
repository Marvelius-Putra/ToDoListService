using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoListService.Model
{
    public class UpdateChecklistItemStatusRequest
    {
        public int Id { get; set; } // ID item yang akan diperbarui
        public bool IsCompleted { get; set; } // Status baru
    }

}
