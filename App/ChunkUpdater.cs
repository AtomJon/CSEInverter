using System;

namespace CSEInverter
{
    public class ChunkUpdater
    {
        private readonly Action<TaskProgressUpdateArgs> ProgressUpdate;
        private readonly string Status;

        private readonly int BufferSize;
        private readonly int StepsOnProgressBar;

        int iterationsPerUpdate = -1;

        public bool shouldUpdate;
        public bool dontUpdate { get => !shouldUpdate; }

        public ChunkUpdater(Action<TaskProgressUpdateArgs> progressUpdate, string status, int bufferSize, int stepsOnProgressBar)
        {
            ProgressUpdate = progressUpdate;
            Status = status;

            BufferSize = bufferSize;
            StepsOnProgressBar = stepsOnProgressBar;

            shouldUpdate = CanUpdateProgress();
        }

        private bool CanUpdateProgress()
        {
            return ProgressUpdate != null && !String.IsNullOrEmpty(Status) && BufferSize > 0;
        }

        public void CalculateUpdates(long operationSize)
        {
            if (ProgressUpdate != null)
                UpdateTaskStartet();

            if (dontUpdate || operationSize < 1 || StepsOnProgressBar < 1 || BufferSize < 1) return;

            int amountOfWork = (int)Math.Ceiling((double)operationSize / BufferSize);

            iterationsPerUpdate = amountOfWork / StepsOnProgressBar;
            if (iterationsPerUpdate < 1) iterationsPerUpdate = 1;

            int progressUpdates = amountOfWork / iterationsPerUpdate;

            UpdateAddWork(progressUpdates);
        }

        public void UpdateOnIteration(int iteration)
        {
            if (dontUpdate || iterationsPerUpdate < 1) return;

            if ((iteration + 1) % iterationsPerUpdate == 0)
            {
                UpdateWorkDone();
            }
        }

        public void FinalizeProgress()
        {
            if (ProgressUpdate != null)
                UpdateTaskFinished();
        }

        private void UpdateTaskStartet()
        {
            Update(TaskProgressUpdateType.TaskStartet);
        }

        private void UpdateTaskFinished()
        {
            Update(TaskProgressUpdateType.TaskFinished);
        }

        private void UpdateAddWork(int work)
        {
            Update(TaskProgressUpdateType.AddWork, work);
        }

        private void UpdateWorkDone()
        {
            Update(TaskProgressUpdateType.WorkDone);
        }

        private void Update(TaskProgressUpdateType type, int work = -1)
        {
            ProgressUpdate(new() { Type = type, AmountOfWork = work, WorkStatus = Status });
        }
    }
}
