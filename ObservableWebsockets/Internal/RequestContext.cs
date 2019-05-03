using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObservableWebsockets.Internal
{
    abstract class RequestContext
    {
        public abstract string LocalIpAddress { get; }
        public abstract int LocalPort { get; }
        public abstract string RemoteIpAddress { get; }
        public abstract int RemotePort { get; }
        public abstract string Host { get; }
        public abstract string Path { get; }
        public abstract string PathBase { get; }
        public abstract string QueryString { get; }
        public abstract string Scheme { get; }
        public abstract string Method { get; }
        public abstract bool IsSecure { get; }
        public abstract string Url { get; }
        public abstract string Protocol { get; }
        public abstract IDictionary<string,string> Headers { get; }

#if NETSTANDARD
        public abstract Microsoft.AspNetCore.Http.IHeaderDictionary RawHeaders { get; }
#else
        public abstract IDictionary<string, string[]> RawHeaders { get; }
#endif
    }
}
