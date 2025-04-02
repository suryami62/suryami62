using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using suryami62.Models;

namespace suryami62.Services;

public sealed class PublicationAboutService : IDisposable
{
    private readonly GraphQLHttpClient _graphqlClient;

    private static readonly string AboutQuery = @"
        query Publication($host: String!) {
            PublicationData: publication(host: $host) {
                staticPage(slug: ""about"") {
                    title
                    content {
                        markdown
                    }
                }
            }
        }";

    public PublicationAboutService(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        var endpoint = configuration["GraphQL:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentNullException(nameof(endpoint), "GraphQL endpoint value is missing or empty.");
        }

        _graphqlClient = new GraphQLHttpClient(endpoint, new NewtonsoftJsonSerializer());
    }

    public async Task<PublicationAbout?> GetUserAboutAsync(string host)
    {
        var request = new GraphQLRequest
        {
            Query = AboutQuery,
            Variables = new { host }
        };

        var response = await _graphqlClient.SendQueryAsync<PublicationAbout>(request).ConfigureAwait(false);
        return response.Data;
    }

    public void Dispose()
    {
        _graphqlClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
