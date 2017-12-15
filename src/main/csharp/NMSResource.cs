﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;

namespace NMS.AMQP
{
    using Util;
    namespace Resource
    {
        public enum Mode
        {
            Stopped,
            Starting,
            Started,
            Stopping
        }
    }

    internal abstract class NMSResource : NMSResource<ResourceInfo>
    {

    }

    /// <summary>
    /// NMSResource abstracts the Implementation of IStartable and IStopable for Key NMS class implemetations.
    /// Eg, Connection, Session, MessageConsumer, MessageProducer, etc.
    /// It layouts a foundation for a state machine given by the states in NMS.AMQP.Resource.Mode where 
    /// in general the transitions are Stopped->Starting->Started->Stopping->Stopped->...
    /// </summary>
    internal abstract class NMSResource<T> : IStartable, IStoppable where T : ResourceInfo
    {
        private T info;
        protected T Info
        {
            get { return info; }
            set
            {
                if (value != null)
                {
                    info = value;
                }
            }
        }

        public virtual Id Id
        {
            get
            {
                if(info != null)
                {
                    return info.Id;
                }
                return null;
            }
        }

        protected NMSResource() { }

        protected Atomic<Resource.Mode> mode = new Atomic<Resource.Mode>(Resource.Mode.Stopped);

        public virtual Boolean IsStarted { get { return mode.Value.Equals(Resource.Mode.Started); } }

        protected abstract void StartResource();
        protected abstract void StopResource();
        protected abstract void ThrowIfClosed();

        public void Start()
        {
            ThrowIfClosed();
            if (!IsStarted && mode.CompareAndSet(Resource.Mode.Stopped, Resource.Mode.Starting))
            {
                Resource.Mode finishedMode = Resource.Mode.Stopped;
                try
                {
                    this.StartResource();
                    finishedMode = Resource.Mode.Started;
                }
                catch (Exception e)
                {
                    if(e is NMSException)
                    {
                        throw e;
                    }
                    else
                    {
                        throw new NMSException("Failed to Start resource.", e);
                    }
                }
                finally
                {
                    this.mode.GetAndSet(finishedMode);
                }
            }
        }

        public void Stop()
        {
            ThrowIfClosed();
            if (mode.CompareAndSet(Resource.Mode.Started, Resource.Mode.Stopping))
            {
                Resource.Mode finishedMode = Resource.Mode.Started;
                try
                {
                    this.StopResource();
                    finishedMode = Resource.Mode.Stopped;
                }
                catch (Exception e)
                {
                    if (e is NMSException)
                    {
                        throw e;
                    }
                    else
                    {
                        throw new NMSException("Failed to Stop resource.", e);
                    }
                }
                finally
                {
                    this.mode.GetAndSet(finishedMode);
                }
            }
        }
    }

    internal abstract class ResourceInfo
    {

        private readonly Id resourceId;

        protected ResourceInfo(Id resourceId)
        {
            this.resourceId = resourceId;
        }

        public virtual Id Id { get { return resourceId; } }
    }
}
