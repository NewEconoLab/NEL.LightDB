using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NEL.Common;
using NEL.Peer.Tcp;

namespace NEL.Pipeline
{
    class PipelineSystemV1 : ISystem
    {
        //本地创建的Actor实例
        global::System.Collections.Concurrent.ConcurrentDictionary<string, IModuleInstance> localModules;
        global::System.Collections.Concurrent.ConcurrentDictionary<IModuleInstance, string> localModulePath;
        //所有的Actor引用，无论是远程的还是本地的

        global::System.Collections.Concurrent.ConcurrentDictionary<string, ISystemPipeline> refSystems;

        //建立连接时找ip用
        global::System.Collections.Concurrent.ConcurrentDictionary<UInt64, string> linkedIP;
        public ISystemPipeline refSystemThis;

        public NEL.Common.ILogger logger;
        public PipelineSystemV1(NEL.Common.ILogger logger)
        {
            this.logger = logger;
            localModules = new global::System.Collections.Concurrent.ConcurrentDictionary<string, IModuleInstance>();
            localModulePath = new global::System.Collections.Concurrent.ConcurrentDictionary<IModuleInstance, string>();
            //refPipelines = new global::System.Collections.Concurrent.ConcurrentDictionary<string, IModulePipeline>();
            refSystems = new global::System.Collections.Concurrent.ConcurrentDictionary<string, ISystemPipeline>();
            linkedIP = new global::System.Collections.Concurrent.ConcurrentDictionary<ulong, string>();
            refSystemThis = new PipelineSystemRefLocal(this);
        }
        bool bStarted = false;
        public void Start()
        {
            bStarted = true;
            foreach (var pipe in this.localModules)
            {
                System.Threading.ThreadPool.QueueUserWorkItem((e) =>
                {
                    pipe.Value.OnStart();
                    pipe.Value.OnStarted();
                });
            }
        }


