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
                }
            }
        }";

    public UserProfileService(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        var endpoint = configuration["GraphQL:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentNullException(nameof(endpoint), "GraphQL endpoint value is missing or empty.");
        }

        _graphqlClient = new GraphQLHttpClient(endpoint, new NewtonsoftJsonSerializer());
    }

    public async Task<UserProfile?> GetUserProfileAsync(string username)
    {
        var request = new GraphQLRequest
        {
            Query = UserQuery,
            Variables = new { username }
        };

        var response = await _graphqlClient.SendQueryAsync<UserProfile>(request).ConfigureAwait(false);
        return response.Data;
    }

    public void Dispose()
    {
        _graphqlClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
