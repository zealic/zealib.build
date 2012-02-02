using System;
using System.Net;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zealib.Build.Tasks
{
    public class SetWebProxy : Task
    {
        public enum ProxyTypes
        {
            Auto,
            Manual,
            Default,
            None,
        }

        [Required]
        public string ProxyType { get; set; }

        public string ProxyAddress { get; set; }

        public bool BypassOnLocal { get; set; }

        public string[] BypassList { get; set; }

        public override bool Execute()
        {
            ProxyTypes proxyType;
            if (!Enum.TryParse(ProxyType, true, out proxyType))
            {
                Log.LogError("ProxyType value must in '{0}'", string.Join(",", Enum.GetNames(typeof(ProxyTypes))));
                return false;
            }

            WebProxy proxy = null;
            switch (proxyType)
            {
                case ProxyTypes.Auto:
                    proxy = GetAutoProxy();
                    if (proxy == null) goto case ProxyTypes.Manual;
                    break;
                case ProxyTypes.Manual:
                    if (string.IsNullOrEmpty(ProxyAddress))
                        proxy = new WebProxy(ProxyAddress, BypassOnLocal, BypassList);
                    if (proxy == null) goto case ProxyTypes.Default;
                    break;
                case ProxyTypes.Default:
                    proxy = WebRequest.GetSystemWebProxy() as WebProxy;
                    break;
                case ProxyTypes.None:
                    proxy = null;
                    break;
            }

            if (WebRequest.DefaultWebProxy != proxy)
            {
                WebRequest.DefaultWebProxy = proxy;
                if (proxy == null) Log.LogMessage("Disable proxy!");
                else Log.LogMessage("Using proxy : {0}", proxy.Address);
            }
            return true;
        }

        private WebProxy GetAutoProxy()
        {
            var http_proxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            var all_proxy = Environment.GetEnvironmentVariable("ALL_PROXY");

            if (!string.IsNullOrEmpty(http_proxy))
                return new WebProxy(http_proxy, BypassOnLocal, BypassList);
            if (!string.IsNullOrEmpty(all_proxy))
                return new WebProxy(all_proxy, BypassOnLocal, BypassList);

            return null;
        }
    }
}
