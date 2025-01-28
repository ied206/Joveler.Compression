using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Joveler.Compression.ZLib
{
    internal class ThrottlingActionBlock<T> : ITargetBlock<T>
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ActionBlock<T> _actionBlock;

        private ITargetBlock<T> TargetBlock => _actionBlock;

        public ThrottlingActionBlock(Action<T> action, int capacity, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            _semaphore = new SemaphoreSlim(capacity);
            dataflowBlockOptions.BoundedCapacity = DataflowBlockOptions.Unbounded;

            _actionBlock = new ActionBlock<T>(t =>
            {
                try
                {
                    action(t);
                }
                finally
                {
                    _semaphore.Release();
                }
            }, dataflowBlockOptions);
        }

        #region ITargetBlock<T> members
        DataflowMessageStatus ITargetBlock<T>.OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept)
        {
            _semaphore.Wait();
            return TargetBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }
        #endregion

        #region IDataflowBlock members
        public Task Completion => _actionBlock.Completion;

        public void Complete()
        {
            _actionBlock.Complete();
        }

        public void Fault(Exception exception)
        {
            TargetBlock.Fault(exception);
        }
        #endregion
    }

    internal class ThrottlingTransformBlock<TInput, TOutput> : IPropagatorBlock<TInput, TOutput>, IReceivableSourceBlock<TOutput>
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly TransformBlock<TInput, TOutput> _transformBlock;

        private ITargetBlock<TInput> TargetBlock => _transformBlock;
        private IReceivableSourceBlock<TOutput> SourceBlock => _transformBlock;


        public ThrottlingTransformBlock(Func<TInput, TOutput> func, int capacity, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            _semaphore = new SemaphoreSlim(capacity);

            dataflowBlockOptions.BoundedCapacity = DataflowBlockOptions.Unbounded;
            _transformBlock = new TransformBlock<TInput, TOutput>(t =>
            {
                try
                {
                    return func(t);
                }
                finally
                {
                    _semaphore.Release();
                }
            }, dataflowBlockOptions);
        }

        #region ITargetBlock<T> members
        DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept)
        {
            _semaphore.Wait();
            return TargetBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }
        #endregion

        #region IReceivableSourceBlock<T> members
#pragma warning disable CS8767 // Ignore nullable in signature
        public bool TryReceive(Predicate<TOutput>? filter, out TOutput? item)
#pragma warning restore CS8767 // Ignore nullable in signature
        {
            return SourceBlock.TryReceive(filter, out item);
        }

#pragma warning disable CS8767 // Ignore nullable in signature
        public bool TryReceiveAll(out IList<TOutput>? items)
#pragma warning restore CS8767 // Ignore nullable in signature
        {
            return SourceBlock.TryReceiveAll(out items);
        }
        #endregion

        #region ISourceBlock<T> members
        public IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        {
            return SourceBlock.LinkTo(target, linkOptions);
        }

        bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            return SourceBlock.ReserveMessage(messageHeader, target);
        }

        TOutput? ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
        {
            return SourceBlock.ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            SourceBlock.ReleaseReservation(messageHeader, target);
        }
        #endregion

        #region IDataflowBlock members
        public Task Completion => SourceBlock.Completion;

        public void Complete()
        {
            SourceBlock.Complete();
        }

        public void Fault(Exception exception)
        {
            TargetBlock.Fault(exception);
        }
        #endregion
    }

    internal class ThrottlingBufferBlock<T> : IPropagatorBlock<T, T>, IReceivableSourceBlock<T>
    {
        private readonly int _capacity;
        private readonly ManualResetEvent _bufferFullEvent = new ManualResetEvent(true);
        private readonly BufferBlock<T> _bufferBlock;

        private ITargetBlock<T> TargetBlock => _bufferBlock;
        private IReceivableSourceBlock<T> SourceBlock => _bufferBlock;


        public ThrottlingBufferBlock(int capacity, DataflowBlockOptions dataflowBlockOptions)
        {
            _capacity = capacity;

            dataflowBlockOptions.BoundedCapacity = DataflowBlockOptions.Unbounded;
            _bufferBlock = new BufferBlock<T>(dataflowBlockOptions);
        }

        private void CheckBufferFullEvent()
        {
            if (_capacity <= _bufferBlock.Count)
                _bufferFullEvent.Reset();
            else
                _bufferFullEvent.Set();
        }

        #region ITargetBlock<T> members
        DataflowMessageStatus ITargetBlock<T>.OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept)
        {
            _bufferFullEvent.WaitOne();

            DataflowMessageStatus ret = TargetBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);

            if (ret == DataflowMessageStatus.Accepted)
                CheckBufferFullEvent();

            return ret;
        }
        #endregion

        #region IReceivableSourceBlock<T> members
#pragma warning disable CS8767 // Ignore nullable in signature
        public bool TryReceive(Predicate<T>? filter, out T? item)
#pragma warning restore CS8767 // Ignore nullable in signature
        {
            bool ret = SourceBlock.TryReceive(filter, out item);
            if (item != null)
                CheckBufferFullEvent();
            return ret;
        }

#pragma warning disable CS8767 // Ignore nullable in signature
        public bool TryReceiveAll(out IList<T>? items)
#pragma warning restore CS8767 // Ignore nullable in signature
        {
            bool ret = SourceBlock.TryReceiveAll(out items);
            if (items != null && 0 < items.Count)
                CheckBufferFullEvent();
            return ret;
        }
        #endregion

        #region ISourceBlock<T> members
        public IDisposable LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions)
        {
            return SourceBlock.LinkTo(target, linkOptions);
        }

        bool ISourceBlock<T>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            return SourceBlock.ReserveMessage(messageHeader, target);
        }

        T? ISourceBlock<T>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target, out bool messageConsumed)
        {
            T? item = SourceBlock.ConsumeMessage(messageHeader, target, out messageConsumed);

            if (item != null)
            {
                if (_capacity <= _bufferBlock.Count)
                    _bufferFullEvent.Reset();
                else
                    _bufferFullEvent.Set();
            }

            return item;
        }

        void ISourceBlock<T>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            SourceBlock.ReleaseReservation(messageHeader, target);
        }
        #endregion

        #region IDataflowBlock members
        public Task Completion => SourceBlock.Completion;

        public void Complete()
        {
            SourceBlock.Complete();
        }

        public void Fault(Exception exception)
        {
            TargetBlock.Fault(exception);
        }
        #endregion
    }
}
