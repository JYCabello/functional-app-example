using DeFuncto;
using FunctionalTodo.Models;

namespace FunctionalTodo.DomainModel;

public delegate Task<IEnumerable<TodoListItem>> GetAllFromDb();
public delegate Task<IEnumerable<TodoListItem>> GetAllIncompleteFromDb();
public delegate Task<IEnumerable<TodoListItem>> GetAllCompleteFromDb();
public delegate Task<TodoListItem> GetByTitle(string title);
public delegate AsyncOption<TodoListItem> GetById(int id);
public delegate Task<int> CreateTodo(TodoCreation parameters);
public delegate Task<int> MarkTodoAsComplete(int id);
public delegate Task<int> MarkTodoAsIncomplete(int id);
public delegate Task<bool> CheckIfCompleted(int id);
public delegate AsyncOption<TodoListItem> FindByTitle(string title);
public delegate AsyncOption<TodoListItem> FindById(int id);


public delegate string GetConnectionString();
