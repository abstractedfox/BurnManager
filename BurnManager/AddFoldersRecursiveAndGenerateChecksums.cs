using BurnManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    public class AddFoldersRecursiveAndGenerateChecksums : AddFoldersRecursive
    {
        public new Action? callOnCompletionDelegate { get; set; }
        private ChecksumFactory _checksumProcedure = new ChecksumFactory();
        private Task? _checksumHandoffTask;
        public AddFoldersRecursiveAndGenerateChecksums(BurnManagerAPI api, Action callback) : base(api)
        {
            callOnCompletionDelegate = callback;
        }

        public override void StartOperation()
        {
            base.StartOperation();
            _checksumProcedure.StartOperation();
            _checksumHandoffTask = _mainTask();
        }

        public override void EndWhenComplete()
        {
            base.EndWhenComplete();
        }

        public override void EndImmediately()
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
                    IReadOnlyList<FileProps>? checksumBuffer = base.GetBufferContentsAndClear(50);
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
