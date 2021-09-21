namespace PgnArtistLib.Data;



public record GameLine
{
    public Dictionary<string, string> TextForKey { get; } = new();
    public List<SortedList<string, RenderableGameMove>> MoveLines { get; } = new();
}



public record class RenderableGameMove
{
    public bool IsHidden { get; set; } = false;
    public string San { get; set; } = "";
    public string LastBoardFen { get; set; } = "";
    public string BoardFen { get; set; } = "";
    public Image? BoardImage { get; set; }
    public string Comment { get; set; } = "";
}


public class MoveImageData
{
    public IEnumerable<Game> ParsedGames { get; set; }
    public GameFilter Filter { get; set; }
    public bool IsFromWhitesPerspective { get; set; }
}


public class RenderableGameCollection
{
    public List<string> LastMoveNameList { get; set; } = new();

    public string[] Annotations { get; set; } = Array.Empty<string>();

    public RenderableGameMove[,] DisplayGrid { get; set; } = new RenderableGameMove[0, 0];

    public int MaxWidth { get; set; } = 0;



}

