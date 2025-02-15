using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoListService.Interfaces;
using ToDoListService.Model;

namespace ToDoListService.Services
{
    public class CheckListService : ICheckListService
    {
        private static readonly List<Checklist> _checklists = new();
        private static int _nextId = 1; // ID dimulai dari 1
        public Checklist CreateChecklist(string title)
        {
            var newChecklist = new Checklist
            {
                Id = _nextId++, 
                Title = title
            };

            _checklists.Add(newChecklist);
            return newChecklist;
        }

        public List<Checklist> GetAllChecklists()
        {
            return _checklists;
        }

        public bool DeleteChecklist(int id)
        {       
            var checklist = _checklists.FirstOrDefault(c => c.Id == id);

            if (checklist == null)
            {
                return false;
            }

            _checklists.Remove(checklist);
            return true;
        }

    }
}
