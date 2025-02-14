using ToDoListService.Model;

namespace ToDoListService.Interfaces
{
    public interface ICheckListService
    {
        Checklist CreateChecklist(string title);
        List<Checklist> GetAllChecklists();
        bool DeleteChecklist(int id);
    }
}