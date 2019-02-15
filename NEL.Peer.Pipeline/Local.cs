using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NEL.Pipeline
{
    class PipelineRefLocal : IModulePipeline
    {
        PipelineSystemV1 _System;
        public PipelineRefLocal(PipelineSystemV1 _System, IModuleInstance module)
        {
            this._System = _System;
            this.system = _System.refSystemThis;
            //if (string.IsNullOrEmpty(userPath))
            //    this.userUrl = null;
            //else if (userPath[0] == '@')
            //    this.userUrl = userPath;
            //else
            //    this.userUrl = "this/" + userPath;



            this.path = module.path;
            this.targetModule = module;
        }
        //public void SetFrom()
        //{
        //    try
        //    {
        //        fromPipeline = userUrl == null ? null : _System.GetPipeline(targetModule, userUrl);
        //    }
        //    catch
        //    {
        //        Console.WriteLine("error here.");
        //    }
        //}
        public void SetFromPipeline(IModulePipeline pipeline)
        {
            fromPipeline = pipeline;
        }
        IModulePipeline fromPipeline;
        //指向的模块
        public IModuleInstance targetModule;
        public string userUrl;
        public ISystemPipeline system
        {
            get;
            private set;
        }

        public string path
        {
            get;
            private set;
        }

        public bool IsVaild
        {
            get
            {
                var path = _System.GetModulePath(targetModule);
                bool bExist = string.IsNullOrEmpty(path) == false;
                if (bExist && targetModule.HasDisposed == true)
                {
                    _System.UnRegistModule(path);
                    return false;
                }
                return !targetModule.HasDisposed;
            }
        }

        public bool IsLocal => true;

        public void TellDirect(byte[] data)
        {
            this.targetModule.OnTell(fromPipeline, data);
        }

        public void Tell(byte[] data)
        {
            if (data.Length == 0)
                throw new Exception("do not support  zero length bytearray.");

            if (targetModule.MultiThreadTell == true && targetModule.Inited)
            {//直接开线程投递，不阻塞
                global::System.Threading.ThreadPool.QueueUserWorkItem((s) =>
                {
                    this.targetModule.OnTell(fromPipeline, data);
                }
                );
            }
            else
            {
                //队列投递,不阻塞，队列在内部实现
                this.targetModule.QueueTell(fromPipeline, data);
            }
        }
        public void TellLocalObj(object obj)
        {
            if (obj == null)
                throw new Exception("do not support  null.");

            if (targetModule.MultiThreadTell == true && targetModule.Inited)
            {//直接开线程投递，不阻塞
                global::System.Threading.ThreadPool.QueueUserWorkItem((s) =>
                {
                    this.targetModule.OnTellLocalObj(fromPipeline, obj);
                }
                );
            }
            else
            {
                //队列投递,不阻塞，队列在内部实现
                this.targetModule.QueueTellLocalObj(fromPipeline, obj);
            }
        }
    }
    class PipelineSystemRefLocal : ISystemPipeline
    {
        global::System.Collections.Concurrent.ConcurrentDictionary<string, IModulePipeline> refPipelines;

        public PipelineSystemRefLocal(PipelineSystemV1 system)
        {
            this._System = system;
            refPipelines = new System.Collections.Concurrent.ConcurrentDictionary<string, IModulePipeline>();
        }
        PipelineSystemV1 _System;

        public event Action<UInt64, bool, IPEndPoint> OnPeerLink;
        public event Action<UInt64> OnPeerClose;

        public bool IsLocal => true;

        public string remoteaddr => null;

        public bool linked => false;

        public bool IsHost => false;

        public UInt64 PeerID => 0;
        public IPEndPoint Remote => null;

        public IModulePipeline GetPipeline(IModuleInstance user, string path)
        {
            var pipestr = path + "_";
            if (user != null) pipestr += user.path;
            if (this.refPipelines.TryGetValue(pipestr, out IModulePipeline pipe))
            {
                return pipe;
            }
            IModuleInstance module = this._System.GetModule(path);

            PipelineRefLocal _pipe = new PipelineRefLocal(_System, module);
            this.refPipelines[pipestr] = _pipe;

            var userpipe = user == null ? null : _System.GetPipeline(module, "this/" + user.path);
            _pipe.SetFromPipeline(userpipe);
            return _pipe;
        }

        public IModulePipeline GetPipeLineByFrom(IModulePipeline from, IModuleInstance to)
        {
            var fromstr = from.IsLocal ? from.path : (from.system.Remote.ToString() + "/" + from.path);
            var pipestr = to.path + "_" + fromstr;
            if (this.refPipelines.TryGetValue(pipestr, out IModulePipeline pipe))
            {
                return pipe;
            }
            PipelineRefLocal _pipe = new PipelineRefLocal(_System, to);
            this.refPipelines[pipestr] = _pipe;

            _pipe.SetFromPipeline(from);
            return _pipe;
        }
    }
}
