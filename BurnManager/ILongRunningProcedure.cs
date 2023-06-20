using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BurnManager
{
    public interface ILongRunningProcedure
    {
        public Action? callOnCompletionDelegate { get; set; } //Delegate to call when the running process completes

        public bool IsComplete { get; }

        //Start running the operation
        public void StartOperation();

        //Indicate that the operation should finish whatever is in queue and exit.
        public void EndWhenComplete();

        //Indicate that the operation should finish as soon as possible, regardless of work in queue
        public void EndImmediately();
    }
}
