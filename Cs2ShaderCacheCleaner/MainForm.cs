using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cs2ShaderCacheCleaner
{
    internal sealed class MainForm : Form
    {
        private readonly TextBox steamPathTextBox;
        private readonly TextBox cs2PathTextBox;
        private readonly ListView targetListView;
        private readonly TextBox logTextBox;
        private readonly Button scanButton;
        private readonly Button cleanButton;
        private List<CacheTarget> currentTargets = new List<CacheTarget>();

        public MainForm()
        {
            Text = "CS2 着色器缓存清理工具";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 560);
            Size = new Size(980, 640);
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            var headerLabel = new Label
            {
                AutoSize = true,
                Text = "选择缓存位置，扫描后勾选要清理的项目",
                Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
                Location = new Point(16, 16)
            };

            var pathPanel = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = 2,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(16, 52),
                Size = new Size(932, 70)
            };
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            pathPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            pathPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

            steamPathTextBox = CreatePathTextBox();
            cs2PathTextBox = CreatePathTextBox();

            pathPanel.Controls.Add(CreatePathLabel("Steam 目录"), 0, 0);
            pathPanel.Controls.Add(steamPathTextBox, 1, 0);
            pathPanel.Controls.Add(CreateBrowseButton(steamPathTextBox, true), 2, 0);
            pathPanel.Controls.Add(CreatePathLabel("CS2 目录"), 0, 1);
            pathPanel.Controls.Add(cs2PathTextBox, 1, 1);
            pathPanel.Controls.Add(CreateBrowseButton(cs2PathTextBox, false), 2, 1);

            targetListView = new ListView
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(16, 132),
                Size = new Size(932, 300),
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                View = View.Details
            };
            targetListView.Columns.Add("清理项", 190);
            targetListView.Columns.Add("状态", 130);
            targetListView.Columns.Add("数量", 80);
            targetListView.Columns.Add("大小", 100);
            targetListView.Columns.Add("路径/匹配规则", 410);

            scanButton = new Button
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(728, 446),
                Size = new Size(100, 34),
                Text = "扫描"
            };
            scanButton.Click += async (sender, args) => await ScanAsync();

            cleanButton = new Button
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(848, 446),
                Size = new Size(100, 34),
                Text = "清理所选"
            };
            cleanButton.Click += async (sender, args) => await CleanSelectedAsync();

            logTextBox = new TextBox
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(16, 494),
                Size = new Size(932, 96),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            Controls.Add(headerLabel);
            Controls.Add(pathPanel);
            Controls.Add(targetListView);
            Controls.Add(scanButton);
            Controls.Add(cleanButton);
            Controls.Add(logTextBox);

            Load += async (sender, args) => await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            SetBusy(true);
            try
            {
                var steamPath = await Task.Run(() => SteamLibraryLocator.FindSteamPath());
                var cs2Path = await Task.Run(() => SteamLibraryLocator.FindCs2Path(steamPath));
                steamPathTextBox.Text = steamPath;
                cs2PathTextBox.Text = cs2Path;
                AppendLog("已自动定位 Steam/CS2 路径，可手动调整后重新扫描。");
                await ScanAsync();
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task ScanAsync()
        {
            SetBusy(true);
            try
            {
                AppendLog("开始扫描缓存目标...");
                currentTargets = await Task.Run(() => CacheCleaner.BuildTargets(steamPathTextBox.Text, cs2PathTextBox.Text));
                RenderTargets();
                AppendLog("扫描完成。");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task CleanSelectedAsync()
        {
            var selectedTargets = targetListView.CheckedItems
                .Cast<ListViewItem>()
                .Select(item => item.Tag as CacheTarget)
                .Where(target => target != null && target.ItemCount > 0)
                .ToList();

            if (selectedTargets.Count == 0)
            {
                MessageBox.Show(this, "请先勾选至少一个有内容的清理项。", "没有可清理项目", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var message = "即将清理以下项目：" + Environment.NewLine + Environment.NewLine
                          + string.Join(Environment.NewLine, selectedTargets.Select(target => " - " + target.Name + "：" + target.Path))
                          + Environment.NewLine + Environment.NewLine
                          + "建议先关闭 CS2、Steam 和可能占用缓存的程序。是否继续？";

            if (MessageBox.Show(this, message, "确认清理", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            SetBusy(true);
            try
            {
                AppendLog("开始清理所选缓存...");
                var results = await Task.Run(() => selectedTargets.Select(CacheCleaner.Clean).ToList());
                foreach (var result in results)
                {
                    AppendLog(string.Format("{0}：{1}（删除 {2} 项）{3}",
                        result.Name,
                        result.Success ? "成功" : "失败",
                        result.DeletedCount,
                        string.IsNullOrWhiteSpace(result.Message) ? "" : "，" + result.Message));
                }

                if (results.Any(result => result.Success && result.RequiresSteamValidation))
                {
                    string validationMessage;
                    SteamGameVerifier.TryRequestCs2Validation(out validationMessage);
                    AppendLog(validationMessage);
                }

                await ScanAsync();
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void RenderTargets()
        {
            targetListView.BeginUpdate();
            targetListView.Items.Clear();

            foreach (var target in currentTargets)
            {
                var item = new ListViewItem(target.Name)
                {
                    Checked = target.ItemCount > 0,
                    Tag = target
                };
                item.SubItems.Add(target.Status ?? "");
                item.SubItems.Add(target.ItemCount.ToString());
                item.SubItems.Add(FormatBytes(target.SizeBytes));
                item.SubItems.Add(FormatTargetPath(target));
                targetListView.Items.Add(item);
            }

            targetListView.EndUpdate();
        }

        private static string FormatTargetPath(CacheTarget target)
        {
            if (string.IsNullOrWhiteSpace(target.Path))
            {
                return "";
            }

            return string.IsNullOrWhiteSpace(target.Pattern)
                ? target.Path
                : target.Path + "\\" + target.Pattern;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0)
            {
                return "0 B";
            }

            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            var unit = 0;
            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }

            return string.Format("{0:0.##} {1}", value, units[unit]);
        }

        private TextBox CreatePathTextBox()
        {
            return new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 4, 8, 0)
            };
        }

        private static Label CreatePathLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Button CreateBrowseButton(TextBox targetTextBox, bool steamDirectory)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                Text = "浏览..."
            };

            button.Click += (sender, args) =>
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = steamDirectory ? "选择 Steam 安装目录" : "选择 CS2 安装目录";
                    dialog.SelectedPath = targetTextBox.Text;
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        targetTextBox.Text = dialog.SelectedPath;
                    }
                }
            };

            return button;
        }

        private void SetBusy(bool busy)
        {
            scanButton.Enabled = !busy;
            cleanButton.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private void AppendLog(string message)
        {
            logTextBox.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine);
        }
    }
}
