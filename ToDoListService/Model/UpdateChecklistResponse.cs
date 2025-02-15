using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoListService.Model
{
    public class UpdateChecklistResponse
    {
        public Checklist Checklist { get; set; } // Checklist setelah update
        public List<int> IgnoredItems { get; set; } // Item yang tidak ditemukan
    }

}
