namespace suryami62.Models;

public record PublicationAbout
{
    public PublicationData? PublicationData { get; init; }
}

public record PublicationData
{
    public StaticPage? StaticPage { get; init; }
}

public record StaticPage
{
    public string? Title { get; init; }
    public Content? Content { get; init; }
}

public record Content
{
    public string? Markdown { get; init; }
}