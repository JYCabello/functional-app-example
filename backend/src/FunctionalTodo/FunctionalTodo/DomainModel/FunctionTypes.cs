using FunctionalTodo.Models;

namespace FunctionalTodo.DomainModel;

public delegate Task<IEnumerable<TodoListItem>> GetAllFromDb();
public delegate Task<int> CreateTodo(TodoCreation parameters);
public delegate string GetConnectionString();
