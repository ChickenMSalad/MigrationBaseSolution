using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Utilities
{
    public sealed class HttpClientDiagnosticsHandler : DelegatingHandler
    {
        private readonly string _clientName;

        public HttpClientDiagnosticsHandler(string clientName)
        {
            _clientName = clientName;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // HttpClient.Timeout is not directly accessible here,
            // but we can infer it by logging a marker + stack trace.
            System.Diagnostics.Debug.WriteLine(
                $"[HTTP] Client='{_clientName}' | {request.Method} {request.RequestUri}");

            return await base.SendAsync(request, cancellationToken);
        }
    }


}
