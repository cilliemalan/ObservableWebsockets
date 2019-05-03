#if OWIN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObservableWebsockets.Internal
{
    class OwinRequestContext : RequestContext
    {
        private IDictionary<string, object> _env;

        private T Get<T>(string n) => _env.TryGetValue(n, out var o) ? (T)o : default;

        public OwinRequestContext(IDictionary<string, object> env)
        {
            _env = env;
            _headersD = new Lazy<IDictionary<string, string>>(() =>
                Get<IDictionary<string, string[]>>("owin.RequestHeaders")
                    .ToDictionary(x => x.Key, y => y.Value.First()));
        }

        public override string Host
        {
            get
            {
                string host = RawHeaders.TryGetValue("Host", out var v) ? v.FirstOrDefault() : default;
                if (!string.IsNullOrWhiteSpace(host))
                {
                    return host;
                }

                return LocalPort == 0 ? LocalIpAddress : $"{LocalIpAddress}:{LocalPort}";
            }
        }

        private Lazy<IDictionary<string, string>> _headersD;

        public override string LocalIpAddress => Get<string>("server.LocalIpAddress");

        public override int LocalPort => int.TryParse(Get<string>("server.LocalPort"), out var i) ? i : default;

        public override string RemoteIpAddress => Get<string>("server.RemoteIpAddress");

        public override int RemotePort => int.TryParse(Get<string>("server.RemotePort"), out var i) ? i : default;

        public override string Path => Get<string>("owin.RequestPath");

        public override string PathBase => Get<string>("owin.RequestPathBase");

        public override string QueryString => Get<string>("owin.RequestQueryString");

        public override string Scheme => Get<string>("owin.RequestScheme");

        public override string Method => Get<string>("owin.RequestMethod");

        public override bool IsSecure => Scheme?.Equals("https") ?? false;

        public override string Url => $"{Scheme}://{Host}{PathBase}{Path}{(string.IsNullOrEmpty(QueryString) ? null : "?" + QueryString)}";

        public override string Protocol => Get<string>("owin.RequestProtocol");

        public override IDictionary<string, string> Headers => _headersD.Value;

        public override IDictionary<string, string[]> RawHeaders => Get<IDictionary<string, string[]>>("owin.RequestHeaders");
    }
}
#endif