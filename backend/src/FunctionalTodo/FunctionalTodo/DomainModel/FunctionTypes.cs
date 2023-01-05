﻿using DeFuncto;
using FunctionalTodo.Models;

namespace FunctionalTodo.DomainModel;

public delegate Task<IEnumerable<TodoListItem>> GetAllFromDb();
public delegate Task<int> CreateTodo(TodoCreation parameters);
public delegate Task<TodoListItem> GetByTitle(string title);
public delegate Task<TodoListItem?> GetById(int id);
public delegate Task<int> MarkTodoAsCompleted(int id);
public delegate AsyncOption<TodoListItem> FindByTitle(string title);
public delegate AsyncOption<TodoListItem> FindById(int id);


public delegate string GetConnectionString();
