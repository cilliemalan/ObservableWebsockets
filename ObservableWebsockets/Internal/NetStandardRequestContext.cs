#if NETSTANDARD
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using System.Net;

namespace ObservableWebsockets.Internal
{
    class NetStandardRequestContext : RequestContext
    {
        private HttpContext _ctx;
        private IHttpConnectionFeature _conn;
        private Lazy<IDictionary<string, string>> _headersD;

        public NetStandardRequestContext(HttpContext context)
        {
            _ctx = context;
            _conn = _ctx.Features.Get<IHttpConnectionFeature>();
            _headersD = new Lazy<IDictionary<string, string>>(() => RawHeaders.ToDictionary(x => x.Key, y => y.Value.First()));
        }

        public override string Host => AsString(_ctx.Request.Host);

        public override string LocalIpAddress => AsString(_conn.LocalIpAddress);

        public override int LocalPort => _conn.LocalPort;

        public override string RemoteIpAddress => AsString(_conn.RemoteIpAddress);

        public override int RemotePort => _conn.RemotePort;

        public override string PathBase => AsString(_ctx.Request.PathBase);

        public override string Path => AsString(_ctx.Request.Path);

        public override string QueryString => AsString(_ctx.Request.QueryString);

        public override string Scheme => _ctx.Request.Scheme;

        public override string Method => _ctx.Request.Method;

        public override bool IsSecure => _ctx.Request.IsHttps;

        public override string Url => $"{Scheme}://{Host}{PathBase}{Path}{QueryString}";

        public override string Protocol => _ctx.Request.Protocol;

        public override IDictionary<string, string> Headers => _headersD.Value;

        public override IHeaderDictionary RawHeaders => _ctx.Request.Headers;

        private string AsString(HostString host) => host.HasValue ? host.Value : "";
        private string AsString(PathString pathBase) => pathBase.HasValue ? pathBase.Value : "";
        private string AsString(QueryString queryString) => queryString.HasValue ? queryString.Value : "";
        private string AsString(IPAddress localIpAddress) => localIpAddress?.ToString() ?? "";
    }
}

#endif