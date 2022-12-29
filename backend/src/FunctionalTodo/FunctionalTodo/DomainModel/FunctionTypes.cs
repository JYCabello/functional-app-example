﻿using DeFuncto;
using FunctionalTodo.Models;

namespace FunctionalTodo.DomainModel;

public delegate Task<IEnumerable<TodoListItem>> GetAllFromDb();
public delegate Task<bool> CreateTodo(TodoCreation parameters);
public delegate Task<TodoListItem> GetByTitle(string title);
public delegate AsyncOption<TodoListItem> FindByTitle(string title);
public delegate Task<TodoListItem> GetById(int id);
public delegate Task<int> MarkTodoAsCompleted(TodoListItem todo);


public delegate string GetConnectionString();