        public bool Disposed
        {
            get;
            private set;
        }
        public void Dispose()
        {
            if (this.Disposed)
                return;
            this.CloseListen();
            this.CloseNetwork();
            this.Disposed = true;
        }
        public void RegistModule(string path, IModuleInstance actor)
        {
            if (localModules.ContainsKey(path) == true)
                throw new Exception("already have that path.");

            localModules[path] = actor;
            localModulePath[actor] = path;
            actor.OnRegistered(this, path);

            if (bStarted)
            {
                System.Threading.ThreadPool.QueueUserWorkItem((e) =>
                {
                    actor.OnStart();
                    actor.OnStarted();
                });
            }
        }
        public void UnRegistModule(string path)
        {
            if (path.IndexOf("this/") != 0)
                path = "this/" + path;

            path = path.Substring(5);
            if (localModules.ContainsKey(path))
            {
                localModules.TryRemove(path, out IModuleInstance actor);
            }
        }
        public string GetModulePath(IModuleInstance actor)
        {
            if (localModulePath.TryGetValue(actor, out string path))
            {
                return path;
            }
            return null;
        }
        public IModuleInstance GetModule(string path)
        {
            return localModules[path];
        }
        public IModulePipeline GetPipelineByFrom(ISystemPipeline system, string from, string urlActor)
        {
            if (bStarted == false)
                throw new Exception("must getpipeline after System.Start()");

            if (urlActor.IndexOf("this/") != 0)
                throw new Exception("remote pipeonly");
            var refName = from + "_" + urlActor;


            var actorpath = urlActor.Substring(5);
            var targetModule = this.localModules[actorpath];

            var _from = refName == null ? null : this.GetPipeline(targetModule, from);

            return refSystemThis.GetPipeLineByFrom(_from, targetModule);
            //if (refPipelines.TryGetValue(refName, out IModulePipeline pipe))
            //{
            //    return pipe;
            //}

            //{

            //    refSystemThis.GetPipeline(from, actorpath);
            //    refPipelines[refName] = new PipelineRefLocal(refSystemThis, from, actorpath, targetModule);
            //    return refPipelines[refName];
            //}
        }
        public IModulePipeline GetPipeline(IModuleInstance user, string urlActor)
        {
            if (bStarted == false)
                throw new Exception("must getpipeline after System.Start()");

            var userstr = "";
            if (user != null)
                userstr = localModulePath[user];
            var refName = userstr + "_" + urlActor;

            //if (refPipelines.TryGetValue(refName, out IModulePipeline pipe))
            //{
            //    return pipe;
            //}

            if (urlActor[0] == '@')//收到通讯
            {
                var sppos = urlActor.IndexOf('/');
                var addrid = UInt64.Parse(urlActor.Substring(1, sppos - 1));
                var path = urlActor.Substring(sppos + 1);
                var addr = linkedIP[addrid];
                ISystemPipeline refsys = null;
                if (refSystems.TryGetValue(addr, out refsys))
                {
                    return refsys.GetPipeline(user, path);
                }
                else
                {//没连接
                    return null;
                }
                //refPipelines[refName] = new PipelineRefRemote(refSystemThis, userstr, refsys as RefSystemRemote, path);
                //return refPipelines[refName];
            }
            if (urlActor.IndexOf("this/") == 0)//本地模块
            {
                var actorpath = urlActor.Substring(5);
                //var actor = this.localModules[actorpath];
                return refSystemThis.GetPipeline(user, actorpath);
                //refPipelines[refName] = new PipelineRefLocal(refSystemThis, userstr, actorpath, actor);
                //return refPipelines[refName];
            }
            else //有地址的模块
            {
                var sppos = urlActor.IndexOf('/');
                var addr = urlActor.Substring(0, sppos);
                var path = urlActor.Substring(sppos + 1);
                ISystemPipeline refsys = null;
                if (refSystems.TryGetValue(addr, out refsys))
                {

                }
                else
                {//没连接
                    refsys = this.Connect(addr.AsIPEndPoint());
                }
                return refsys.GetPipeline(user, path);
                //refPipelines[refName] = new PipelineRefRemote(refSystemThis, userstr, refsys as RefSystemRemote, path);
                //return refPipelines[refName];
            }
        }
        //public IPipelineRef GetPipelineLocal(IPipelineInstance user, string path)
        //{
        //    return refActors["this/" + path];
        //}
        //public IPipelineRef GetPipelineRemote(IPipelineInstance user, IPEndPoint remote, string path)
        //{
        //    string url = remote.Address.ToString() + ":" + remote.Port + "/"+path;
        //    return refActors[url];
        //}
        NEL.Peer.Tcp.IPeer peer;
        public unsafe void OpenNetwork(PeerOption option)
        {
            if (peer != null)
                throw new Exception("already have init peer.");
            peer = Peer.Tcp.PeerV2.PeerV2.CreatePeer(logger);
            peer.Start(option);
            peer.OnClosed += (id) =>
              {
                  if (this.linkedIP.TryRemove(id, out string _remotestr))
                  {
                      if (this.refSystems.TryRemove(_remotestr, out ISystemPipeline remote))
                      {
                          (remote as RefSystemRemote).Close(id);
                      }
                      Console.WriteLine("close line=" + id);
                  }
              };
            peer.OnLinkError += (id, err) =>
            {
                Console.WriteLine("OnLinkError line=" + id + " ,err=" + err.ToString());
                //var remotestr = linkedIP[id];
                //if (this.refSystems.TryRemove(remotestr, out ISystemPipeline remote))
                //{
                //    (remote as RefSystemRemote).Close(); ;
                //}
            };
            peer.OnRecv += (id, data) =>
            {
                //if (data.Length == 0)
                //{
                //    throw new Exception("err h01");
                //}
                int seek = 0;
                var fromlen = data[seek]; seek++;
                string from = System.Text.Encoding.UTF8.GetString(data, seek, fromlen); seek += fromlen;
                var tolen = data[seek];  seek++;
                string to = System.Text.Encoding.UTF8.GetString(data, seek, tolen); seek += tolen;
                //if (from == "" || to =="")
                //{
                //    throw new Exception("err h02");
                //}
                var remotestr = linkedIP[id];
                var refsys = this.refSystems[remotestr];
                var pipe = this.GetPipelineByFrom(refsys, "@" + id + "/" + from, "this/" + to) as PipelineRefLocal;
                //var pipe = this.GetPipeline(user, "this/" + to);
                var outbytes = new byte[data.Length - seek];
                fixed (byte* pdata = data, pout = outbytes)
                {
                    Buffer.MemoryCopy(pdata + seek, pout, outbytes.Length, outbytes.Length);
                }
                pipe.TellDirect(outbytes);
            };

            peer.OnAccepted += (ulong id, IPEndPoint endpoint) =>
            {
                var remotestr = endpoint.ToString();
                linkedIP[id] = remotestr;
                RefSystemRemote remote = new RefSystemRemote(this, peer, endpoint, id, true);
                remote.linked = true;
                (remote as RefSystemRemote).Linked(id, true, endpoint);
                this.refSystems[remotestr] = remote;

                Console.WriteLine("on accepted." + id + " = " + endpoint);
            };

            peer.OnConnected += (ulong id, IPEndPoint endpoint) =>
              {
                  if (this.linkedIP.ContainsKey(id) == false)
                  {
                      var __remotestr = endpoint.ToString();
                      this.linkedIP[id] = __remotestr;
                      RefSystemRemote __remote = new RefSystemRemote(this, peer, endpoint, id, false);
                      __remote.linked = true;
                      this.refSystems[__remotestr] = __remote;
                      __remote.Linked(id, false, endpoint);
                  }
                  else
                  {
                      lock (endpoint)//这个锁比较粗糙，临时增加，因为有可能Connect之后执行了一半，进入这里
                      //后面考虑从更底层做操作
                      {
                          var remotestr = this.linkedIP[id];
                          if (this.refSystems.ContainsKey(remotestr) == false)
                          {
                              logger.Warn("意外的值");
                          }
                          var remote = this.refSystems[remotestr] as RefSystemRemote;
                          remote.linked = true;
                          (remote as RefSystemRemote).Linked(id, false, endpoint);
                      }
                  }
                  //this.linkedIP[id] = endpoint.ToString();

                  ////主动连接成功，创建一个systemRef
                  //var remotestr = this.linkedIP[id];
                  //RefSystemRemote remote = new RefSystemRemote(peer, remotestr, id);
                  //remote.linked = true;
                  //this.refSystems[remotestr] = remote;
                  Console.WriteLine("on OnConnected." + id + " = " + endpoint);
              };
        }
        public void CloseNetwork()
        {
            if (peer == null)
                return;
            peer.Close();
            peer = null;
        }
        public void OpenListen(IPEndPoint host)
        {
            if (peer == null)
                throw new Exception("not init peer.");

            peer.Listen(host);
        }

