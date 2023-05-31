using BurnManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnManagerFront
{
    internal class AddFoldersRecursiveAndGenerateChecksums : AddFoldersRecursive
    {
        public new CompletionCallback? callOnCompletionDelegate { get; set; }
        private ChecksumFactory _checksumProcedure = new ChecksumFactory();
        private Task? _checksumHandoffTask;
        public AddFoldersRecursiveAndGenerateChecksums(BurnManagerAPI api, CompletionCallback callback) : base(api)
        {
            callOnCompletionDelegate = callback;
        }

        public new void StartOperation()
        {
            base.StartOperation();
            _checksumProcedure.StartOperation();
            _checksumHandoffTask = _mainTask();
        }

        public new void EndWhenComplete()
        {
            base.EndWhenComplete();
        }

        public new void EndImmediately()
        {
            base.EndImmediately();
            _checksumProcedure.EndImmediately();
        }

        private async Task _mainTask()
        {
            //Doesn't need to check the halt condition on its own as that's already done in the base class
            await Task.Run(() =>
            {
                while (base._isCurrentlyRunning)
                {
                    IReadOnlyList<FileProps>? checksumBuffer = base.GetBufferContentsAndClear(100);
                    if (!(checksumBuffer is null))
                    {
                        _checksumProcedure.AddBatch(checksumBuffer);
                    }
                }

                _end();
            });
        }

        private void _end()
        {
            //optimization candidate! (maybe we can expose the tasks in the other classes and await them?)
            IReadOnlyList<FileProps>? checksumBuffer = base.GetBufferContentsAndClear(0);
            if (!(checksumBuffer is null))
            {
                _checksumProcedure.AddBatch(checksumBuffer);
            }
            _checksumProcedure.EndWhenComplete();
            while (base._isCurrentlyRunning || !_checksumProcedure.IsComplete) ;

            if (!(callOnCompletionDelegate is null))
            {
                callOnCompletionDelegate();
            }
        }
    }
}
