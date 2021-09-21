namespace PgnArtistLib.Data;


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
    public List<Game> ParsedGames { get; set; } = new();
    public GameFilter Filter { get; set; } = new();
    public bool IsFromWhitesPerspective { get; set; }
}


public class RenderableGameCollection
{
    public string[] Annotations { get; set; } = Array.Empty<string>();
    public int[] GridStartY { get; set; } = Array.Empty<int>();
    public int[] GridEndY { get; set; } = Array.Empty<int>();
    public RenderableGameMove[,] DisplayGrid { get; set; } = new RenderableGameMove[0, 0];
    public int MaxWidth { get; set; } = 0;
}

