using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;

using suryami62.Models;

namespace suryami62.Services;

public sealed class UserProfileService : IDisposable
{
    private readonly GraphQLHttpClient _graphqlClient;

    private static readonly string UserQuery = @"
        query User($username: String!) {
            UserData: user(username: $username) {
                username
                name
                bio { text }
                profilePicture
                socialMediaLinks {
                    instagram
                    twitter
                    linkedin
                    github
                    facebook
                    youtube
                    bluesky
                }
            }
        }";

    public UserProfileService(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var endpoint = configuration["GraphQL:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentNullException(nameof(configuration), "GraphQL endpoint value is missing or empty.");
        }

        _graphqlClient = new GraphQLHttpClient(new Uri(endpoint), new NewtonsoftJsonSerializer());
    }

    public async Task<UserProfile?> GetUserProfileAsync(string username, CancellationToken cancellationToken = default)
    {
        var request = new GraphQLRequest
        {
            Query = UserQuery,
            Variables = new { username }
        };

        var response = await _graphqlClient.SendQueryAsync<UserProfile>(request, cancellationToken)
                                             .ConfigureAwait(false);

        if (response.Errors != null && response.Errors.Length > 0)
        {
            var errorMessages = string.Join(", ", response.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"GraphQL errors: {errorMessages}");
        }

        return response.Data;
    }

    public void Dispose()
    {
        _graphqlClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
