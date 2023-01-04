using DeFuncto;
using FunctionalTodo.Models;

namespace FunctionalTodo.DomainModel;

public delegate Task<IEnumerable<TodoListItem>> GetAllFromDb();
public delegate Task<int> CreateTodo(TodoCreation parameters);
public delegate Task<TodoListItem> GetByTitle(string title);
public delegate Task<TodoListItem?> GetById(int id);
public delegate Task<int> MarkTodoAsCompleted(TodoListItem todo);
// he añadido Task al tipo. Está bien o tenia que hacerlo funcionar sin?
public delegate AsyncOption<TodoListItem> FindByTitle(string title);


public delegate string GetConnectionString();
