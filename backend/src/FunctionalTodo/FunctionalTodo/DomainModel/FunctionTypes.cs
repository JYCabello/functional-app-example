using DeFuncto;
using FunctionalTodo.Models;

namespace FunctionalTodo.DomainModel;

public delegate Task<IEnumerable<TodoListItem>> GetAllFromDb();
public delegate Task<int> CreateTodo(TodoCreation parameters);
//Para Irene
// docker compose up EN LA CARPETA DONDE ESTá EL YAML
// crea la base de datos "todo" a mano.
public delegate Task<TodoListItem> GetByTitle(string title);
public delegate AsyncOption<TodoListItem> FindByTitle(string title);


public delegate string GetConnectionString();
