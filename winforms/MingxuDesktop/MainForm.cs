using System.Collections;
using System.Collections.Generic;
using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mingxu.Core.Models;
using Mingxu.Core.Pipeline;
using Mingxu.Core.Liunian;
using Mingxu.Core.Rules;
using Mingxu.Core.Scoring;
using Mingxu.Core.Mail;
using Mingxu.Core.Site;
using Mingxu.Data;
using Mingxu.Export;
using Mingxu.Export.Flow;

namespace MingxuDesktop
{

public sealed class MainForm : Form
{
    private readonly CharacterRepository _repo;
    private AnalysisRequest _lastRequest;
    private AnalysisResult _lastResult;

    private readonly ComboBox _mode = new ComboBox();
    private readonly ComboBox _gender = new ComboBox();
    private readonly TextBox _surname = new TextBox();
    private readonly TextBox _currentName = new TextBox();
    private readonly DateTimePicker _birthDate = new DateTimePicker();
    private readonly DateTimePicker _birthTime = new DateTimePicker();
    private readonly ComboBox _place = new ComboBox();
    private readonly CheckBox _trueSolar = new CheckBox();
    private readonly TextBox _father = new TextBox();
    private readonly TextBox _mother = new TextBox();
    private readonly ComboBox _namingMode = new ComboBox();
    private readonly ComboBox _exploration = new ComboBox();
    private readonly TextBox _preferred = new TextBox();
    private readonly TextBox _forbidden = new TextBox();
    private readonly TextBox _zibei = new TextBox();
    private readonly ComboBox _zibeiPosition = new ComboBox();
    private readonly CheckBox _excludeHot5y = new CheckBox();
    private readonly CheckBox _excludeClassicHot = new CheckBox();
    private readonly ComboBox _renameReason = new ComboBox();
    private readonly CheckedListBox _improveDirs = new CheckedListBox();
    private readonly CheckedListBox _renameFocus = new CheckedListBox();
    private readonly CheckBox _avoidParentChars = new CheckBox();
    private readonly CheckBox _allowSingle = new CheckBox();
    private readonly CheckBox _allowDouble = new CheckBox();
    private readonly NumericUpDown _resultCount = new NumericUpDown();
    private readonly NumericUpDown _liunianStartYear = new NumericUpDown();
    private readonly NumericUpDown _liunianEndYear = new NumericUpDown();
    private readonly CheckBox _includeMonths = new CheckBox();
    private readonly ComboBox _monthDetail = new ComboBox();
    private readonly NumericUpDown _candidateSeed = new NumericUpDown();
    private readonly CheckBox _useChatGpt = new CheckBox();
    private readonly TextBox _aestheticBrief = new TextBox();
    private readonly Button _btnAnalyze = new Button();
    private readonly Button _btnExport = new Button();
    private readonly Label _status = new Label();
    private readonly DataGridView _grid = new DataGridView();
    private readonly TextBox _preview = new TextBox();
    private SplitContainer _mainSplit;
    private SplitContainer _rightSplit;
    private Panel _formPanel;
    private readonly List<FormRow> _formRows = new List<FormRow>();
    private Label _lblCurrentName;
    private Label _lblLiunianStart;
    private Label _lblLiunianEnd;
    private Label _hintSeed;
    private string _chatGptApiKey = "";
    private string _chatGptModel = "gpt-4o-mini";
    private string _chatGptBaseUrl = "https://api.openai.com/v1";
    private YearLuck _selectedYear;

    private static readonly string[] Places =
    {
        "台北市","新北市","基隆市","桃園市","新竹市","新竹縣","苗栗縣","台中市","彰化縣","南投縣",
        "雲林縣","嘉義市","嘉義縣","台南市","高雄市","屏東縣","宜蘭縣","花蓮縣","台東縣","澎湖縣","金門縣","連江縣（馬祖）"
    };

    private enum FieldGroup
    {
        Always,
        Birth,
        Surname,     // newborn + rename + liunian（複姓可手填／自動辨識）
        Naming,      // newborn + rename
        Parents,     // newborn + rename
        Rename,
        NeedName,    // rename + liunian
        Liunian,
        Actions
    }

    private sealed class FormRow
    {
        public FieldGroup Group;
        public int Height;
        public Control[] Controls;
    }

