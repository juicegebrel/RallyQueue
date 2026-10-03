namespace RallyQueue.Models;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Games { get; set; }
    public int Order { get; set; }      // lower = has waited longer
    public bool Resting { get; set; }
}

public class Match
{
    public List<Player> Players { get; set; } = new();
}

public class Court
{
    public int Number { get; set; }
    public Match? Current { get; set; }
}