        public void CloseListen()
        {
            if (peer == null)
                return;

            peer.StopListen();
        }
        public ISystemPipeline Connect(IPEndPoint _remote)
        {
            if (peer == null)
                throw new Exception("not init peer.");

            //lock (this)
            //{
            var linkid = peer.Connect(_remote);
            if (this.linkedIP.ContainsKey(linkid) == false)
            {
                lock (_remote)//这个锁比较粗糙
                {
                    var remotestr = _remote.ToString();
                    this.linkedIP[linkid] = remotestr;

                    //主动连接成功，创建一个systemRef
                    RefSystemRemote remote = new RefSystemRemote(this, peer, _remote, linkid, false);
                    remote.linked = false;
                    this.refSystems[remotestr] = remote;
                    return remote;
                }
            }
            else
            {
                var remotestr = this.linkedIP[linkid];
                return this.refSystems[remotestr];
            }
            //}
        }
        public void DisConnect(ISystemPipeline pipe)
        {
            var id = pipe.PeerID;
            this.peer.Disconnect(pipe.PeerID);

        }
        public async Task<ISystemPipeline> ConnectAsync(IPEndPoint remote)
        {
            var pipe = Connect(remote);
            //var linkid = peer.Connect(remote);
            //var remotestr = remote.ToString();
            ////这里处理linkid时机不稳定，还是最好放在on connect事件中处理
            ////linkedIP[linkid] = remotestr;


            while (true)
            {
                await global::System.Threading.Tasks.Task.Delay(100);
                if (pipe.linked)
                {
                    return pipe;
                }
            }
        }




        //public ICollection<string> GetAllPipelinePath()
        //{
        //    return refPipelines.Keys;
        //}
        public ICollection<string> GetAllSystemsPath()
        {
            return refSystems.Keys;
        }
        public ICollection<ISystemPipeline> GetAllSystems()
        {
            return refSystems.Values;
        }




    }
}