    public MainForm()
    {
        try { _repo = new CharacterRepository(); }
        catch (Exception ex)
        {
            MessageBox.Show("字庫載入失敗：\n" + ex.Message, "名序", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
        }

        Text = "名序｜命名剖象  " + AppVersion.Display;
        Width = 1280;
        Height = 820;
        MinimumSize = new Size(1100, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft JhengHei UI", 10F);
        BackColor = Color.FromArgb(247, 244, 239);
        TryApplyAppIcon();
        LoadChatGptSettings();
        BuildUi();
        Shown += MainFormShown;
        SizeChanged += MainFormSizeChanged;
    }

    private void LoadChatGptSettings()
    {
        try
        {
            _chatGptApiKey = ConfigurationManager.AppSettings["OpenAI:ApiKey"] ?? "";
            _chatGptModel = ConfigurationManager.AppSettings["OpenAI:Model"] ?? "gpt-4o-mini";
            _chatGptBaseUrl = ConfigurationManager.AppSettings["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
            var use = (ConfigurationManager.AppSettings["Naming:UseChatGpt"] ?? "true").Trim().ToLowerInvariant();
            _useChatGpt.Checked = use != "false" && use != "0";
        }
        catch
        {
            _useChatGpt.Checked = true;
        }
        if (string.IsNullOrWhiteSpace(_chatGptModel)) _chatGptModel = "gpt-4o-mini";
        if (string.IsNullOrWhiteSpace(_chatGptBaseUrl)) _chatGptBaseUrl = "https://api.openai.com/v1";
    }

    private void TryApplyAppIcon()
    {
        try
        {
            var ico = ResolveAssetPath("app.ico");
            if (ico != null)
                Icon = new Icon(ico);
        }
        catch
        {
            // keep default icon
        }
    }

    private static void TryLoadLogo(PictureBox box)
    {
        try
        {
            var path = ResolveAssetPath("logo-mark.png")
                ?? ResolveAssetPath("logo-ui.png")
                ?? ResolveAssetPath("logo.png");
            if (path == null) return;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                box.Image = Image.FromStream(fs);
        }
        catch
        {
            // header still works without image
        }
    }

    private static string ResolveAssetPath(string fileName)
    {
        var bases = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets"),
            Path.Combine(Application.StartupPath, "Assets"),
        };
        foreach (var dir in bases)
        {
            var path = Path.GetFullPath(Path.Combine(dir, fileName));
            if (File.Exists(path)) return path;
        }
        return null;
    }

    private void ApplySplitSizes()
    {
        if (_mainSplit != null)
        {
            EnsureMinSizes(_mainSplit, 280, 400);
            var target = Math.Min(400, Math.Max(340, _mainSplit.Width / 3));
            SafeSetSplitterDistance(_mainSplit, target);
        }
        if (_rightSplit != null)
        {
            EnsureMinSizes(_rightSplit, 180, 120);
            var target = (int)(_rightSplit.Height * 0.62);
            SafeSetSplitterDistance(_rightSplit, target);
        }
    }

    /// <summary>
    /// 必須等 SplitContainer 實際尺寸夠大再設 MinSize，
    /// 否則在建構時（預設約 150×100）會直接 InvalidOperationException。
    /// </summary>
    private static void EnsureMinSizes(SplitContainer split, int panel1Min, int panel2Min)
    {
        if (!split.IsHandleCreated) return;
        var total = split.Orientation == Orientation.Vertical ? split.Width : split.Height;
        var need = panel1Min + panel2Min + split.SplitterWidth;
        if (total < need) return;
        try
        {
            if (split.Panel1MinSize != panel1Min) split.Panel1MinSize = panel1Min;
            if (split.Panel2MinSize != panel2Min) split.Panel2MinSize = panel2Min;
        }
        catch (InvalidOperationException)
        {
            // ignore layout race
        }
    }

    /// <summary>避免啟動／縮放時 SplitterDistance 超出允許範圍而崩潰。</summary>
    private static void SafeSetSplitterDistance(SplitContainer split, int desired)
    {
        if (!split.IsHandleCreated) return;
        var total = split.Orientation == Orientation.Vertical ? split.Width : split.Height;
        if (total <= split.SplitterWidth + 2) return;
        var min = Math.Max(0, split.Panel1MinSize);
        var max = total - Math.Max(0, split.Panel2MinSize) - split.SplitterWidth;
        if (max < min) return;
        var value = Clamp(desired, min, max);
        try
        {
            if (split.SplitterDistance != value)
                split.SplitterDistance = value;
        }
        catch (InvalidOperationException)
        {
            // layout race
        }
        catch (ArgumentOutOfRangeException)
        {
            // layout race
        }
    }

    private void BuildUi()
    {
        SuspendLayout();
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.FromArgb(247, 244, 239),
            Padding = new Padding(10, 8, 12, 8),
        };
        var logoBox = new PictureBox
        {
            Width = 52,
            Height = 52,
            Left = 10,
            Top = 8,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
        };
        TryLoadLogo(logoBox);
        var title = new Label
        {
            Text = "名序 － 以命為本．以字成名",
            AutoSize = false,
            Left = 72,
            Top = 8,
            Width = 700,
            Height = 52,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Microsoft JhengHei UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 42, 46),
        };
        header.Controls.Add(logoBox);
        header.Controls.Add(title);

        // 注意：不要在這裡設大的 Panel1MinSize/Panel2MinSize（控制項尚未有真實尺寸會崩潰）
        _mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            BorderStyle = BorderStyle.FixedSingle,
        };

        // Fill first, then Top — so Fill gets remaining space correctly
        Controls.Add(_mainSplit);
        Controls.Add(header);

