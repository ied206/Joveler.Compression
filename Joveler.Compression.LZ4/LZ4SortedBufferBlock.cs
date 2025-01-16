using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Joveler.Compression.LZ4
{
    internal sealed class LZ4SortedBufferBlock : IPropagatorBlock<LZ4ParallelCompressJob, LZ4ParallelCompressJob>, IReceivableSourceBlock<LZ4ParallelCompressJob>
    {
        private long _outSeq = 0;
        public long OutSeq => _outSeq;

        private readonly object _outSetLock = new object();
        private readonly SortedSet<LZ4ParallelCompressJob> _outSet = new SortedSet<LZ4ParallelCompressJob>(new LZ4ParallelCompressJobComparator());

        private readonly ITargetBlock<LZ4ParallelCompressJob> _targetBlock;
        private readonly IReceivableSourceBlock<LZ4ParallelCompressJob> _sourceBlock;


        public LZ4SortedBufferBlock(CancellationTokenSource cancelTokenSrc)
        {
            BufferBlock<LZ4ParallelCompressJob> sourceBlock = new BufferBlock<LZ4ParallelCompressJob>();
            _sourceBlock = sourceBlock;

            // Flow of SortedBufferBlock
            // [IN]
            // 1. A job comes in is added to the SortedSet
            // [OUT] When the job of right seq is available
            // 1. Remove a job of right seq from the SortedSet
            // 2. Post it to the next DataFlow Block.

            // Receive a LZ4ParallelCompressJob (which completed compressing), then put it into sorted list
            _targetBlock = new ActionBlock<LZ4ParallelCompressJob>(item =>
            {
                // Receive a LZ4ParallelCompressJob (which completed compressing), then put it into sorted list
                _outSet.Add(item);

                // Check if the jobs of right seq is available.
                // If available, post all of the designated jobs.
                while (0 < _outSet.Count)
                {
                    LZ4ParallelCompressJob? job = _outSet.FirstOrDefault(x => x.Seq == _outSeq);
                    if (job == null)
                        break;

                    _outSeq += 1;
                    bool isLastBlock = job.IsLastBlock;

                    sourceBlock.Post(job);

                    if (isLastBlock)
                        Complete();
                }
            },
            new ExecutionDataflowBlockOptions()
            {
                CancellationToken = cancelTokenSrc.Token,
                EnsureOrdered = false,
                MaxDegreeOfParallelism = 1,
            });
        }
        

        #region ActionBlock: Put job into the SortedSet
        public void AddSortedSet(LZ4ParallelCompressJob job)
        {
            lock (_outSetLock)
                _outSet.Add(job);
        }
        #endregion

        #region IReceivableSourceBlock<LZ4ParallelCompressJob> members
#pragma warning disable CS8767 // Ignore nullable in signature
        public bool TryReceive(Predicate<LZ4ParallelCompressJob>? filter, out LZ4ParallelCompressJob? item)
#pragma warning restore CS8767 // Ignore nullable in signature
        {
            return _sourceBlock.TryReceive(filter, out item);
        }

#pragma warning disable CS8767 // Ignore nullable in signature
        public bool TryReceiveAll(out IList<LZ4ParallelCompressJob>? items)
#pragma warning restore CS8767 // Ignore nullable in signature
        {
            return _sourceBlock.TryReceiveAll(out items);
        }
        #endregion

        #region ISourceBlock<LZ4ParallelCompressJob> members
        public IDisposable LinkTo(ITargetBlock<LZ4ParallelCompressJob> target, DataflowLinkOptions linkOptions)
        {
            return _sourceBlock.LinkTo(target, linkOptions);
        }

        bool ISourceBlock<LZ4ParallelCompressJob>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<LZ4ParallelCompressJob> target)
        {
            return _sourceBlock.ReserveMessage(messageHeader, target);
        }

        LZ4ParallelCompressJob? ISourceBlock<LZ4ParallelCompressJob>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<LZ4ParallelCompressJob> target, out bool messageConsumed)
        {
            return _sourceBlock.ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        void ISourceBlock<LZ4ParallelCompressJob>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<LZ4ParallelCompressJob> target)
        {
            _sourceBlock.ReleaseReservation(messageHeader, target);
        }
        #endregion

        #region ITargetBlock<LZ4ParallelCompressJob> members
        DataflowMessageStatus ITargetBlock<LZ4ParallelCompressJob>.OfferMessage(DataflowMessageHeader messageHeader, LZ4ParallelCompressJob messageValue, ISourceBlock<LZ4ParallelCompressJob>? source, bool consumeToAccept)
        {
            return _targetBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }
        #endregion

        #region IDataflowBlock members
        public Task Completion => _sourceBlock.Completion;

        public void Complete()
        {
            _sourceBlock.Complete();
        }

        public void Fault(Exception exception)
        {
            _targetBlock.Fault(exception);
        }
        #endregion
    }
}
