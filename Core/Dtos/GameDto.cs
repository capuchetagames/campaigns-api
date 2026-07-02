namespace Core.Dtos;

public class GameDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string Name { get; set; }
    public string Category { get; set; }
    public bool Active { get; set; }
    public decimal Price { get; set; }
}