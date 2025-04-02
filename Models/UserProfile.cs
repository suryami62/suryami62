namespace suryami62.Models;

public record UserProfile
{
    public UserData? UserData { get; init; }
}

public record UserData
{
    public string? Username { get; init; }
    public string? Name { get; init; }
    public Bio? Bio { get; init; }
    public string? ProfilePicture { get; init; }
    public SocialMediaLinks? SocialMediaLinks { get; init; }
}

public record Bio
{
    public string? Text { get; init; }
}

public record SocialMediaLinks
{
    public string? Instagram { get; init; }
    public string? Twitter { get; init; }
    public string? Linkedin { get; init; }
    public string? Github { get; init; }
}