        _formPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10) };
        _mainSplit.Panel1.Controls.Add(_formPanel);
        BuildFormRows();
        ApplyModeUi(false);

        _rightSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            BorderStyle = BorderStyle.FixedSingle,
        };
        _mainSplit.Panel2.Controls.Add(_rightSplit);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _grid.RowHeadersVisible = false;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.None;
        _grid.ColumnHeadersHeight = 32;
        _grid.RowTemplate.Height = 28;
        _grid.SelectionChanged += GridSelectionChanged;
        _rightSplit.Panel1.Controls.Add(_grid);

        var previewHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
        _preview.Dock = DockStyle.Fill;
        _preview.Multiline = true;
        _preview.ScrollBars = ScrollBars.Vertical;
        _preview.ReadOnly = true;
        _preview.BackColor = Color.FromArgb(255, 253, 248);
        _preview.Font = new Font("Microsoft JhengHei UI", 10F);
        previewHost.Controls.Add(_preview);
        _rightSplit.Panel2.Controls.Add(previewHost);
        ResumeLayout(true);
    }

    private void BuildFormRows()
    {
        _formRows.Clear();
        _formPanel.Controls.Clear();

        _mode.DropDownStyle = ComboBoxStyle.DropDownList;
        _mode.Width = 220;
        _mode.Items.Clear();
        _mode.Items.AddRange(new object[] { "新生兒命名", "專業改名", "流年分析" });
        _mode.SelectedIndex = 0;
        AddRow(FieldGroup.Always, 32, Lbl("服務"), _mode);

        _gender.DropDownStyle = ComboBoxStyle.DropDownList;
        _gender.Width = 85;
        _gender.Items.Clear();
        _gender.Items.AddRange(new object[] { "男", "女" });
        _gender.SelectedIndex = 0;
        AddRow(FieldGroup.Always, 32, Lbl("性別"), _gender);

        _surname.Width = 100;
        AddRow(FieldGroup.Surname, 32, Lbl("姓氏"), _surname);

        _birthDate.Width = 180;
        _birthDate.Format = DateTimePickerFormat.Custom;
        _birthDate.ShowCheckBox = true;
        _birthDate.Checked = false;
        _birthDate.Value = DateTime.Today;
        SyncBirthDateFormat();
        EventHandler syncBirthDate = (s, e) => SyncBirthDateFormat();
        _birthDate.ValueChanged += syncBirthDate;
        _birthDate.DropDown += syncBirthDate;
        _birthDate.CloseUp += syncBirthDate;
        _birthDate.MouseUp += (s, e) => SyncBirthDateFormat();
        AddRow(FieldGroup.Birth, 32, Lbl("出生日期"), _birthDate);

        _birthTime.Width = 140;
        _birthTime.Format = DateTimePickerFormat.Custom;
        _birthTime.ShowUpDown = true;
        _birthTime.ShowCheckBox = true;
        _birthTime.Checked = false;
        _birthTime.Value = DateTime.Today;
        SyncBirthTimeFormat();
        EventHandler syncBirthTime = (s, e) => SyncBirthTimeFormat();
        _birthTime.ValueChanged += syncBirthTime;
        _birthTime.MouseUp += (s, e) => SyncBirthTimeFormat();
        AddRow(FieldGroup.Birth, 32, Lbl("出生時間"), _birthTime);

        _place.DropDownStyle = ComboBoxStyle.DropDownList;
        _place.Width = 160;
        _place.Items.Clear();
        _place.Items.AddRange(Places);
        _place.SelectedIndex = -1; // 預設不填
        _trueSolar.Width = 100;
        _trueSolar.Text = "真太陽時";
        _trueSolar.Checked = true;
        AddRow(FieldGroup.Birth, 32, Lbl("出生地"), _place, _trueSolar);

        _lblCurrentName = Lbl("原姓名");
        _currentName.Width = 250;
        AddRow(FieldGroup.NeedName, 32, _lblCurrentName, _currentName);
        _currentName.TextChanged += (s, e) => SyncSurnameFromFullName();

        _father.Width = 100;
        _mother.Width = 100;
        AddRow(FieldGroup.Parents, 32, Lbl("父親"), _father, SideLbl("母親", 40), _mother);

        _namingMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _namingMode.Width = 180;
        _namingMode.Items.Clear();
        _namingMode.Items.AddRange(new object[] { "綜合", "傳統", "現代", "文學", "小眾" });
        _namingMode.SelectedIndex = 0;
        AddRow(FieldGroup.Naming, 32, Lbl("命名模式"), _namingMode);

        _useChatGpt.Width = 250;
        _useChatGpt.Text = "用 ChatGPT 產生名（審美優先）";
        AddRow(FieldGroup.Naming, 28, Lbl("組名引擎"), _useChatGpt);

        _aestheticBrief.Width = 250;
        _aestheticBrief.Height = 64;
        _aestheticBrief.Multiline = true;
        _aestheticBrief.ScrollBars = ScrollBars.Vertical;
        _aestheticBrief.Text = "清雅有骨、現代書卷氣，好念好寫，避免俗氣與過度華麗。";
        AddRow(FieldGroup.Naming, 70, Lbl("審美描述"), _aestheticBrief);

        _preferred.Width = 100;
        _forbidden.Width = 90;
        AddRow(FieldGroup.Naming, 32, Lbl("偏好字"), _preferred, SideLbl("禁用字／詞", 80), _forbidden);

        _zibei.Width = 55;
        _zibei.MaxLength = 1;
        _zibeiPosition.DropDownStyle = ComboBoxStyle.DropDownList;
        _zibeiPosition.Width = 185;
        _zibeiPosition.Items.Clear();
        _zibeiPosition.Items.AddRange(new object[] { "名首（字輩在前）", "名末（字輩在後）" });
        _zibeiPosition.SelectedIndex = 0;
        AddRow(FieldGroup.Naming, 32, Lbl("字輩"), _zibei, _zibeiPosition);

        _allowSingle.Width = 100;
        _allowSingle.Text = "允許單名";
        _allowSingle.Checked = true;
        _allowDouble.Width = 100;
        _allowDouble.Text = "允許雙名";
        _allowDouble.Checked = true;
        AddRow(FieldGroup.Naming, 32, Lbl("名長"), _allowSingle, _allowDouble);

        _excludeHot5y.Width = 140;
        _excludeHot5y.Text = "加強避近5年熱門";
        _excludeClassicHot.Width = 140;
        _excludeClassicHot.Text = "加強避經典熱門";
        AddRow(FieldGroup.Naming, 32, Lbl("熱門名"), _excludeHot5y, _excludeClassicHot);

        _renameReason.DropDownStyle = ComboBoxStyle.DropDownList;
        _renameReason.Width = 250;
        _renameReason.Items.Clear();
        _renameReason.Items.Add("");
        _renameReason.Items.AddRange(AdultRename.RenameReasons.Cast<object>().ToArray());
        _renameReason.SelectedIndex = 0;
        AddRow(FieldGroup.Rename, 32, Lbl("改名原因"), _renameReason);

        _improveDirs.Width = 250;
        _improveDirs.Height = 76;
        _improveDirs.CheckOnClick = true;
        _improveDirs.Items.Clear();
        _improveDirs.Items.AddRange(AdultRename.ImproveOptions.Cast<object>().ToArray());
        AddRow(FieldGroup.Rename, 82, Lbl("改善方向"), _improveDirs);

        _renameFocus.Width = 250;
        _renameFocus.Height = 52;
        _renameFocus.CheckOnClick = true;
        _renameFocus.Items.Clear();
        _renameFocus.Items.AddRange(new object[] { "事業", "財運", "感情", "健康" });
        AddRow(FieldGroup.Rename, 58, Lbl("改名關注"), _renameFocus);

        _avoidParentChars.Width = 200;
        _avoidParentChars.Text = "避用父母名字用字";
        _avoidParentChars.Checked = true;
        AddRow(FieldGroup.Parents, 32, Lbl("家長"), _avoidParentChars);

        _exploration.DropDownStyle = ComboBoxStyle.DropDownList;
        _exploration.Width = 180;
        _exploration.Items.Clear();
        _exploration.Items.AddRange(new object[] { "自動", "保守", "平衡", "探索", "創意" });
        _exploration.SelectedIndex = 0;
        AddRow(FieldGroup.Naming, 32, Lbl("探索程度"), _exploration);

        _resultCount.Width = 70;
        _resultCount.Minimum = 5;
        _resultCount.Maximum = 80;
        _resultCount.Value = 30;
        AddRow(FieldGroup.Naming, 32, Lbl("候選數"), _resultCount);

        _lblLiunianStart = Lbl("起始年份");
        _liunianStartYear.Width = 90;
        _liunianStartYear.Minimum = 1950;
        _liunianStartYear.Maximum = 2100;
        _liunianStartYear.Value = DateTime.Now.Year;
        AddRow(FieldGroup.Liunian, 32, _lblLiunianStart, _liunianStartYear);

        _lblLiunianEnd = Lbl("結束年份");
        _liunianEndYear.Width = 90;
        _liunianEndYear.Minimum = 1950;
        _liunianEndYear.Maximum = 2100;
        _liunianEndYear.Value = DateTime.Now.Year + 9;
        AddRow(FieldGroup.Liunian, 32, _lblLiunianEnd, _liunianEndYear);

        _includeMonths.Width = 200;
        _includeMonths.Text = "包含流月分析";
        _includeMonths.Checked = true;
        AddRow(FieldGroup.Liunian, 32, Lbl("流月"), _includeMonths);

        _monthDetail.DropDownStyle = ComboBoxStyle.DropDownList;
        _monthDetail.Width = 160;
        _monthDetail.Items.Clear();
        _monthDetail.Items.AddRange(new object[] { "標準", "詳細" });
        _monthDetail.SelectedIndex = 1;
        AddRow(FieldGroup.Liunian, 32, Lbl("流月詳細程度"), _monthDetail);

        _candidateSeed.Width = 100;
        _candidateSeed.Minimum = 0;
        _candidateSeed.Maximum = int.MaxValue;
        _candidateSeed.Value = 0;
        _hintSeed = SideLbl("0＝隨機", 80);
        AddRow(FieldGroup.Naming, 36, Lbl("亂數種子"), _candidateSeed, _hintSeed);

        _btnAnalyze.Text = "開始分析";
        _btnAnalyze.Width = 110;
        _btnAnalyze.Height = 34;
        _btnAnalyze.Click += AnalyzeClicked;
        _btnExport.Text = "匯出 PDF";
        _btnExport.Width = 110;
        _btnExport.Height = 34;
        _btnExport.Enabled = false;
        _btnExport.Click += ExportClicked;
        AddRow(FieldGroup.Actions, 42, Lbl(""), _btnAnalyze, _btnExport);

        _status.Width = 390;
        _status.Height = 72;
        _status.Text = "請選擇服務並填寫資料後開始分析。";
        AddRow(FieldGroup.Actions, 76, _status);

        _mode.SelectedIndexChanged += ModeChanged;
    }

    private void AddRow(FieldGroup group, int height, params Control[] controls)
    {
        foreach (var c in controls)
        {
            if (c == null) continue;
            if (!_formPanel.Controls.Contains(c))
                _formPanel.Controls.Add(c);
        }
        _formRows.Add(new FormRow
        {
            Group = group,
            Height = height,
            Controls = controls.Where(c => c != null).ToArray(),
        });
    }

    private static Label Lbl(string text)
    {
        return new Label
        {
            Text = text,
            Width = 110,
            Height = 24,
            TextAlign = ContentAlignment.MiddleRight,
        };
    }

    private static Label SideLbl(string text, int width)
    {
        return new Label
        {
            Text = text,
            Width = width,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
        };
    }

    private bool RowVisible(FieldGroup group, string mode)
    {
        switch (group)
        {
            case FieldGroup.Always:
            case FieldGroup.Birth:
            case FieldGroup.Actions:
                return true;
            case FieldGroup.Surname:
                return mode == "newborn" || mode == "rename" || mode == "liunian";
            case FieldGroup.Naming:
            case FieldGroup.Parents:
                return mode == "newborn" || mode == "rename";
            case FieldGroup.Rename:
                return mode == "rename";
            case FieldGroup.NeedName:
                return mode == "rename" || mode == "liunian";
            case FieldGroup.Liunian:
                return mode == "liunian";
            default:
                return true;
        }
    }

    private void ApplyModeUi(bool fromUser)
    {
        var mode = CurrentMode();
        if (mode == "rename" && fromUser)
        {
            _excludeHot5y.Checked = true;
            _excludeClassicHot.Checked = true;
        }

        _lblCurrentName.Text = mode == "liunian" ? "分析姓名" : "原姓名";
        _btnAnalyze.Text = mode == "liunian" ? "分析流年" : mode == "rename" ? "開始改名分析" : "開始命名分析";
        _btnExport.Visible = true;
        if (_lastResult == null)
            _status.Text = ModeReadyText(mode);

        if (mode == "rename" || mode == "liunian")
            SyncSurnameFromFullName();

        int y = 8;
        const int leftCol = 8;
        const int fieldCol = 126;

        foreach (var row in _formRows)
        {
            var show = RowVisible(row.Group, mode);
            if (!show)
            {
                foreach (var c in row.Controls)
                    c.Visible = false;
                continue;
            }

            int x = fieldCol;
            bool firstLabel = true;
            foreach (var c in row.Controls)
            {
                c.Visible = true;
                if (firstLabel && c is Label && c.Width >= 100)
                {
                    c.Left = leftCol;
                    c.Top = y + 3;
                    firstLabel = false;
                }
                else
                {
                    c.Left = x;
                    if (c is CheckedListBox)
                        c.Top = y;
                    else if (c is Button)
                        c.Top = y;
                    else
                        c.Top = y + 2;
                    x += c.Width + 8;
                }
            }
            y += row.Height;
        }
    }

    private static string ModeReadyText(string mode)
    {
        if (mode == "rename") return "就緒｜專業改名：請填姓氏（複姓可填兩字）、原姓名與出生資料。";
        if (mode == "liunian") return "就緒｜流年分析：請填姓氏（複姓可填兩字）、分析姓名與出生資料。";
        return "就緒｜新生兒命名：請填姓氏與出生資料。";
    }

    /// <summary>
    /// 從全名自動辨識姓氏（含複姓表）；若使用者已手填且仍為全名前綴則保留手填。
    /// </summary>
    private void SyncSurnameFromFullName()
    {
        var mode = CurrentMode();
        if (mode != "rename" && mode != "liunian") return;
        if (_repo == null) return;

        var full = (_currentName.Text ?? "").Trim().Replace(" ", "").Replace("　", "");
        if (full.Length < 2) return;

        var typed = (_surname.Text ?? "").Trim().Replace(" ", "").Replace("　", "");
        // 手填複姓且仍為全名前綴時保留（支援表外複姓）
        if (typed.Length >= 2 && full.StartsWith(typed, StringComparison.Ordinal) && full.Length > typed.Length)
            return;

        var parsed = NameParse.SplitFullName(full, _repo.CompoundSurnames());
        if (!string.IsNullOrEmpty(parsed.Surname) && _surname.Text != parsed.Surname)
            _surname.Text = parsed.Surname;
    }

    private string CurrentMode()
    {
        switch (_mode.SelectedIndex)
        {
            case 1: return "rename";
            case 2: return "liunian";
            default: return "newborn";
        }
    }

    private string CurrentGender()
    {
        return _gender.SelectedIndex == 1 ? "F" : "M";
    }

    private string CurrentNamingMode()
    {
        switch (_namingMode.SelectedIndex)
        {
            case 1: return "traditional";
            case 2: return "modern";
            case 3: return "literary";
            case 4: return "niche";
            default: return "balanced";
        }
    }

    private string CurrentExploration()
    {
        switch (_exploration.SelectedIndex)
        {
            case 1: return "conservative";
            case 2: return "balanced";
            case 3: return "exploratory";
            case 4: return "creative";
            default: return "";
        }
    }

    private void SyncBirthDateFormat()
    {
        _birthDate.CustomFormat = _birthDate.Checked ? "yyyy-MM-dd" : " ";
    }

    private void SyncBirthTimeFormat()
    {
        _birthTime.CustomFormat = _birthTime.Checked ? "HH:mm" : " ";
    }

    private bool TryReadBirth(out DateTime birth, out string birthPlace, out string error)
    {
        birth = default(DateTime);
        birthPlace = "";
        error = null;
        if (!_birthDate.Checked)
        {
            error = "請選擇出生日期。";
            return false;
        }
        if (!_birthTime.Checked)
        {
            error = "請選擇出生時間。";
            return false;
        }
        if (_place.SelectedItem == null || string.IsNullOrWhiteSpace(_place.SelectedItem.ToString()))
        {
            error = "請選擇出生地。";
            return false;
        }
        birth = _birthDate.Value.Date
            .AddHours(_birthTime.Value.Hour)
            .AddMinutes(_birthTime.Value.Minute);
        birthPlace = _place.SelectedItem.ToString();
        return true;
    }

    private AnalysisRequest BuildRequest()
    {
        var mode = CurrentMode();
        DateTime birth;
        string birthPlace;
        string birthError;
        if (!TryReadBirth(out birth, out birthPlace, out birthError))
            throw new InvalidOperationException(birthError);

        return new AnalysisRequest
        {
            Mode = mode,
            Gender = CurrentGender(),
            Birth = birth,
            Surname = _surname.Text.Trim(),
            CurrentFullName = _currentName.Text.Trim(),
            BirthPlace = birthPlace,
            UseTrueSolar = _trueSolar.Checked,
            FatherName = mode == "liunian" ? "" : _father.Text.Trim(),
            MotherName = mode == "liunian" ? "" : _mother.Text.Trim(),
            NamingMode = CurrentNamingMode(),
            ExplorationLevel = CurrentExploration(),
            PreferredChars = mode == "liunian" ? "" : _preferred.Text.Trim(),
            ForbiddenChars = mode == "liunian" ? "" : _forbidden.Text.Trim(),
            Zibei = mode == "liunian" ? "" : _zibei.Text.Trim(),
            ZibeiPosition = _zibeiPosition.SelectedIndex <= 0 ? 0 : 1,
            RenameReason = mode == "rename" && _renameReason.SelectedItem != null ? _renameReason.SelectedItem.ToString() : "",
            ImproveDirections = mode == "rename"
                ? string.Join(",", _improveDirs.CheckedItems.Cast<object>().Select(x => x.ToString()))
                : "",
            RenameFocus = mode == "rename"
                ? string.Join(",", _renameFocus.CheckedItems.Cast<object>().Select(x => x.ToString()))
                : "",
            ExcludeHot5y = mode == "rename" || _excludeHot5y.Checked,
            ExcludeClassicHot = mode == "rename" || _excludeClassicHot.Checked,
            AvoidParentChars = mode != "liunian" && _avoidParentChars.Checked,
            AllowSingle = _allowSingle.Checked,
            AllowDouble = _allowDouble.Checked,
            ResultCount = (int)_resultCount.Value,
            LiunianYears = Math.Max(1, (int)_liunianEndYear.Value - (int)_liunianStartYear.Value + 1),
            LiunianStartYear = (int)_liunianStartYear.Value,
            LiunianEndYear = (int)_liunianEndYear.Value,
            IncludeLiunianMonths = mode == "liunian" && _includeMonths.Checked,
            LiunianMonthDetail = mode == "liunian" && _monthDetail.SelectedIndex == 0 ? "standard" : "detailed",
            CandidateSeed = (int)_candidateSeed.Value,
            UseChatGptGivens = mode != "liunian" && _useChatGpt.Checked,
            ChatGptApiKey = _chatGptApiKey,
            ChatGptModel = _chatGptModel,
            ChatGptBaseUrl = _chatGptBaseUrl,
            AestheticBrief = _aestheticBrief.Text.Trim(),
        };
    }

    private void BindGrid(AnalysisResult result)
    {
        _grid.DataSource = null;
        _grid.Columns.Clear();
        _grid.AutoGenerateColumns = false;
        var mode = CurrentMode();
        if (mode == "liunian")
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Year", HeaderText = "年度", Width = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Ganzhi", HeaderText = "干支", Width = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Total", HeaderText = "整體", Width = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Career", HeaderText = "事業", Width = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Wealth", HeaderText = "財運", Width = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Relation", HeaderText = "感情", Width = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Keyword", HeaderText = "定位", Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Level", HeaderText = "綜合", Width = 60 });
            _grid.DataSource = (result.Liunian ?? new List<YearLuck>()).Select(y => new LiunianGridRow
            {
                Year = y.Year,
                Ganzhi = y.Ganzhi,
                Total = y.TotalScore,
                Career = y.CareerScore,
                Wealth = y.WealthScore,
                Relation = y.RelationshipScore,
                Keyword = y.Keyword,
                Level = y.Level,
            }).ToList();
            return;
        }

        var isRename = mode == "rename";
        if (isRename)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Rank", HeaderText = "名次", Width = 50 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "姓名", Width = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Total", HeaderText = "綜合", Width = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Improve", HeaderText = "改善", Width = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Wuge", HeaderText = "五格", Width = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Sancai", HeaderText = "三才", Width = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Bazi", HeaderText = "命理", Width = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Wuxing", HeaderText = "五行", Width = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Phonology", HeaderText = "音韻", Width = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Meaning", HeaderText = "字義", Width = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Style", HeaderText = "風格", Width = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Rarity", HeaderText = "辨識度", Width = 60 });
            _grid.DataSource = result.Suggestions.Select((s, i) =>
            {
                var cmp = s.RenameComparison;
                return new RenameGridRow
                {
                    Rank = i + 1,
                    Name = s.FullName,
                    Total = s.Total,
                    Improve = cmp == null ? "" : AdultRename.FormatDelta(cmp.TotalDelta),
                    Wuge = s.WugeScore,
                    Sancai = s.Wuge == null ? "" : s.Wuge.SancaiLuck,
                    Bazi = s.BaziScore,
                    Wuxing = s.WuxingScore,
                    Phonology = s.PhonologyScore,
                    Meaning = s.MeaningScore,
                    Style = s.StyleScore,
                    Rarity = s.RarityScore,
                };
            }).ToList();
        }
        else
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Rank", HeaderText = "名次", Width = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "姓名", Width = 120 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Total", HeaderText = "綜合", Width = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Grade", HeaderText = "評等", Width = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Bazi", HeaderText = "八字", Width = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Wuge", HeaderText = "五格", Width = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Meaning", HeaderText = "字義", Width = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Wx", HeaderText = "名中五行", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 120 });
            _grid.DataSource = result.Suggestions.Select((s, i) => new GridRow
            {
                Rank = i + 1,
                Name = s.FullName,
                Total = s.Total,
                Grade = s.Grade,
                Bazi = s.BaziScore,
                Wuge = s.WugeScore,
                Meaning = s.MeaningScore,
                Wx = string.Join("、", s.CharWuxing),
            }).ToList();
        }
    }

    private sealed class LiunianGridRow
    {
        public int Year { get; set; }
        public string Ganzhi { get; set; } = "";
        public double Total { get; set; }
        public double Career { get; set; }
        public double Wealth { get; set; }
        public double Relation { get; set; }
        public string Keyword { get; set; } = "";
        public string Level { get; set; } = "";
    }

    private sealed class GridRow
    {
        public int Rank { get; set; }
        public string Name { get; set; } = "";
        public double Total { get; set; }
        public string Grade { get; set; } = "";
        public double Bazi { get; set; }
        public double Wuge { get; set; }
        public double Meaning { get; set; }
        public string Wx { get; set; } = "";
    }

    private sealed class RenameGridRow
    {
        public int Rank { get; set; }
        public string Name { get; set; } = "";
        public double Total { get; set; }
        public string Improve { get; set; } = "";
        public double Wuge { get; set; }
        public string Sancai { get; set; } = "";
        public double Bazi { get; set; }
        public double Wuxing { get; set; }
        public double Phonology { get; set; }
        public double Meaning { get; set; }
        public double Style { get; set; }
        public double Rarity { get; set; }
    }

    private async Task RunAnalyzeAsync()
    {
        _btnAnalyze.Enabled = false;
        _btnExport.Enabled = false;
        _status.Text = "分析中…";
        if (CurrentMode() == "liunian")
            _status.Text = "分析中（ChatGPT 產流年解說，請稍候）…";
        else if (_useChatGpt.Checked)
            _status.Text = "分析中（ChatGPT 組名中，請稍候）…";
        try
        {
            if (CurrentMode() == "liunian" && string.IsNullOrWhiteSpace(_chatGptApiKey))
            {
                MessageBox.Show(this,
                    "流年分析建議設定 OpenAI API Key。\n\n未設定時仍可用本地流年，但文案較制式。\n\n請在 App.config 填入 OpenAI:ApiKey。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (_useChatGpt.Checked && CurrentMode() != "liunian" && string.IsNullOrWhiteSpace(_chatGptApiKey))
            {
                MessageBox.Show(this,
                    "尚未設定 OpenAI API Key。\n\n請在 App.config 填入：\n<add key=\"OpenAI:ApiKey\" value=\"你的金鑰\" />\n\n檔案位置：\n" +
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MingxuDesktop.exe.config"),
                    "需要 API Key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var req = BuildRequest();
            // 流年模式也帶上 API 設定
            req.ChatGptApiKey = _chatGptApiKey;
            req.ChatGptModel = _chatGptModel;
            req.ChatGptBaseUrl = _chatGptBaseUrl;
            var result = await Task.Run(() => NamingService.Run(req, _repo));
            _lastRequest = req;
            _lastResult = result;
            _selectedYear = null;
            BindGrid(result);
            _btnExport.Enabled = result.Suggestions.Count > 0 ||
                (result.Liunian != null && result.Liunian.Count > 0);
            object notes;
            result.Destiny.TryGetValue("pipeline_notes", out notes);
            var list = notes as IEnumerable;
            var extra = list != null
                ? string.Join("｜", list.Cast<object>().Take(3))
                : "";
            var countText = CurrentMode() == "liunian"
                ? (result.Liunian == null ? 0 : result.Liunian.Count) + " 年"
                : result.Suggestions.Count + " 筆";
            _status.Text = $"完成：{countText}\n{extra}";
            if (_grid.Rows.Count > 0)
            {
                _grid.ClearSelection();
                _grid.Rows[0].Selected = true;
                _grid.CurrentCell = _grid.Rows[0].Cells[0];
            }
            ApplySplitSizes();
            UpdatePreview();
        }
        catch (Exception ex)
        {
            _status.Text = "失敗";
            MessageBox.Show(this, ex.Message, "分析失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _btnAnalyze.Enabled = true; }
    }

    private NameSuggestion Selected()
    {
        if (_lastResult == null) return null;
        if (CurrentMode() == "liunian")
            return _lastResult.Suggestions != null && _lastResult.Suggestions.Count > 0
                ? _lastResult.Suggestions[0] : _lastResult.Current;
        if (_grid.CurrentRow == null) return null;
        var idx = _grid.CurrentRow.Index;
        return idx >= 0 && idx < _lastResult.Suggestions.Count ? _lastResult.Suggestions[idx] : null;
    }

    private YearLuck SelectedYear()
    {
        if (_lastResult == null || _lastResult.Liunian == null || _grid.CurrentRow == null) return null;
        var idx = _grid.CurrentRow.Index;
        if (idx < 0 || idx >= _lastResult.Liunian.Count) return null;
        return _lastResult.Liunian[idx];
    }

    private void UpdatePreview()
    {
        if (_lastResult == null) { _preview.Text = ""; return; }

        if (CurrentMode() == "liunian")
        {
            var blocks = new List<string>();
            var name = _lastResult.Current != null ? _lastResult.Current.FullName : "";
            object rangeObj;
            var range = _lastResult.Destiny.TryGetValue("liunian_range", out rangeObj) && rangeObj != null
                ? rangeObj.ToString() : "";
            blocks.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            blocks.Add("流年分析　" + name);
            blocks.Add("分析期間：" + range);
            blocks.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            object tableObj;
            if (_lastResult.Destiny.TryGetValue("liunian_year_table", out tableObj) && tableObj != null)
            {
                blocks.Add("【年度總覽表】");
                blocks.Add(tableObj.ToString().Replace("\n", "\r\n"));
                blocks.Add("");
            }
            AppendDestinyBlock(blocks, _lastResult.Destiny, "liunian_overview", "宏觀總覽");
            AppendDestinyBlock(blocks, _lastResult.Destiny, "liunian_phases", "宏觀階段");
            AppendDestinyBlock(blocks, _lastResult.Destiny, "liunian_trend", "趨勢與風險預警");
            AppendDestinyBlock(blocks, _lastResult.Destiny, "liunian_actions", "行動建議（Do's / Don'ts）");

            var year = SelectedYear();
            _selectedYear = year;
            if (year != null)
            {
                blocks.Add("");
                blocks.Add(LiunianEngine.FormatYearDetail(year, year.Months != null && year.Months.Count > 0)
                    .Replace("\n", "\r\n"));
            }
            else
            {
                blocks.Add("請點選上方年度列，查看該年總覽與流月。");
            }
            _preview.Text = string.Join("\r\n", blocks);
            return;
        }

        var sug = Selected();
        if (sug == null) { _preview.Text = ""; return; }

        if (CurrentMode() == "rename")
        {
            var blocks = new System.Collections.Generic.List<string>();
            blocks.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            blocks.Add("專業改名分析");
            blocks.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            if (_lastResult.Current != null)
                blocks.Add("原名：" + _lastResult.Current.FullName);
            object dirObj;
            if (_lastResult.Destiny.TryGetValue("rename_directions", out dirObj) && dirObj != null)
                blocks.Add(dirObj.ToString().Replace("\n", "\r\n"));
            blocks.Add("");
            object evalObj;
            if (_lastResult.Destiny.TryGetValue("current_eval", out evalObj) && evalObj != null)
                blocks.Add(evalObj.ToString().Replace("\n", "\r\n"));
            blocks.Add("");
            blocks.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            blocks.Add("② 新名字候選（點選上方列表切換）");
            blocks.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            blocks.Add("選擇：" + sug.FullName + "　綜合 " + sug.Total.ToString("0") + "（" + sug.Grade + "）");
            if (sug.RenameComparison != null)
                blocks.Add("相對原名：" + AdultRename.FormatDelta(sug.RenameComparison.TotalDelta));
            blocks.Add("");
            if (!string.IsNullOrWhiteSpace(sug.ComparisonText))
                blocks.Add(sug.ComparisonText.Replace("\n", "\r\n"));
            else if (!string.IsNullOrWhiteSpace(sug.ReasonText))
                blocks.Add(sug.ReasonText.Replace("\n", "\r\n"));
            _preview.Text = string.Join("\r\n", blocks);
            return;
        }

        var comparison = string.IsNullOrWhiteSpace(sug.ComparisonText)
            ? "" : "\r\n\r\n" + sug.ComparisonText.Replace("\n", "\r\n");
        object currentEval;
        var currentText = _lastResult.Destiny.TryGetValue("current_eval", out currentEval) && currentEval != null
            ? "\r\n原名評估：" + currentEval.ToString().Replace("\n", "\r\n") + "\r\n" : "";
        object packObj;
        var story = "";
        if (_lastResult.Destiny.TryGetValue("content_pack", out packObj))
        {
            var pack = packObj as System.Collections.Generic.Dictionary<string, string>;
            string storyLine;
            if (pack != null && pack.TryGetValue("story", out storyLine) && !string.IsNullOrWhiteSpace(storyLine))
                story = "\r\n【命名故事】\r\n" + storyLine + "\r\n";
        }
        object catsObj;
        var catsText = "";
        if (_lastResult.Destiny.TryGetValue("category_lists", out catsObj))
        {
            var cats = catsObj as System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>;
            if (cats != null)
            {
                var lines = new System.Collections.Generic.List<string>();
                foreach (var kv in cats)
                    lines.Add(kv.Key + "：" + string.Join("、", kv.Value.Take(5)));
                if (lines.Count > 0)
                    catsText = "\r\n【分類推薦】\r\n" + string.Join("\r\n", lines) + "\r\n";
            }
        }
        var liunianText = "";
        if (_lastResult.Liunian != null && _lastResult.Liunian.Count > 0)
        {
            var lines = new System.Collections.Generic.List<string>();
            AppendDestinyBlock(lines, _lastResult.Destiny, "liunian_overview", "流年總覽");
            AppendDestinyBlock(lines, _lastResult.Destiny, "liunian_phases", "宏觀階段");
            AppendDestinyBlock(lines, _lastResult.Destiny, "liunian_trend", "趨勢與風險預警");
            AppendDestinyBlock(lines, _lastResult.Destiny, "liunian_actions", "行動建議（Do's / Don'ts）");
            lines.Add("逐年說明：");
            foreach (var yr in _lastResult.Liunian.Take(Math.Min(20, _lastResult.Liunian.Count)))
                lines.Add(string.Format("{0}（{1}歲・{2}）〔{3}〕{4}", yr.Year, yr.Age, yr.Ganzhi, yr.Level, yr.Summary));
            liunianText = "\r\n【流年】\r\n" + string.Join("\r\n", lines) + "\r\n";
        }
        _preview.Text = $"【{sug.FullName}】{sug.Total:0}（{sug.Grade}）\r\n" +
            $"姓名美感：{sug.AestheticScore:0.0}\r\n" +
            (sug.ParentUsed ? $"父母合參：{sug.ParentScore:0.0}\r\n" : "") +
            currentText + story + catsText + liunianText +
            $"\r\n{_lastResult.HumanReadableDestiny}\r\n\r\n{sug.ReasonText.Replace("\n", "\r\n")}{comparison}";
    }

    private static void AppendDestinyBlock(
        System.Collections.Generic.List<string> lines,
        System.Collections.Generic.Dictionary<string, object> destiny,
        string key,
        string title)
    {
        if (destiny == null) return;
        object value;
        if (!destiny.TryGetValue(key, out value) || value == null) return;
        var text = value.ToString();
        if (string.IsNullOrWhiteSpace(text)) return;
        lines.Add("〔" + title + "〕");
        lines.Add(text.Replace("\n", "\r\n"));
        lines.Add("");
    }

    private async void ExportClicked(object sender, EventArgs e) { await ExportPdfAsync(); }

    private async Task ExportPdfAsync()
    {
        if (_lastRequest == null || _lastResult == null) { MessageBox.Show("請先分析"); return; }
        var sug = Selected();
        if (sug == null) { MessageBox.Show("請選擇姓名"); return; }
        var serviceName = CurrentMode() == "rename" ? "專業改名"
            : CurrentMode() == "liunian" ? "流年分析"
            : "新生兒命名";
        using (var dlg = new SaveFileDialog
        {
            Filter = "PDF|*.pdf",
            FileName = $"名序_{serviceName}_{sug.FullName}_命名剖象.pdf",
        })
        {
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            _btnExport.Enabled = false;
            _btnAnalyze.Enabled = false;
            var exportMode = _lastRequest.Mode ?? "";
            if (exportMode == "newborn" || exportMode == "rename" || exportMode == "liunian")
                _status.Text = "匯出中（HTML 報告 → Chromium PDF）…";
            else
                _status.Text = "匯出中…";
            try
            {
                // 確保匯出時帶上目前的 API 設定（分析後若改過 config 仍可用）
                _lastRequest.ChatGptApiKey = _chatGptApiKey;
                _lastRequest.ChatGptModel = _chatGptModel;
                _lastRequest.ChatGptBaseUrl = _chatGptBaseUrl;
                _lastRequest.AestheticBrief = _aestheticBrief.Text.Trim();

                var path = dlg.FileName;
                var req = _lastRequest;
                var result = _lastResult;
                var engine = (ConfigurationManager.AppSettings["Pdf:Engine"] ?? "WebView2").Trim();
                var browserPath = (ConfigurationManager.AppSettings["Pdf:BrowserPath"] ?? "").Trim();
                var useFlowPdf = (exportMode == "liunian" || exportMode == "newborn" || exportMode == "rename")
                    && !string.Equals(engine, "PdfSharp", StringComparison.OrdinalIgnoreCase);

                if (useFlowPdf)
                {
                    _status.Text = exportMode == "newborn" ? "產生新生兒 HTML 報告…"
                        : exportMode == "rename" ? "產生改名 HTML 報告…"
                        : "產生流年 HTML 報告…";
                    var html = await Task.Run(() => FlowReportExporter.BuildHtml(req, result, sug));
                    _status.Text = "Chromium 轉 PDF…";
                    await WebView2PdfRenderer.PrintHtmlToPdfAsync(html, path, browserPath);
                }
                else
                {
                    await Task.Run(() => PdfReportExporter.Export(req, result, sug, path));
                }

                string casePath = null;
                CasePublishResult publish = null;
                try
                {
                    var card = CaseCardBuilder.Build(req, result, sug);
                    casePath = Path.ChangeExtension(path, ".case.json");
                    await Task.Run(() =>
                    {
                        var text = card.ToString(Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(casePath, text, new System.Text.UTF8Encoding(false));
                    });

                    var publishOn = string.Equals(
                        (ConfigurationManager.AppSettings["Site:PublishCases"] ?? "true").Trim(),
                        "true",
                        StringComparison.OrdinalIgnoreCase);
                    if (publishOn)
                    {
                        _status.Text = "更新官網真實案例並 push…";
                        var repoOverride = (ConfigurationManager.AppSettings["Site:RepoRoot"] ?? "").Trim();
                        publish = await Task.Run(() => CaseSitePublisher.Publish(
                            card,
                            commitAndPush: true,
                            repoRootOverride: string.IsNullOrEmpty(repoOverride) ? null : repoOverride));
                    }
                }
                catch (Exception caseEx)
                {
                    casePath = null;
                    publish = new CasePublishResult
                    {
                        Ok = false,
                        Error = caseEx.Message,
                        Message = "案例發佈失敗",
                    };
                }

                MailSendResult mail = null;
                var mailOn = string.Equals(
                    (ConfigurationManager.AppSettings["Mail:Enabled"] ?? "true").Trim(),
                    "true",
                    StringComparison.OrdinalIgnoreCase);
                if (mailOn)
                {
                    _status.Text = "寄送 PDF 至信箱…";
                    var smtpPort = 587;
                    int.TryParse(ConfigurationManager.AppSettings["Mail:SmtpPort"] ?? "587", out smtpPort);
                    var enableSsl = !string.Equals(
                        (ConfigurationManager.AppSettings["Mail:EnableSsl"] ?? "true").Trim(),
                        "false",
                        StringComparison.OrdinalIgnoreCase);
                    mail = await Task.Run(() => PdfMailSender.SendPdf(
                        path,
                        sug.FullName,
                        serviceName,
                        ConfigurationManager.AppSettings["Mail:SmtpHost"] ?? "smtp.gmail.com",
                        smtpPort,
                        ConfigurationManager.AppSettings["Mail:User"] ?? "",
                        ConfigurationManager.AppSettings["Mail:AppPassword"] ?? "",
                        ConfigurationManager.AppSettings["Mail:To"] ?? "",
                        ConfigurationManager.AppSettings["Mail:From"] ?? "",
                        enableSsl));
                }

                _status.Text = "已匯出：" + path;
                var msg = "已匯出：\n" + path;
                if (!string.IsNullOrEmpty(casePath))
                    msg += "\n\n案例檔：\n" + casePath;
                if (publish != null)
                {
                    if (publish.Ok)
                        msg += "\n\n官網：" + (publish.Message ?? "已更新");
                    else
                        msg += "\n\n官網更新未完成：\n"
                            + (publish.Message ?? "")
                            + (string.IsNullOrEmpty(publish.Error) ? "" : "\n" + publish.Error);
                }
                if (mail != null)
                {
                    if (mail.Ok)
                        msg += "\n\n寄信：" + (mail.Message ?? "已寄出");
                    else if (mail.Skipped)
                        msg += "\n\n寄信：" + (mail.Message ?? "已略過");
                    else
                        msg += "\n\n寄信失敗：\n"
                            + (mail.Message ?? "")
                            + (string.IsNullOrEmpty(mail.Error) ? "" : "\n" + mail.Error);
                }
                MessageBox.Show(this, msg, "完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, (ex.InnerException != null ? ex.InnerException.Message : ex.Message), "匯出失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _status.Text = "匯出失敗";
            }
            finally
            {
                _btnAnalyze.Enabled = true;
                _btnExport.Enabled = _lastResult != null && _lastResult.Suggestions.Count > 0;
            }
        }
    }

    private void ExportPdf()
    {
        // kept for compatibility; route to async
        var ignored = ExportPdfAsync();
    }

    private void MainFormShown(object sender, EventArgs e) { ApplySplitSizes(); }
    private void MainFormSizeChanged(object sender, EventArgs e)
    {
        if (IsHandleCreated && Visible) ApplySplitSizes();
    }
    private async void AnalyzeClicked(object sender, EventArgs e) { await RunAnalyzeAsync(); }
    private void GridSelectionChanged(object sender, EventArgs e) { UpdatePreview(); }
    private void ModeChanged(object sender, EventArgs e)
    {
        ApplyModeUi(true);
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Max(min, Math.Min(max, value));
    }
}
}
