namespace suryami62.Models;

public record PublicationAbout(PublicationData? PublicationData);

public record PublicationData(StaticPage? StaticPage);

public record StaticPage(string? Title, Content? Content);

public record Content(string? Markdown);
