using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    public interface ILongRunningProcedure
    {
        public CompletionCallback? callOnCompletionDelegate { get; set; }
        public bool IsComplete { get; }

        public Task StartOperation();

        //Indicate that the operation should finish whatever is in queue and exit.
        public void EndWhenComplete();

        //Indicate that the operation should finish as soon as possible, regardless of work in queue
        public void EndImmediately();
    }
}
