using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace CSEInverter
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class AddTaskWindow : Window
    {
        public IEnumerable<TaskDetails> AvailableTask { get; set; }

        Config Config;

        public AddTaskWindow(Config config)
        {
            this.Config = config;
            AvailableTask = GetAllAvailableTask();

            InitializeComponent();

            IsVisibleChanged += VisibilityChanged;
        }

        private void VisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                AvailableTask = GetAllAvailableTask();
                TaskListBox.ItemsSource = AvailableTask;
            }
        }

        private IEnumerable<TaskDetails> GetAllAvailableTask()
        {
            IEnumerable<Type> unavailableTask = Config.GetTasks().Select(task => task.GetType());

            IEnumerable<Type> availableTask = MainWindow.AllTask.Except(unavailableTask);

            var taskDescriptions = availableTask.Select(task => new TaskDetails() { Type = task, Description = GetTaskDescription(task) });

            return taskDescriptions;
        }

        private string GetTaskDescription(Type task)
        {
            var instance = Activator.CreateInstance(task);

            string description = (string)typeof(IProductTask).InvokeMember(
                nameof(IProductTask.GetDescription),
                BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance,
                null,
                instance,
                null
                );

            return description;
        }

        private void Add_Task_Event(object sender, RoutedEventArgs e)
        {
            AddSelectedTask();
        }

        private void AddSelectedTask()
        {
            if (TaskListBox.SelectedItem == null) return;

            TaskDetails task = (TaskDetails)TaskListBox.SelectedItem;
            InstaniateTaskAndAddToConfig(task);

            CloseWindow();
        }

        private void InstaniateTaskAndAddToConfig(TaskDetails taskDetails)
        {
            IProductTask task = InstaniateTask(taskDetails.Type);

            Config.AddTask(task);
        }

        private IProductTask InstaniateTask(Type task)
        {
            ConstructorInfo constructor = task.GetConstructor(Type.EmptyTypes);
            IProductTask instaniatedTask = (IProductTask)constructor.Invoke(Type.EmptyTypes);

            return instaniatedTask;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (MainWindow.IsClosing == false)
            {
                e.Cancel = true;
                CloseWindow();
            }
        }

        private void CloseWindow()
        {
            Visibility = Visibility.Hidden;
        }

        public struct TaskDetails
        {
            public string Description { get; set; }
            public Type Type { get; set; }
        }
    }
}
