namespace PgnArtistLib.Data;

public record class MoveLine
{
    public string San { get; set; } = "";
    public string BoardFen { get; set; } = "";
    public Image? BoardImage { get; set; }
    public string Comment { get; set; } = "";
}


public class MoveImageData
{
    public IEnumerable<Game<MoveStorage>> ParsedGames { get; set; }
    public GameFilter Filter { get; set; }
    public bool IsFromWhitesPerspective { get; set; }
}


public class RenderableGame
{
    public SortedList<string, string> LastMoveNameList { get; set; } = new();
    public List<SortedList<string, MoveLine>> MoveLines { get; set; } = new();

    public int MaxWidth { get; set; } = 0;



}

