using System;
using System.Collections.Generic;
using System.Text;

namespace NEL.Pipeline
{
    public abstract class Module : IModuleInstance
    {
        public Module(bool MultiThreadTell = true)
        {
            this.MultiThreadTell = MultiThreadTell;
            this.HasDisposed = false;
        }
        public bool MultiThreadTell
        {
            get;
            private set;
        }
        public ISystem _System
        {
            get;
            private set;
        }
        private int _inited;
        public bool Inited //是否已经初始化
        {
            get
            {
                return _inited > 0;
            }
        }
        public string path
        {
            get;
            private set;
        }
        public bool HasDisposed
        {
            get;
            private set;
        }
        public IModulePipeline GetPipeline(string urlActor)
        {
            return _System.GetPipeline(this, urlActor);
        }
        public void OnRegistered(ISystem system, string path)
        {
            this.path = path;
            this._System = system;
        }
        public virtual void Dispose()
        {
            this.HasDisposed = true;
        }
        public void OnStarted()
        {
            if (MultiThreadTell == false)//如果是单线程投递，不用管，有dequeueThread处理
            {

            }
            else
            {//此时
                DequeueThread();
            }

            global::System.Threading.Interlocked.Exchange(ref this._inited, 1);

        }
        class QueueObj
        {
            public IModulePipeline from;
            public byte[] data;
            public object obj;
        }
        System.Collections.Concurrent.ConcurrentQueue<QueueObj> queueObj;
        public void QueueTell(IModulePipeline _from, byte[] _data)
        {
            if (queueObj == null)
            {
                queueObj = new System.Collections.Concurrent.ConcurrentQueue<QueueObj>();
                if (MultiThreadTell == false)//单线程投递则必须开一个线程去处理队列消息
                {
                    global::System.Threading.Thread t = new System.Threading.Thread(DequeueThread);
                    t.IsBackground = true;
                    t.Start();
                }
            }
            queueObj.Enqueue(new QueueObj() { data = _data, from = _from });
        }
        public void QueueTellLocalObj(IModulePipeline _from, object _obj)
        {
            if (queueObj == null)
            {
                queueObj = new System.Collections.Concurrent.ConcurrentQueue<QueueObj>();
                if (MultiThreadTell == false)//单线程投递则必须开一个线程去处理队列消息
                {
                    global::System.Threading.Thread t = new System.Threading.Thread(DequeueThread);
                    t.IsBackground = true;
                    t.Start();
                }
            }
            queueObj.Enqueue(new QueueObj() { data = null, from = _from, obj = _obj });
        }
        void DequeueThread()
        {
            if (queueObj == null)
                return;
            while (MultiThreadTell == false || queueObj.IsEmpty == false)
            {
                if (queueObj.TryDequeue(out QueueObj queueobj))
                {
                    if (queueobj.data != null)
                        this.OnTell(queueobj.from, queueobj.data);
                    else if (queueobj.obj != null)
                        this.OnTellLocalObj(queueobj.from, queueobj.obj);
                    else
                        throw new Exception("error queueobj.");
                }
                System.Threading.Thread.Sleep(1);
            }
        }

        public abstract void OnStart();

        public abstract void OnTell(IModulePipeline from, byte[] data);
        //这个不是必须实现
        public abstract void OnTellLocalObj(IModulePipeline from, object obj);
    }

}
