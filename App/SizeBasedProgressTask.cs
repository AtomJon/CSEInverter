using System;
using System.ComponentModel;
using System.IO;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace CSEInverter
{
    [CategoryOrder("Status", 1)]
    public abstract class SizeBasedProgressTask : IProductTask
    {
        [Category("Status"), DisplayName("Opdater Status Bar"), PropertyOrder(0), Description("Slå Opdateringer Fra For At Få Mere Fart")]
        public bool UpdateProgress { get; set; } = true;

        [Category("Status"), DisplayName("Skridt På Status Bar"), PropertyOrder(1), Description("Hvor Mange Gange Skal Status Baren Opdateres (Færre opdateringer, mere fart)")]
        public int StepsOnProgressBar { get; set; } = 40;

        Action<TaskProgressUpdateArgs> ProgressUpdate;
        string Status;

        int iterationsPerUpdate;

        protected bool shouldUpdate;
        protected bool dontUpdate { get => !shouldUpdate; }

        public abstract string GetDescription();
        public abstract void Initiate(TaskArguments args);
        public abstract void Run(TaskArguments args);

        protected virtual bool ShouldUpdateProgress() => true;

        protected bool CanUpdateProgress()
        {
            return UpdateProgress && ProgressUpdate != null && StepsOnProgressBar > 0 && ShouldUpdateProgress();
        }

        protected void InitializeProgress(Action<TaskProgressUpdateArgs> progressUpdate, string status)
        {
            ProgressUpdate = progressUpdate;
            Status = status;

            shouldUpdate = CanUpdateProgress();

            if (ProgressUpdate != null)
                UpdateTaskStartet();
        }

        protected void ReadLinesResetStreamAndCalculateUpdates(StreamReader reader)
        {
            if (dontUpdate) return;

            int lines = ReadLinesInStreamReader(reader);
            reader.BaseStream.Position = 0;

            CalculateUpdates(lines);
        }

        protected void CalculateUpdates(int max)
        {
            if (dontUpdate) return;

            int amountOfWork;
            if (StepsOnProgressBar > max)
            {
                if (max < 1) max = 1;
                amountOfWork = max;
                iterationsPerUpdate = 1;
            }
            else
            {
                iterationsPerUpdate = (int)Math.Floor(max / (double)StepsOnProgressBar);
                amountOfWork = max / iterationsPerUpdate;
            }

            UpdateAddWork(amountOfWork);
        }

        private int ReadLinesInStreamReader(StreamReader reader)
        {
            int amountOfLines = 0;
            while (reader.ReadLine() != null) { amountOfLines++; }

            return amountOfLines;
        }

        public void UpdateOnIteration(int iteration)
        {
            if (dontUpdate) return;

            if (iteration % iterationsPerUpdate == 0)
            {
                UpdateWorkDone();
            }
        }

        protected void FinalizeProgress()
        {
            if (ProgressUpdate != null)
                UpdateTaskFinished();
        }

        private void UpdateTaskStartet()
        {
            CheckAndUpdate(TaskProgressUpdateType.TaskStartet);
        }

        private void UpdateTaskFinished()
        {
            CheckAndUpdate(TaskProgressUpdateType.TaskFinished);
        }

        private void UpdateAddWork(int work)
        {
            CheckAndUpdate(TaskProgressUpdateType.AddWork, work);
        }

        private void UpdateWorkDone()
        {
            CheckAndUpdate(TaskProgressUpdateType.WorkDone);
        }

        private void CheckAndUpdate(TaskProgressUpdateType type, int work = -1)
        {
            ProgressUpdate(new() { Type = type, AmountOfWork = work, WorkStatus = Status });
        }
    }
}
