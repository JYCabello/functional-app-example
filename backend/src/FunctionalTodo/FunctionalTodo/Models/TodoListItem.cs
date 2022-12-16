namespace FunctionalTodo.Models;

public class TodoListItem
{
    public Guid ID { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
