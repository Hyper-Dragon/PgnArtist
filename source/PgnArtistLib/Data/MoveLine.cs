namespace PgnArtistLib.Data;

public record class MoveLine
{
    public string San { get; set; } = "";
    public string BoardFen { get; set; } = "";
    public Image? BoardImage { get; set; }
    public string Comment { get; set; } = "";
}

