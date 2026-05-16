namespace TodoMVC.Models;

public class Instrumento
{
    public int Id { get; set; }
    public string? Titulo { get; set; }
    public bool Completada { get; set; }
    public int Cantidad { get; set; } = 1;
}
