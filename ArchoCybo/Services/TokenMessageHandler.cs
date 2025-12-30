using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ArchoCybo.Services;

public class TokenMessageHandler : DelegatingHandler
{
    private readonly TokenProvider _tokenProvider;

    public TokenMessageHandler(TokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_tokenProvider.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenProvider.Token);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
