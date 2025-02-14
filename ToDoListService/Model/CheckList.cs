using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoListService.Model
{
    public class Checklist
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<ChecklistItem> Items { get; set; } = new();
    }

    public class ChecklistItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsCompleted { get; set; } = false;
    }
}
