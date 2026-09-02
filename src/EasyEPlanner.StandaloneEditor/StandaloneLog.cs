using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace EasyEPlanner.StandaloneEditor
{
    /// <summary>
    /// Журнал сообщений standalone-редактора для открытого проекта.
    /// </summary>
    public class StandaloneLog : ILog, IDisposable
    {
        public const string FileName = "standalone_project_errors.log";

        private readonly object syncRoot = new object();
        private readonly List<string> messages = new List<string>();
        private StandaloneLogForm logForm;

        /// <summary>
        /// Создать журнал в каталоге приложения.
        /// </summary>
        public StandaloneLog()
            : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        /// <summary>
        /// Создать журнал для проекта.
        /// </summary>
        /// <param name="projectDirectory">Каталог проекта.</param>
        public StandaloneLog(string projectDirectory)
        {
            LogPath = Path.Combine(projectDirectory, FileName);
            AppendToFile($"{Environment.NewLine}" +
                $"=== Сеанс {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===" +
                Environment.NewLine);
        }

        /// <summary>
        /// Путь к файлу журнала.
        /// </summary>
        public string LogPath { get; }

        /// <summary>
        /// Количество сообщений текущей операции.
        /// </summary>
        public int MessageCount
        {
            get
            {
                lock (syncRoot)
                {
                    return messages.Count;
                }
            }
        }

        /// <summary>
        /// Событие изменения журнала текущей операции.
        /// </summary>
        public event EventHandler Changed;

        public bool IsEmpty() => MessageCount == 0;

        public void ShowLog()
        {
            if (logForm == null || logForm.IsDisposed)
            {
                logForm = new StandaloneLogForm(LogPath);
            }

            UpdateFormText();
            if (!logForm.Visible)
            {
                logForm.Show();
            }

            logForm.BringToFront();
        }

        public void HideLog()
        {
            if (logForm != null && !logForm.IsDisposed)
            {
                logForm.Hide();
            }
        }

        public void Clear()
        {
            lock (syncRoot)
            {
                messages.Clear();
            }

            UpdateFormText();
            OnChanged();
        }

        public bool Active()
        {
            return logForm != null && !logForm.IsDisposed && logForm.Visible;
        }

        public void EnableButtons()
        {
            UpdateForm(form => form.SetCloseButtonEnabled(true));
        }

        public void DisableButtons()
        {
            UpdateForm(form => form.SetCloseButtonEnabled(false));
        }

        public void AddMessage(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
            {
                return;
            }

            string message = msg.TrimEnd();
            lock (syncRoot)
            {
                if (messages.Contains(message))
                {
                    return;
                }

                messages.Add(message);
            }

            string record = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                message + Environment.NewLine;
            AppendToFile(record);
            Debug.WriteLine(message);
            UpdateFormText();
            OnChanged();
        }

        /// <summary>
        /// Добавить исключение с названием выполняемой операции.
        /// </summary>
        public void AddException(string operation, Exception exception)
        {
            AddMessage($"{operation}: {exception}");
        }

        public void ShowLastLine()
        {
            UpdateForm(form => form.ShowLastLine());
        }

        public void SetProgress(int val)
        {
            UpdateForm(form => form.SetProgress(val));
        }

        public bool IsNull() => false;

        public void Dispose()
        {
            if (logForm != null && !logForm.IsDisposed)
            {
                logForm.Dispose();
            }
        }

        private string GetText()
        {
            lock (syncRoot)
            {
                return string.Join(Environment.NewLine, messages);
            }
        }

        private void AppendToFile(string text)
        {
            try
            {
                File.AppendAllText(LogPath, text, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Не удалось записать журнал проекта: " +
                    ex.Message);
            }
        }

        private void UpdateFormText()
        {
            string text = GetText();
            UpdateForm(form => form.SetText(text));
        }

        private void UpdateForm(Action<StandaloneLogForm> action)
        {
            if (logForm == null || logForm.IsDisposed)
            {
                return;
            }

            if (logForm.InvokeRequired)
            {
                logForm.BeginInvoke(action, logForm);
                return;
            }

            action(logForm);
        }

        private void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Окно журнала standalone-редактора.
    /// </summary>
    internal class StandaloneLogForm : Form
    {
        private readonly RichTextBox messagesTextBox;
        private readonly ProgressBar progressBar;
        private readonly Button closeButton;

        public StandaloneLogForm(string logPath)
        {
            Text = "Журнал проекта";
            Width = 850;
            Height = 500;
            StartPosition = FormStartPosition.CenterParent;

            messagesTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font(FontFamily.GenericMonospace, 9),
                WordWrap = false,
            };

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
            };

            closeButton = new Button
            {
                Text = "Закрыть",
                AutoSize = true,
                Anchor = AnchorStyles.Right,
            };
            closeButton.Click += (sender, args) => Hide();

            var pathLabel = new Label
            {
                Text = logPath,
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
            };

            var bottomPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(6),
            };
            bottomPanel.ColumnStyles.Add(new ColumnStyle(
                SizeType.Percent, 100));
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottomPanel.Controls.Add(progressBar, 0, 0);
            bottomPanel.Controls.Add(closeButton, 1, 0);
            bottomPanel.Controls.Add(pathLabel, 0, 1);
            bottomPanel.SetColumnSpan(pathLabel, 2);

            Controls.Add(messagesTextBox);
            Controls.Add(bottomPanel);

            FormClosing += (sender, args) =>
            {
                if (args.CloseReason == CloseReason.UserClosing)
                {
                    args.Cancel = true;
                    Hide();
                }
            };
        }

        public void SetText(string text)
        {
            messagesTextBox.Text = text;
        }

        public void SetProgress(int value)
        {
            progressBar.Value = Math.Max(progressBar.Minimum,
                Math.Min(progressBar.Maximum, value));
        }

        public void SetCloseButtonEnabled(bool enabled)
        {
            closeButton.Enabled = enabled;
        }

        public void ShowLastLine()
        {
            messagesTextBox.SelectionStart = messagesTextBox.TextLength;
            messagesTextBox.ScrollToCaret();
        }
    }
}
