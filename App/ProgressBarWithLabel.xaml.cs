using System.Windows.Controls;

namespace CSEInverter
{
    /// <summary>
    /// Interaction logic for ProgressBarWithLabel.xaml
    /// </summary>
    public partial class ProgressBarWithLabel : UserControl
    {
        public double ProgressBarValue { get { return progressBar.Value; } set { progressBar.Value = value; } }

        public ProgressBarWithLabel(string textForLabel)
        {
            InitializeComponent();

            label.Text = textForLabel;
            progressBar.Maximum = 1;
            progressBar.Value = 0;
        }

        public void SetMaximum(int maximumProgressBarValue)
        {
            progressBar.Maximum = maximumProgressBarValue;
        }
    }
}
