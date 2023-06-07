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
    public class ChecksumFactory : ILongRunningProcedure
    {
        private Task? _queueTask;
        public Action? callOnCompletionDelegate { get; set; }

        private object _lockObj = new object();
        private bool _shouldAlwaysRun = false;
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
        private bool _halt = false;
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
        public void StartOperation()
        {
            lock (_lockObj)
            {
                if (_shouldAlwaysRun) return;
                _shouldAlwaysRun = true;
            }

            _queueTask = _mainTask();
        }

        //Call when finished passing new batches. All queued batches will complete, and the batch task will return.
        public void EndWhenComplete()
        {
            lock (_lockObj)
            {
                _shouldAlwaysRun = false;
            }
        }
        
        //The batch process will end after the current job finishes regardless of queued work
        public void EndImmediately()
        {
            _halt = true;
        }

        private async Task _mainTask()
        {
            Func<bool> loopCondition = () =>
            {
                lock (_lockObj)
                {
                    if (!(!_halt && (_shouldAlwaysRun || batches.Count > 0)))
                    {
                        Console.WriteLine("boilerplate!");
                    }
                    return !_halt && (_shouldAlwaysRun || batches.Count > 0);
                }
            };

            await Task.Run(async () => { 
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
                        }
                        else continue;
                    }

                    List<FileProps> errorOut = new List<FileProps>();
                    await BurnManagerAPI.GenerateChecksums(thisBatch, true, errorOut);
                    if (errorOut.Count > 0) erroredFiles.Add(errorOut);

                    if (_halt)
                    {
                        _shouldAlwaysRun = false;
                        break;
                    }
                }

                if (callOnCompletionDelegate != null)
                {
                    callOnCompletionDelegate();
                }
            });
        }


    }
}
