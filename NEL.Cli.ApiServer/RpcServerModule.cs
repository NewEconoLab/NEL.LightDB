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
using Neo.Network.P2P.Payloads;

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

        public DataCache<string, byte[]> storageCache = new DataCache<string, byte[]>();
        public DataCache<string, BlockState> blockstateCache = new DataCache<string, BlockState>();
        public DataCache<string, Transaction[]> transactionCache = new DataCache<string, Transaction[]>();

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
                    response =await ProcessSend(request);
                }
            }
            else
            {
                response = await ProcessSend(request);
            }
            context.Response.ContentType = "application/json-rpc";
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

        private async Task<JObject> ProcessSend(JObject request)
        {
            if(!actor.IsVaild) //以防数据库服务挂了
                actor = this.GetPipeline(string.Format("{0}:{1}/{2}", setting.DBServerAddress, setting.DBServerPort, setting.DBServerPath));
            //因为收发消息全异步，就用一个标识来明确某个回复对应哪个请求
            JArray _params = (JArray)request["params"];
            switch (request["method"].AsString())
            {
                case "getstorage":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        byte[] key = _params[1].AsString().HexToBytes();
                        StorageKey storageKey = new StorageKey
                        {
                            ScriptHash = script_hash,
                            Key = key
                        };
                        Identity identity = new Identity(request["host"].AsString(), request["method"].AsString(), request["id"].AsString(), storageKey.ToArray().ToHexString());
                        NetMessage netMessage = Protocol_GetStorage.CreateSendMsg(storageKey.ToArray(), identity.ToString(), true);
                        actor.Tell(netMessage.ToBytes());
                        var value = await storageCache.Get(netMessage.ID);
                        var response = CreateResponse(identity.ID);
                        response["result"] = value.ToHexString();
                        return response;
                    }
                case "getblock":
                    {
                        Block block;
                        UInt256 hash;
                        hash = UInt256.Parse(_params[0].AsString());
                        Identity identity = new Identity(request["host"].AsString(), request["method"].AsString(), request["id"].AsString(), hash.ToString());
                        NetMessage netMessage = Protocol_GetBlock.CreateSendMsg(hash.ToArray(), identity.ToString(), true);
                        actor.Tell(netMessage.ToBytes());
                        var blockstate = await blockstateCache.Get(netMessage.ID);
                        var transactions = new Transaction[] { };
                        //获取这个block中的所有交易
                        {
                            UInt256[] hashes = blockstate.TrimmedBlock.Hashes;
                            //简单处理 key可能有重复
                            var key = hashes.First().ToString()+hashes.Length+hashes.Last().ToString();
                            Identity identity_tran = new Identity(request["host"].AsString(), "gettransaction", request["id"].AsString(), key);
                            NetMessage netMessage_tran = Protocol_GetTransaction.CreateSendMsg(hashes, identity_tran.ToString(), true);
                            actor.Tell(netMessage_tran.ToBytes());
                            transactions = await transactionCache.Get(netMessage_tran.ID);
                        }
                        block = new Block
                        {
                            Version = blockstate.TrimmedBlock.Version,
                            PrevHash = blockstate.TrimmedBlock.PrevHash,
                            MerkleRoot = blockstate.TrimmedBlock.MerkleRoot,
                            Timestamp = blockstate.TrimmedBlock.Timestamp,
                            Index = blockstate.TrimmedBlock.Index,
                            ConsensusData = blockstate.TrimmedBlock.ConsensusData,
                            NextConsensus = blockstate.TrimmedBlock.NextConsensus,
                            Witness = blockstate.TrimmedBlock.Witness,
                            Transactions = transactions
                        };
                        var response = CreateResponse(identity.ID);
                        bool verbose = _params.Count >= 2 && _params[1].AsBooleanOrDefault(false);
                        if (verbose)
                        {
                            JObject json = block.ToJson(); // 目前使用neo中的block.tojson用到了protocol这个东西，后续抽离
                            response["result"] = json;
                        }
                        else
                        {
                            response["result"] = block.ToArray().ToHexString();
                        }
                        return response;
                    }
                default:
                    break;
            }
            return null;
        }

        private void ProcessGet(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                NetMessage netMsg = NetMessage.Unpack(ms);
                Identity identity = Identity.ToIdentity(netMsg.ID);
                if (identity.Mehotd == "getstorage")
                {
                    Protocol_GetStorage.message message = Protocol_GetStorage.PraseRecvMsg(netMsg)[0];
                    storageCache.Add(netMsg.ID, message.value);
                }
                else if (identity.Mehotd == "getblock")
                {
                    BlockState blockState = Protocol_GetBlock.PraseRecvMsg(netMsg)[0];
                    blockstateCache.Add(netMsg.ID, blockState);
                }
                else if (identity.Mehotd == "gettransaction")
                {
                    Transaction[] transactions = Protocol_GetTransaction.PraseRecvMsg(netMsg);
                    transactionCache.Add(netMsg.ID,transactions);
                }
            }
        }
    }
}
