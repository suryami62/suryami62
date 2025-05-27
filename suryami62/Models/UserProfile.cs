namespace suryami62.Models;

public record UserProfile(UserData? UserData);

public record UserData(
    string? Username,
    string? Name,
    Bio? Bio,
    string? ProfilePicture,
    SocialMediaLinks? SocialMediaLinks
);

public record Bio(string? Text);

public record SocialMediaLinks(
    string? Instagram,
    string? Twitter,
    string? Linkedin,
    string? Github,
    string? Facebook,
    string? Youtube,
    string? Bluesky
);
