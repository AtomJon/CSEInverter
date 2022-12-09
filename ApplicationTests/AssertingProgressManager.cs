using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSEInverter.Tests
{
    public struct Counter
    {
        public int Value;
        public int Maximum;

        public Counter(int maximum)
        {
            Value = 0;
            Maximum = maximum;
        }

        public override string ToString()
        {
            return $"Value: {Value}, Max: {Maximum}";
        }
    }

    public class AssertingProgressManager : IProgressManager<Counter>
    {
        bool WasWorkAdded = false;
        bool WasStartet = false;
        bool WasFinished = false;

        bool BarShouldBeAdded;
        bool ShouldBeStartetAndFinished;

        public AssertingProgressManager(bool shouldBarBeAdded, bool shouldBeStartetAndFinished)
        {
            BarShouldBeAdded = shouldBarBeAdded;
            ShouldBeStartetAndFinished = shouldBeStartetAndFinished;
        }

        public void ProgressUpdate(TaskProgressUpdateArgs args)
        {
            //Logger.WriteLine($"Type: {args.Type}, status: {args.WorkStatus}, Work: {args.AmountOfWork}", true);

            switch (args.Type)
            {
                case TaskProgressUpdateType.AddWork:
                    UpdateBarMaximum(args.WorkStatus, args.AmountOfWork);
                    break;
                case TaskProgressUpdateType.WorkDone:
                    IncreaseBarValue(args.WorkStatus);
                    break;
                case TaskProgressUpdateType.TaskStartet:
                    progressBars.Add(args.WorkStatus, new());
                    WasStartet = true;
                    break;
                case TaskProgressUpdateType.TaskFinished:
                    progressBars.Remove(args.WorkStatus);
                    WasFinished = true;
                    break;
            }
        }

        public override void UpdateBarMaximum(string status, int maximum)
        {
            //Assert.IsTrue(BarShouldBeAdded, "Bar was not supposed to be added");
            Assert.IsTrue(maximum > 0, "An zero-work update was made");

            WasWorkAdded = true;

            Counter counter = progressBars[status];
            counter.Maximum = maximum;

            progressBars[status] = counter;
        }

        public override void IncreaseBarValue(string status)
        {
            Counter bar = progressBars[status];
            bar.Value++;

            progressBars[status] = bar;
        }

        public void AssertUpdates()
        {
            if (BarShouldBeAdded)
            {
                Assert.IsTrue(WasWorkAdded, "Task did not make a progress bar");
            }

            if (ShouldBeStartetAndFinished)
            {
                Assert.IsTrue(WasStartet, "Task was not startet");
                Assert.IsTrue(WasFinished, "Task was not finished");
            }

            Assert.IsTrue(NoneBarsAreLeft(), "Task did not clean up progress bars");
        }
    }
}
