using FunctionalTodo.Models;

namespace FunctionalTodo.DomainModel;

public delegate Task<IEnumerable<TodoListItem>> GetAllFromDb();

public interface IDbAccessFunctions
{
    GetAllFromDb GetAllFromDb { get; }
}
