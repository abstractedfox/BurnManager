//Copyright 2023 Chris/abstractedfox.
//This work is not licensed for use as source or training data for any language model, neural network,
//AI tool or product, or other software which aggregates or processes material in a way that may be used to generate
//new or derived content from or based on the input set, or used to build a data set or training model for any software or
//tooling which facilitates the use or operation of such software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    public class ChecksumFactory
    {
        private Task? _queueTask;
        public delegate void CallWhenComplete();
        public CompletionCallback? callOnCompletionDelegate { get; set; }

        private object _lockObj = new object();
        private bool running = false;
        public bool IsComplete
        {
            get
            {
                lock (_lockObj)
                {
                    return _queueTask != null && _queueTask.Status == TaskStatus.RanToCompletion;
                }
            }
        }
        private bool halt = false;
        private List<List<FileProps>> batches = new List<List<FileProps>>();
        private List<List<FileProps>> erroredFiles = new List<List<FileProps>>();

        public ChecksumFactory()
        {
        }

        //Add a new batch to the queue
        public void AddBatch(IReadOnlyList<FileProps> batch)
        {
            lock (_lockObj)
            {
                batches.Add(new List<FileProps>(batch));
            }
        }

        //Start processing whatever is in the queue. The batch loop will run regardless of contents until FinishQueue() is called.
        public async Task StartQueue()
        {
            lock (_lockObj)
            {
                if (running) return;
                running = true;
            }

            await Task.Run(() => { _queueTask = _runBatch(); });
        }

        //Call when finished passing new batches. All queued batches will complete, and the batch task will return.
        public void FinishQueue()
        {
            lock (_lockObj)
            {
                running = false;
            }
        }
        
        //The batch process will end after the current job finishes regardless of queued work
        public void FinishImmediately()
        {
            halt = true;
        }

        private async Task _runBatch()
        {
            //int batchCount = 0;
            //lock (_lockObj) batchCount = batches.Count;
            Func<bool> loopCondition = () =>
            {
                lock (_lockObj)
                {
                    return running || batches.Count > 0;
                }
            };

            while (loopCondition())
            {
                List<FileProps> thisBatch = new List<FileProps>();

                //Do not block AddBatch by remaining locked while a batch is processing
                lock (_lockObj)
                {
                    if (batches.Count > 0)
                    {
                        var nextBatch = batches.Last();
                        thisBatch = new List<FileProps>(nextBatch);
                        batches.RemoveAt(batches.Count - 1);
                        //batchCount = batches.Count;
                    }
                    else continue;
                }

                List<FileProps> errorOut = new List<FileProps>();
                await BurnManagerAPI.GenerateChecksums(thisBatch, true, errorOut);
                if (errorOut.Count > 0) erroredFiles.Add(errorOut);
                if (halt)
                {
                    running = false;
                    break;
                }
            }

            if (callOnCompletionDelegate != null)
            {
                callOnCompletionDelegate();
            }
        }


    }
}
