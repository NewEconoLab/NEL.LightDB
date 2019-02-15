using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using NEL.Pipeline;
using NEL.Simple.SDK;
using Neo;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using LightDB;

namespace NEL.Cli.ApiServer
{
    public sealed class RpcServerModule : Module,IDisposable
    {
        private IWebHost host;

        private Setting setting;

        private IModulePipeline actor;

        public RpcServerModule()
        {
            setting = new Setting();
        }

        private ConcurrentDictionary<string, ConcurrentQueue<JObject>> dic = new ConcurrentDictionary<string, ConcurrentQueue<JObject>>();

        private async Task ProcessAsync(HttpContext context)
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            context.Response.Headers["Access-Control-Max-Age"] = "31536000";
            if (context.Request.Method != "GET" && context.Request.Method != "POST") return;
            var session = context.Response.Cookies.ToString();
            JObject request = null;
            if (context.Request.Method == "GET")
            {
                string jsonrpc = context.Request.Query["jsonrpc"];
                string id = context.Request.Query["id"];
                string method = context.Request.Query["method"];
                string _params = context.Request.Query["params"];
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(method) && !string.IsNullOrEmpty(_params))
                {
                    try
                    {
                        _params = Encoding.UTF8.GetString(Convert.FromBase64String(_params));
                    }
                    catch (FormatException) { }
                    request = new JObject();
                    if (!string.IsNullOrEmpty(jsonrpc))
                        request["jsonrpc"] = jsonrpc;
                    request["id"] = id;
                    request["method"] = method;
                    request["params"] = JObject.Parse(_params);
                }
            }
            else if (context.Request.Method == "POST")
            {
                using (StreamReader reader = new StreamReader(context.Request.Body))
                {
                    try
                    {
                        request = JObject.Parse(reader);
                    }
                    catch (FormatException) { }
                }
            }
            JObject response = CreateErrorResponse(null, -32800, "time out");
            request["host"] = context.Request.Host.Value;
            if (request == null)
            {
                response = CreateErrorResponse(null, -32700, "Parse error");
            }
            else if (request is JArray array)
            {
                if (array.Count == 0)
                {
                    response = CreateErrorResponse(request["id"], -32600, "Invalid Request");
                }
                else
                {
                    ProcessSend(request);
                }
            }
            else
            {
                ProcessSend(request);
            }
            context.Response.ContentType = "application/json-rpc";
            var key = request["host"].AsString() + request["method"].AsString();
            DateTime time = DateTime.Now;
            while ((DateTime.Now - time).TotalSeconds < 5.0f)
            {
                if (dic.ContainsKey(key) && !dic[key].IsEmpty)
                {
                    dic[key].TryDequeue(out response);
                    break;
                }
            }
            await context.Response.WriteAsync(response.ToString(), Encoding.UTF8);
        }

        public void Start(IPAddress bindAddress, int port, string sslCert = null, string password = null, string[] trustedAuthorities = null)
        {
            host = new WebHostBuilder().UseKestrel(options => options.Listen(bindAddress, port, listenOptions =>
            {
                if (string.IsNullOrEmpty(sslCert)) return;
                listenOptions.UseHttps(sslCert, password, httpsConnectionAdapterOptions =>
                {
                    if (trustedAuthorities is null || trustedAuthorities.Length == 0)
                        return;
                    httpsConnectionAdapterOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    httpsConnectionAdapterOptions.ClientCertificateValidation = (cert, chain, err) =>
                    {
                        if (err != SslPolicyErrors.None)
                            return false;
                        X509Certificate2 authority = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
                        return trustedAuthorities.Contains(authority.Thumbprint);
                    };
                });
            }))
            .Configure(app =>
            {
                app.UseResponseCompression();
                app.Run(ProcessAsync);
            })
            .ConfigureServices(services =>
            {
                services.AddResponseCompression(options =>
                {
                    options.Providers.Add<GzipCompressionProvider>();
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json-rpc" });
                });

                services.Configure<GzipCompressionProviderOptions>(options =>
                {
                    options.Level = CompressionLevel.Fastest;
                });
            })
            .Build();

            host.Start();
        }

        private static JObject CreateErrorResponse(JObject id, int code, string message, JObject data = null)
        {
            JObject response = CreateResponse(id);
            response["error"] = new JObject();
            response["error"]["code"] = code;
            response["error"]["message"] = message;
            if (data != null)
                response["error"]["data"] = data;
            return response;
        }

        private static JObject CreateResponse(JObject id)
        {
            JObject response = new JObject();
            response["jsonrpc"] = "2.0";
            response["id"] = id;
            return response;
        }

        public override void Dispose()
        {
            base.Dispose();
            if (host != null)
            {
                host.Dispose();
                host = null;
            }
        }
 
        public override void OnStart()
        {
            //开启rpc服务
            Start(IPAddress.Parse(setting.BindAddress),setting.Port);
            //链接数据库服务器
            actor= this.GetPipeline(string.Format("{0}:{1}/{2}",setting.DBServerAddress,setting.DBServerPort,setting.DBServerPath));
        }

        public override void OnTell(IModulePipeline from, byte[] data)
        {
            if (from != null  && from.system.Remote != null)//来自server
            {
                ProcessGet(data);
            }
        }

        public override void OnTellLocalObj(IModulePipeline from, object obj)
        {
        }

        private void ProcessSend(JObject request)
        {
            if(!actor.IsVaild) //以防数据库服务挂了
                actor = this.GetPipeline(string.Format("{0}:{1}/{2}", setting.DBServerAddress, setting.DBServerPort, setting.DBServerPath));

            string method = request["method"].AsString();
            string host = request["host"].AsString();
            string id = request["id"].AsString();
            JArray _params = (JArray)request["params"];
            NetMessage msgBack = NetMessage.Create("_db.usesnapshot");
            actor.Tell(msgBack.ToBytes());
            switch (method)
            {
                case "getstorage":
                    UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                    byte[] key = _params[1].AsString().HexToBytes();
                    StorageKey storageKey = new StorageKey
                    {
                        ScriptHash = script_hash,
                        Key = key
                    };
                    Identity identity = new Identity(host,method,id);
                    NetMessage netMessage = NetMessage.Create("_db.snapshot.getvalue", identity.ToString());
                    netMessage.Params["tableid"] = new byte[] { };
                    netMessage.Params["key"] = (new byte[] { 0x70 }).Concat(storageKey.ToArray()).ToArray();
                    actor.Tell(netMessage.ToBytes());
                    break;
                default:
                    break;
            }
        }

        private void ProcessGet(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                NetMessage netMsg = NetMessage.Unpack(ms);
                Identity identity = Identity.ToIdentity(netMsg.ID);
                if (identity.Mehotd == "getstorage")
                {
                    string key = identity.Host + identity.Mehotd;
                    JObject response = CreateResponse(identity.ID);
                    response["result"] = DBValue.FromRaw(netMsg.Params["data"]).value.AsSerializable<StorageItem>().Value.ToHexString();
                    if (dic.ContainsKey(key))
                        dic[key].Enqueue(response);
                    else
                    {
                        dic.TryAdd(key, new ConcurrentQueue<JObject>());
                        dic[key].Enqueue(response);
                    }
                }
            }
        }
    }
}
