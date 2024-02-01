// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using k8s;
using k8s.Autorest;

namespace Aspire.Hosting.Dcp;

internal class DcpKubernetesClient : k8s.Kubernetes
{
    public DcpKubernetesClient(KubernetesClientConfiguration config, params DelegatingHandler[] handlers) : base(config, handlers)
    {
    }

    // TODO: documentation
    // TODO: stdout vs stderr
    public async Task<HttpOperationResponse<Stream>> ReadSubResourceAsStreamAsync(
        string group,
        string version,
        string plural,
        string name,
        string subResource,
        string namespaceParameter = "",
        bool? follow = true,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group, nameof(group));
        ArgumentException.ThrowIfNullOrWhiteSpace(version, nameof(version));
        ArgumentException.ThrowIfNullOrWhiteSpace(plural, nameof(plural));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(subResource, nameof(subResource));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(HttpClientTimeout);
        cancellationToken = cts.Token;

        string url;
        if (string.IsNullOrEmpty(namespaceParameter))
        {
            url = $"apis/{group}/{version}/{plural}/{name}/{subResource}";
        }
        else
        {
            url = $"apis/{group}/{version}/namespaces/{namespaceParameter}/{plural}/{name}/{subResource}";
        }
        var q = new QueryBuilder();
        q.Append("follow", follow);
        url += q.ToString();

        const IReadOnlyDictionary<string, IReadOnlyList<string>>? customHeaders = null;
        const object? content = null;
        var httpResponse = await SendRequest<object?>(url, HttpMethod.Get, customHeaders, content, cancellationToken).ConfigureAwait(false);
        var httpRequest = httpResponse.RequestMessage;
        var result = new HttpOperationResponse<Stream>()
        {
            Request = httpRequest,
            Response = httpResponse,
            Body = await httpResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false)
        };
        return result;
    }

    private sealed class QueryBuilder
    {
        private readonly List<string> _parameters = new List<string>();

        public void Append(string key, int val)
        {
            _parameters.Add($"{key}={val}");
        }

        public void Append(string key, bool? val)
        {
            _parameters.Add($"{key}={(val == true ? "true" : "false")}");
        }

        public void Append(string key, string val)
        {
            _parameters.Add($"{key}={Uri.EscapeDataString(val)}");
        }

        public override string ToString()
        {
            if (_parameters.Count > 0)
            {
                return $"?{string.Join("&", _parameters)}";
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
