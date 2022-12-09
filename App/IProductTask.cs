using System;
using System.Collections.Generic;
using System.IO;

namespace CSEInverter
{
    public interface IProductTask
    {
        public string GetDescription();
        public void Initiate(TaskArguments args);
        public void Run(TaskArguments args);
    }

    public struct TaskArguments
    {
        public Stream Stream;

        public int? LinesInStream;
        public long? StreamSize;

        public string FileName;

        public LinkedListNode<IProductTask> Node;

        public Action<TaskProgressUpdateArgs> ProgressUpdate;
    }

    public struct TaskProgressUpdateArgs
    {
        public TaskProgressUpdateType Type;

        public string WorkStatus;

        public int AmountOfWork;
    }

    public enum TaskProgressUpdateType
    {
        AddWork,
        WorkDone,
        TaskStartet,
        TaskFinished
    }
}
