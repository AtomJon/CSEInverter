using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSEInverter.Tests
{
    [TestClass]
    public class ChunkUpdaterTests
    {
        [TestMethod]
        public void WorkLower()
        {
            UpdateManager updateManager = new();
            var updater = CreateUpdater(updateManager, 2, 10);

            updater.CalculateUpdates(1);
            updater.UpdateOnIteration(0);

            updater.FinalizeProgress();

            updateManager.AssertTimesWorkDoneAndAdded(1);
        }

        [TestMethod]
        public void WorkEqual()
        {
            UpdateManager updateManager = new();
            var updater = CreateUpdater(updateManager, 2, 10);

            updater.CalculateUpdates(2);
            updater.UpdateOnIteration(0);

            updater.FinalizeProgress();

            updateManager.AssertTimesWorkDoneAndAdded(1);
        }

        [TestMethod]
        public void WorkDouble()
        {
            UpdateManager updateManager = new();
            var updater = CreateUpdater(updateManager, 2, 10);

            updater.CalculateUpdates(4);

            updater.UpdateOnIteration(0);
            updater.UpdateOnIteration(1);

            updater.FinalizeProgress();

            updateManager.AssertTimesWorkDoneAndAdded(2);
        }

        [TestMethod]
        public void WorkAlmostTrice()
        {
            UpdateManager updateManager = new();
            var updater = CreateUpdater(updateManager, 4, 10);

            updater.CalculateUpdates(7);

            updater.UpdateOnIteration(0);
            updater.UpdateOnIteration(1);

            updater.FinalizeProgress();

            updateManager.AssertTimesWorkDoneAndAdded(2);
        }

        [TestMethod]
        public void WorkAlmost300Pbar90()
        {
            UpdateManager updateManager = new();
            var updater = CreateUpdater(updateManager, 3, 90);

            updater.CalculateUpdates(299);

            for (int i = 1; i <= 100; i++)
            {
                updater.UpdateOnIteration(i);
            }

            updater.FinalizeProgress();

            updateManager.AssertTimesWorkDoneAndAdded(100);
        }

        [TestMethod]
        public void WorkAlmost300Pbar100()
        {
            UpdateManager updateManager = new();
            var updater = CreateUpdater(updateManager, 3, 100);

            updater.CalculateUpdates(299);

            for (int i = 1; i <= 100; i++)
            {
                updater.UpdateOnIteration(i);
            }

            updater.FinalizeProgress();

            updateManager.AssertTimesWorkDoneAndAdded(100);
        }

        [TestMethod]
        public void SinglePbar()
        {
            UpdateManager updateManager = new();
            var updater = CreateUpdater(updateManager, 1, 1);

            updater.CalculateUpdates(2);

            updater.UpdateOnIteration(0);
            updater.UpdateOnIteration(1);

            updater.FinalizeProgress();

            updateManager.AssertTimesWorkDoneAndAdded(1);
        }

        [TestMethod]
        public void PBar10Steps()
        {
            UpdateManager updateManager = new();
            var updater = CreateUpdater(updateManager, 2, 10);

            updater.CalculateUpdates(20);

            for (int i = 1; i <= 10; i++)
            {
                updater.UpdateOnIteration(i);
            }

            updater.FinalizeProgress();

            updateManager.AssertTimesWorkDoneAndAdded(10);
        }

        [TestMethod]
        public void ZeroPbar()
        {
            UpdateManager updateManager = new();
            var updater = CreateUpdater(updateManager, 1, 0);

            updater.CalculateUpdates(1);

            updater.UpdateOnIteration(0);

            updater.FinalizeProgress();

            updateManager.AssertTimesWorkDoneAndAdded(0);
        }

        [TestMethod]
        public void ZeroUpdates()
        {
            UpdateManager updateManager = new();
            var updater = CreateUpdater(updateManager, 1, 10);

            updater.CalculateUpdates(1);

            updater.FinalizeProgress();

            updateManager.AssertTimesWorkDoneAndAdded(0, false);
        }

        [TestMethod]
        public void NoCalculation()
        {
            UpdateManager updateManager = new();
            var updater = CreateUpdater(updateManager, 1, 10);

            updater.UpdateOnIteration(0);

            updater.FinalizeProgress();

            updateManager.AssertTimesWorkDoneAndAdded(0);
        }

        [TestMethod]
        public void FinalizesAndInitializes()
        {
            UpdateManager updateManager = new();
            var updater = CreateUpdater(updateManager, 1, 1);
            updater.CalculateUpdates(0);

            updater.FinalizeProgress();

            updateManager.AssertInitializeAndFinalize();
        }

        private class UpdateManager
        {
            int AddedWork = 0;
            int AmountOfTimesWorkDoneHaveBeenCalled = 0;

            bool startet = false;
            bool finished = false;

            public void ProgressUpdate(TaskProgressUpdateArgs args)
            {
                switch (args.Type)
                {
                    case TaskProgressUpdateType.AddWork:
                        AddedWork = args.AmountOfWork;
                        break;
                    case TaskProgressUpdateType.WorkDone:
                        AmountOfTimesWorkDoneHaveBeenCalled++;
                        Logger.WriteLine("Done", true);
                        break;
                    case TaskProgressUpdateType.TaskStartet:
                        startet = true;
                        break;
                    case TaskProgressUpdateType.TaskFinished:
                        finished = true;
                        break;
                }
            }

            public void AssertTimesWorkDoneAndAdded(int expectetWorkDone, bool expectWorkAdded = true)
            {
                Assert.AreEqual(expectetWorkDone, AmountOfTimesWorkDoneHaveBeenCalled, "\nIt did not call done the required amount of times");

                if (expectWorkAdded) Assert.AreEqual(expectetWorkDone, AddedWork, "\nThere was not added the expectet amount of work");
            }

            public void AssertInitializeAndFinalize()
            {
                Assert.IsTrue(startet, "\nWork Startet was not called");
                Assert.IsTrue(finished, "\nWork Finished was not called");
            }
        }

        private ChunkUpdater CreateUpdater(UpdateManager updateManager, int bufferSize, int steps) => new(updateManager.ProgressUpdate, "Tester...", bufferSize, steps);
    }
}