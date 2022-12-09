using System.Collections.Generic;
using System.Windows.Controls;

namespace CSEInverter
{
    public abstract class IProgressManager<T>
    {
        protected Dictionary<string, T> progressBars;

        public IProgressManager()
        {
            progressBars = new Dictionary<string, T>();
        }

        public abstract void UpdateBarMaximum(string status, int maximum);

        public abstract void IncreaseBarValue(string status);

        public bool NoneBarsAreLeft()
        {
            return progressBars.Count < 1;
        }
    }

    public class ProgressBarManager : IProgressManager<ProgressBarWithLabel>
    {
        private Panel parentPanel;

        public ProgressBarManager(Panel panel) : base()
        {
            parentPanel = panel;
        }

        public void TaskStartet(string status)
        {
            ProgressBarWithLabel bar = CreateBarAndAddToPanel(status);

            progressBars.Add(status, bar);
        }

        public void TaskFinished(string status)
        {
            RemoveBar(progressBars[status], status);
        }

        private ProgressBarWithLabel CreateBarAndAddToPanel(string barStatus)
        {
            ProgressBarWithLabel bar = new ProgressBarWithLabel(barStatus);
            parentPanel.Children.Add(bar);

            bar.UpdateLayout();

            return bar;
        }

        public override void UpdateBarMaximum(string status, int maximum)
        {
            progressBars[status].SetMaximum(maximum);
        }

        public override void IncreaseBarValue(string status)
        {
            ProgressBarWithLabel bar = progressBars[status];

            bar.ProgressBarValue++;
        }

        public void RemoveAllBars()
        {
            foreach (var bar in progressBars.Values)
            {
                parentPanel.Children.Remove(bar);
            }

            progressBars.Clear();
        }

        private void RemoveBar(ProgressBarWithLabel bar, string status)
        {
            progressBars.Remove(status);

            parentPanel.Children.Remove(bar);
        }
    }
}
