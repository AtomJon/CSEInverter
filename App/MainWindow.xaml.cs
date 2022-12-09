using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CSEInverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool IsClosing = false;
        public static Type[] AllTask => new[] { typeof(DownloadFTPTask), typeof(ExtractTask), typeof(SaveToFileTask) };

        static readonly FileInfo ConfigFile = new FileInfo("Config.xml");

        public Config Config { get; set; }

        private ProgressBarManager barManager;
        private readonly BackgroundWorker backgroundWorker = new BackgroundWorker();

        private AddTaskWindow AddTaskWindow;

        public MainWindow()
        {
            LoadConfig();

            AddTaskWindow = new AddTaskWindow(Config);

            InitializeComponent();

            barManager = new ProgressBarManager(TaskBarPanel);

            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;

            Config.TaskAdded += TasksListBox.Items.Refresh;
        }

        private void LoadConfig()
        {
            if (ConfigFile.Exists)
            {
                using (Stream configStream = ConfigFile.OpenRead())
                {
                    Config = Config.LoadConfigFromStream(configStream);
                }
            }
            else
            {
                Config = new Config("Main");
            }
        }

        private void SaveConfig()
        {
            using Stream stream = ConfigFile.Create();

            Config.SaveToStream(stream);

            stream.Flush();
        }

        private void Abort_Button_Click(object sender, RoutedEventArgs e)
        {
            AbortTask();
        }

        private void AbortTask()
        {
            backgroundWorker.CancelAsync();
        }

        private void Run_Button_Click(object sender, RoutedEventArgs e)
        {
            RunAllTask();
        }

        private void RunAllTask()
        {
            HideWorkDoneText();

            if (!backgroundWorker.IsBusy)
                backgroundWorker.RunWorkerAsync(true);
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (Config == null) return;
            if (Config.ProductTasks.Count < 1) return;

            bool runAllTask = (bool)e.Argument;

            IProductTask task = Config.ProductTasks[0].Task;

            PerformTasks(runAllTask, task);
        }

        private void PerformTasks(bool runAllTask, IProductTask task)
        {
            TaskArguments args = new();

            args.ProgressUpdate = (args) =>
            {
                if (backgroundWorker.CancellationPending)
                {
                    if (args.Type == TaskProgressUpdateType.TaskStartet || args.Type == TaskProgressUpdateType.TaskFinished)
                    {
                        Logger.WriteLine("Aborting tasks", false);
                        throw new OperationCanceledException("Cancellation was requested trough backgroundWorker");
                    }
                }
                else
                {
                    backgroundWorker.ReportProgress(0, args);
                }
            };

            if (Config.ProductTasks.Count > 1 && runAllTask)
            {
                LinkedList<IProductTask> taskList = new(Config.ProductTasks.Select(taskConfiguration => taskConfiguration.Task));
                LinkedListNode<IProductTask> node = taskList.First;

                args.Node = node;
            }

            try
            {
                Logger.WriteLine("Starting operation", true);
                Stopwatch stopwatch = new();
                stopwatch.Start();
                task.Initiate(args);
                stopwatch.Stop();
                Logger.WriteLine($"Operation complete, time Elapsed: {stopwatch.Elapsed}", true);

                GC.Collect();

                Dispatcher.InvokeAsync(DisplayWorkDoneText);
            }
            catch (OperationCanceledException)
            {
                Dispatcher.InvokeAsync(barManager.RemoveAllBars);
            }
            catch (Exception ex)
            {
                Logger.WriteLine(ex, true);

                Dispatcher.InvokeAsync(() =>
                {
                    barManager.RemoveAllBars();

                    MessageBox.Show("Der skete en fejl under operationen");
                });
            }
        }

        private void HideWorkDoneText()
        {
            WorkDoneText.Visibility = Visibility.Collapsed;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var args = (TaskProgressUpdateArgs)e.UserState;

            switch (args.Type)
            {
                case TaskProgressUpdateType.AddWork:
                    barManager.UpdateBarMaximum(args.WorkStatus, args.AmountOfWork);
                    break;
                case TaskProgressUpdateType.WorkDone:
                    barManager.IncreaseBarValue(args.WorkStatus);
                    break;
                case TaskProgressUpdateType.TaskStartet:
                    barManager.TaskStartet(args.WorkStatus);
                    break;
                case TaskProgressUpdateType.TaskFinished:
                    barManager.TaskFinished(args.WorkStatus);
                    break;
            }
        }

        private void DisplayWorkDoneText()
        {
            if (WorkDoneText.Visibility != Visibility.Visible) WorkDoneText.Visibility = Visibility.Visible;
        }

        private void Remove_Task_Button_Click(object sender, RoutedEventArgs e)
        {
            ProductTaskConfiguration task = GetSelectedTask();

            if (task != null)
            {
                Config.RemoveTask(task.Task);
                SaveConfig();

                TasksListBox.Items.Refresh();
            }
        }

        private void Add_Task_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenAddTaskDialog();
        }

        private void OpenAddTaskDialog()
        {
            AddTaskWindow.Show();
        }

        protected override void OnClosed(EventArgs e)
        {
            IsClosing = true;

            AddTaskWindow.Close();
        }

        private void TasksListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenEditTaskWindow();
        }

        private void OpenEditTaskWindow()
        {
            ProductTaskConfiguration taskConfiguration = GetSelectedTask();

            var window = new EditTaskWindow(taskConfiguration.Task);
            window.ShowDialog();

            SaveConfig();
        }

        private ProductTaskConfiguration GetSelectedTask()
        {
            return (ProductTaskConfiguration)TasksListBox.SelectedItem;
        }

        void s_PreviewMouseMoveEvent(object sender, MouseEventArgs e)
        {
            if (sender is ListBoxItem && e.LeftButton == MouseButtonState.Pressed)
            {
                ListBoxItem draggedItem = sender as ListBoxItem;
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        private void ListBoxItem_Drop(object sender, DragEventArgs e)
        {
            ProductTaskConfiguration droppedData = (ProductTaskConfiguration)e.Data.GetData(typeof(ProductTaskConfiguration));
            ProductTaskConfiguration target = (ProductTaskConfiguration)((ListBoxItem)sender).DataContext;

            Config.RearrangeTasks(droppedData, target);
            SaveConfig();

            TasksListBox.Items.Refresh();
        }
    }
}
