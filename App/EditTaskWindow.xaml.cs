using System.ComponentModel;
using System.Windows;

namespace CSEInverter
{
    /// <summary>
    /// Interaction logic for EditTaskWindow.xaml
    /// </summary>
    public partial class EditTaskWindow : Window
    {
        public IProductTask Task { get; set; }

        public EditTaskWindow(IProductTask task)
        {
            Task = task;

            InitializeComponent();
            TaskPropertyGrid.SelectedObject = Task;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            TaskPropertyGrid.Update();
        }
    }
}
