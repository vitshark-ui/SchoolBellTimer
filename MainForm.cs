using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace SchoolBellTimer
{
    /// <summary>
    /// Специализированная панель с включенной двойной буферизацией для плавной отрисовки без мерцания.
    /// </summary>
    public class DoubleBufferedPanel : Panel
    {
        /// <summary>
        /// Конструктор панели: включает стили плавной перерисовки.
        /// </summary>
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            this.UpdateStyles();
        }
    }

    /// <summary>
    /// Состояние отдельной строки урока в боковом списке расписания.
    /// </summary>
    public enum LessonRowState
    {
        /// <summary>
        /// Предстоящий урок (еще не начался).
        /// </summary>
        Upcoming = 0,

        /// <summary>
        /// Текущий активный урок (идет в данный момент).
        /// </summary>
        Active = 1,

        /// <summary>
        /// Следующий урок (во время текущей перемены).
        /// </summary>
        NextBreak = 2,

        /// <summary>
        /// Завершенный урок (время окончания уже прошло).
        /// </summary>
        Past = 3
    }

    /// <summary>
    /// Виджет отдельной карточки урока в боковом списке расписания с аппаратной двойной буферизацией.
    /// </summary>
    public class LessonRowControl : DoubleBufferedPanel
    {
        private LessonItem lesson;
        private LessonRowState state;
        private ThemePalette palette;

        /// <summary>
        /// Данные урока, привязанные к данной строке.
        /// </summary>
        public LessonItem Lesson
        {
            get { return lesson; }
        }

        /// <summary>
        /// Текущее визуальное состояние строки (активен, завершен, перемена, предстоит).
        /// </summary>
        public LessonRowState State
        {
            get { return state; }
            set
            {
                if (state != value)
                {
                    state = value;
                    this.Invalidate();
                }
            }
        }

        /// <summary>
        /// Цветовая палитра оформления строки.
        /// </summary>
        public ThemePalette Palette
        {
            get { return palette; }
            set
            {
                palette = value;
                this.Invalidate();
            }
        }

        /// <summary>
        /// Конструктор строки урока.
        /// </summary>
        /// <param name="item">Модель урока.</param>
        /// <param name="currentPalette">Текущая палитра оформления.</param>
        public LessonRowControl(LessonItem item, ThemePalette currentPalette)
        {
            this.lesson = item;
            this.palette = currentPalette;
            this.state = LessonRowState.Upcoming;
            this.Height = 52;
            this.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Отрисовка стилизованной строки урока в зависимости от темы и текущего статуса.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = this.Width;
            int h = this.Height;

            // Выбор цветов в зависимости от состояния
            Color bgColor;
            Color borderColor;
            Color textColor;
            Color timeColor;
            Color numberBg;
            Color numberText;

            if (state == LessonRowState.Active)
            {
                bgColor = palette.ScheduleActiveItemBackground;
                borderColor = palette.ScheduleActiveItemBorder;
                textColor = palette.ScheduleActiveItemText;
                timeColor = palette.ScheduleActiveItemAccent;
                numberBg = palette.ScheduleActiveItemAccent;
                numberText = Color.FromArgb(15, 23, 42); // Контрастный текст внутри активного бейджа
            }
            else if (state == LessonRowState.NextBreak)
            {
                // Мягкое выделение следующего урока во время перемены
                bgColor = (palette.CardBackground.R < 100)
                    ? Color.FromArgb(22, 38, 52)
                    : Color.FromArgb(240, 246, 255);
                borderColor = palette.ScheduleActiveItemBorder;
                textColor = palette.ScheduleItemText;
                timeColor = palette.ScheduleItemTime;
                numberBg = palette.ScheduleNumberBadgeBackground;
                numberText = palette.ScheduleNumberBadgeText;
            }
            else if (state == LessonRowState.Past)
            {
                // Завершенный урок отображается приглушенным тоном
                bgColor = palette.ScheduleItemBackground;
                borderColor = palette.ScheduleItemBorder;
                textColor = palette.ScheduleItemPastText;
                timeColor = palette.ScheduleItemPastText;
                numberBg = palette.ScheduleNumberBadgeBackground;
                numberText = palette.ScheduleItemPastText;
            }
            else
            {
                // Обычный предстоящий урок
                bgColor = palette.ScheduleItemBackground;
                borderColor = palette.ScheduleItemBorder;
                textColor = palette.ScheduleItemText;
                timeColor = palette.ScheduleItemTime;
                numberBg = palette.ScheduleNumberBadgeBackground;
                numberText = palette.ScheduleNumberBadgeText;
            }

            // Фоновый прямоугольник карточки урока
            Rectangle rowRect = new Rectangle(0, 0, w - 1, h - 1);
            using (SolidBrush bgBrush = new SolidBrush(bgColor))
            {
                g.FillRectangle(bgBrush, rowRect);
            }
            using (Pen borderPen = new Pen(borderColor, (state == LessonRowState.Active) ? 1.5f : 1.0f))
            {
                g.DrawRectangle(borderPen, rowRect);
            }

            // Левая акцентная полоска для текущего урока
            if (state == LessonRowState.Active)
            {
                using (SolidBrush accentBrush = new SolidBrush(palette.ScheduleActiveItemAccent))
                {
                    g.FillRectangle(accentBrush, 0, 0, 4, h);
                }
            }

            // Круглый бейдж с номером урока
            Rectangle badgeRect = new Rectangle(12, 10, 30, 30);
            using (SolidBrush badgeBrush = new SolidBrush(numberBg))
            {
                g.FillEllipse(badgeBrush, badgeRect);
            }
            using (Font numFont = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (SolidBrush numBrush = new SolidBrush(numberText))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString(lesson.Number.ToString(), numFont, numBrush, badgeRect, sf);
            }

            // Название урока
            using (Font titleFont = new Font("Segoe UI", 9.75F, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(textColor))
            {
                g.DrawString(lesson.Name, titleFont, titleBrush, 50, 8);
            }

            // Время урока (ЧЧ:ММ – ЧЧ:ММ)
            string timeStr = string.Format("{0} – {1}", lesson.StartTime, lesson.EndTime);
            using (Font timeFont = new Font("Consolas", 9.0F, FontStyle.Regular))
            using (SolidBrush timeBrush = new SolidBrush(timeColor))
            {
                g.DrawString(timeStr, timeFont, timeBrush, 50, 28);
            }

            // Статусный маркер в правой части строки
            if (state == LessonRowState.Active)
            {
                int badgeW = 68;
                int badgeH = 22;
                Rectangle statusRect = new Rectangle(w - badgeW - 10, (h - badgeH) / 2, badgeW, badgeH);

                using (SolidBrush stBrush = new SolidBrush(palette.ScheduleActiveItemAccent))
                {
                    g.FillRectangle(stBrush, statusRect);
                }
                using (Font stFont = new Font("Segoe UI", 7.5F, FontStyle.Bold))
                using (SolidBrush stTextBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString("● ИДЕТ", stFont, stTextBrush, statusRect, sf);
                }
            }
            else if (state == LessonRowState.NextBreak)
            {
                int badgeW = 86;
                int badgeH = 20;
                Rectangle statusRect = new Rectangle(w - badgeW - 10, (h - badgeH) / 2, badgeW, badgeH);

                using (Pen stPen = new Pen(palette.ScheduleActiveItemBorder, 1f))
                {
                    g.DrawRectangle(stPen, statusRect);
                }
                using (Font stFont = new Font("Segoe UI", 7.2F, FontStyle.Bold))
                using (SolidBrush stTextBrush = new SolidBrush(palette.ScheduleItemTime))
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString("СЛЕДУЮЩИЙ", stFont, stTextBrush, statusRect, sf);
                }
            }
            else if (state == LessonRowState.Past)
            {
                using (Font checkFont = new Font("Segoe UI", 10F, FontStyle.Bold))
                using (SolidBrush checkBrush = new SolidBrush(palette.ScheduleItemPastText))
                {
                    g.DrawString("✓", checkFont, checkBrush, w - 26, (h - 22) / 2);
                }
            }
        }
    }

    /// <summary>
    /// Главное окно приложения SchoolBellTimer.
    /// Содержит центральные электронные часы, таймер обратного отсчета до звонка,
    /// красивую боковую панель расписания уроков, панель управления, переключение тем и полный экран.
    /// </summary>
    public class MainForm : Form
    {
        // -------------------------------------------------------------
        // Элементы пользовательского интерфейса
        // -------------------------------------------------------------
        private MenuStrip menuStripMain;
        private ToolStripMenuItem menuSchedule;
        private ToolStripMenuItem menuScheduleEdit;
        private ToolStripMenuItem menuScheduleListToggle;
        private ToolStripMenuItem menuTheme;
        private ToolStripMenuItem menuThemeDark;
        private ToolStripMenuItem menuThemeLight;
        private ToolStripMenuItem menuBackground;
        private ToolStripMenuItem menuBgColor;
        private ToolStripMenuItem menuBgImage;
        private ToolStripMenuItem menuBgReset;
        private ToolStripMenuItem menuAlwaysOnTop;
        private ToolStripMenuItem menuFullScreen;
        private ToolStripMenuItem menuToggleMenu;

        private ContextMenuStrip contextMenuQuick;
        private ToolStripMenuItem ctxSchedule;
        private ToolStripMenuItem ctxScheduleListToggle;
        private ToolStripMenuItem ctxAlwaysOnTop;
        private ToolStripMenuItem ctxThemeToggle;
        private ToolStripMenuItem ctxFullScreen;
        private ToolStripMenuItem ctxToggleMenu;
        private ToolStripMenuItem ctxExit;

        // Центральная карточка-виджет электронных часов
        private DoubleBufferedPanel panelCard;
        private Label lblDate;
        private Label lblCurrentTime;
        private Label lblStatusBadge;
        private Label lblCountdownTitle;
        private Label lblCountdown;
        private DoubleBufferedPanel panelProgressBar;
        private Label lblProgressPercent;

        // Боковая карточка-виджет списка расписания (справа)
        private DoubleBufferedPanel panelScheduleCard;
        private Panel panelScheduleHeader;
        private Label lblScheduleTitle;
        private Label lblScheduleSubtitle;
        private Button btnScheduleHeaderEdit;
        private DoubleBufferedPanel panelScheduleScroll;
        private List<LessonRowControl> lessonRowControls;

        // Системный таймер обновления времени (интервал 500 мс)
        private Timer timerTick;

        // Настройки приложения и текущее состояние
        private AppSettings settings;
        private ThemePalette currentPalette;
        private Image loadedBackgroundImage = null;

        // Сохранение состояния окна перед переходом в полный экран
        private bool isFullScreenActive = false;
        private FormWindowState previousWindowState = FormWindowState.Normal;
        private FormBorderStyle previousBorderStyle = FormBorderStyle.Sizable;

        // Процент выполнения текущего интервала (для прогресс-бара)
        private int currentProgressPercent = 0;

        /// <summary>
        /// Конструктор главного окна приложения.
        /// </summary>
        public MainForm()
        {
            // Включение двойной буферизации на уровне формы
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            this.UpdateStyles();

            this.lessonRowControls = new List<LessonRowControl>();

            // Загрузка настроек из settings.xml
            this.settings = AppSettings.Load(AppSettings.GetConfigFilePath());
            this.currentPalette = this.settings.GetCurrentPalette();

            // Создание и конфигурация контролов формы
            InitializeComponents();

            // Загрузка фонового изображения
            LoadCustomBackgroundImage();

            // Построение элементов бокового списка уроков
            BuildScheduleListItems();

            // Применение цветовой схемы и фона
            ApplyThemeAndBackground();

            // Первичный расчет расписания
            UpdateClockAndSchedule();

            // Применение режима «Поверх всех окон» при запуске программы (стандартное свойство WinForms)
            this.TopMost = settings.AlwaysOnTop;

            // Запуск таймера
            timerTick.Start();
        }

        /// <summary>
        /// Инициализация графических компонентов формы.
        /// </summary>
        private void InitializeComponents()
        {
            this.Text = "SchoolBellTimer — Электронные офисные часы и расписание уроков";
            this.Size = new Size(1160, 720);
            this.MinimumSize = new Size(720, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;

            // ==========================================
            // Верхнее главное меню (MenuStrip)
            // ==========================================
            menuStripMain = new MenuStrip();
            menuStripMain.Dock = DockStyle.Top;
            menuStripMain.Font = new Font("Segoe UI", 9.5F);

            // Меню: Расписание
            menuSchedule = new ToolStripMenuItem("📋 Расписание");
            menuScheduleEdit = new ToolStripMenuItem("⚙️ Редактировать расписание...", null, new EventHandler(MenuSchedule_Click));
            menuScheduleListToggle = new ToolStripMenuItem("📑 Отображать список уроков на экране (F9)", null, new EventHandler(MenuScheduleListToggle_Click));
            menuScheduleListToggle.CheckOnClick = true;
            menuScheduleListToggle.Checked = settings.ShowScheduleList;
            menuSchedule.DropDownItems.AddRange(new ToolStripItem[] { menuScheduleEdit, menuScheduleListToggle });

            // Меню: Тема оформления
            menuTheme = new ToolStripMenuItem("🎨 Тема часов");
            menuThemeDark = new ToolStripMenuItem("🌙 Офисная темная (Бирюза / Неон)", null, new EventHandler(MenuThemeDark_Click));
            menuThemeLight = new ToolStripMenuItem("☀️ Офисная светлая (ЖК / Синий)", null, new EventHandler(MenuThemeLight_Click));
            menuTheme.DropDownItems.AddRange(new ToolStripItem[] { menuThemeDark, menuThemeLight });

            // Меню: Фон окна
            menuBackground = new ToolStripMenuItem("🖼️ Фон окна");
            menuBgColor = new ToolStripMenuItem("🎨 Выбрать цвет фона...", null, new EventHandler(MenuBgColor_Click));
            menuBgImage = new ToolStripMenuItem("📁 Выбрать фоновое изображение...", null, new EventHandler(MenuBgImage_Click));
            menuBgReset = new ToolStripMenuItem("🔄 Сбросить фон (по умолчанию)", null, new EventHandler(MenuBgReset_Click));
            menuBackground.DropDownItems.AddRange(new ToolStripItem[] { menuBgColor, menuBgImage, new ToolStripSeparator(), menuBgReset });

            // Меню: Поверх всех окон (F10)
            menuAlwaysOnTop = new ToolStripMenuItem("📌 Поверх всех окон (F10)");
            menuAlwaysOnTop.CheckOnClick = true;
            menuAlwaysOnTop.Checked = settings.AlwaysOnTop;
            menuAlwaysOnTop.ToolTipText = "Принудительно удерживать часы поверх всех других окон и всплывающих окон системы.";
            menuAlwaysOnTop.Click += new EventHandler(MenuAlwaysOnTop_Click);

            // Меню: Полный экран (F11)
            menuFullScreen = new ToolStripMenuItem("⛶ Полный экран (F11)");
            menuFullScreen.Click += new EventHandler(MenuFullScreen_Click);

            // Меню: Скрыть верхнюю панель
            menuToggleMenu = new ToolStripMenuItem("👁️ Скрыть меню");
            menuToggleMenu.ToolTipText = "Скрыть панель управления. Для возврата нажмите F11, Esc или правую кнопку мыши.";
            menuToggleMenu.Click += new EventHandler(MenuToggleMenu_Click);

            menuStripMain.Items.AddRange(new ToolStripItem[] {
                menuSchedule,
                menuTheme,
                menuBackground,
                menuAlwaysOnTop,
                menuFullScreen,
                menuToggleMenu
            });

            // ==========================================
            // Контекстное меню по правому клику мыши
            // ==========================================
            contextMenuQuick = new ContextMenuStrip();
            ctxSchedule = new ToolStripMenuItem("📋 Настроить расписание...", null, new EventHandler(MenuSchedule_Click));
            ctxScheduleListToggle = new ToolStripMenuItem("📑 Показать / скрыть список уроков (F9)", null, new EventHandler(MenuScheduleListToggle_Click));
            ctxAlwaysOnTop = new ToolStripMenuItem("📌 Поверх всех окон (F10)", null, new EventHandler(MenuAlwaysOnTop_Click));
            ctxAlwaysOnTop.Checked = settings.AlwaysOnTop;
            ctxThemeToggle = new ToolStripMenuItem("🎨 Переключить тему (Светлая / Темная)", null, new EventHandler(CtxThemeToggle_Click));
            ctxFullScreen = new ToolStripMenuItem("⛶ Во весь экран (F11)", null, new EventHandler(MenuFullScreen_Click));
            ctxToggleMenu = new ToolStripMenuItem("👁️ Показать / скрыть верхнее меню", null, new EventHandler(MenuToggleMenu_Click));
            ctxExit = new ToolStripMenuItem("✖ Выход из программы", null, delegate { Application.Exit(); });

            contextMenuQuick.Items.AddRange(new ToolStripItem[] {
                ctxSchedule,
                ctxScheduleListToggle,
                ctxAlwaysOnTop,
                new ToolStripSeparator(),
                ctxThemeToggle,
                new ToolStripSeparator(),
                ctxFullScreen,
                ctxToggleMenu,
                new ToolStripSeparator(),
                ctxExit
            });
            this.ContextMenuStrip = contextMenuQuick;

            // ==========================================
            // 1. Центральная информационная карточка (Часы)
            // ==========================================
            panelCard = new DoubleBufferedPanel();
            panelCard.Size = new Size(720, 480);
            panelCard.Paint += new PaintEventHandler(PanelCard_Paint);
            panelCard.ContextMenuStrip = contextMenuQuick;

            // Блок: Текущая дата и день недели
            lblDate = new Label();
            lblDate.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            lblDate.TextAlign = ContentAlignment.MiddleCenter;
            lblDate.Size = new Size(680, 26);
            lblDate.Location = new Point(20, 24);

            // Блок: Текущее системное время (крупно)
            lblCurrentTime = new Label();
            lblCurrentTime.Font = new Font("Consolas", 46F, FontStyle.Bold);
            lblCurrentTime.TextAlign = ContentAlignment.MiddleCenter;
            lblCurrentTime.Size = new Size(680, 72);
            lblCurrentTime.Location = new Point(20, 52);
            lblCurrentTime.ContextMenuStrip = contextMenuQuick;

            // Блок: Текущий статус
            lblStatusBadge = new Label();
            lblStatusBadge.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblStatusBadge.TextAlign = ContentAlignment.MiddleCenter;
            lblStatusBadge.Size = new Size(660, 50);
            lblStatusBadge.Location = new Point(30, 138);

            // Блок: Подзаголовок обратного отсчета
            lblCountdownTitle = new Label();
            lblCountdownTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblCountdownTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblCountdownTitle.Text = "ДО ЗВОНКА ОСТАЛОСЬ";
            lblCountdownTitle.Size = new Size(680, 24);
            lblCountdownTitle.Location = new Point(20, 206);

            // Блок: Крупный таймер обратного отсчета (ММ:СС)
            lblCountdown = new Label();
            lblCountdown.Font = new Font("Consolas", 76F, FontStyle.Bold);
            lblCountdown.TextAlign = ContentAlignment.MiddleCenter;
            lblCountdown.Size = new Size(680, 120);
            lblCountdown.Location = new Point(20, 230);
            lblCountdown.ContextMenuStrip = contextMenuQuick;

            // Блок: Визуальный индикатор прогресса
            panelProgressBar = new DoubleBufferedPanel();
            panelProgressBar.Size = new Size(580, 12);
            panelProgressBar.Location = new Point(70, 375);
            panelProgressBar.Paint += new PaintEventHandler(PanelProgressBar_Paint);

            lblProgressPercent = new Label();
            lblProgressPercent.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            lblProgressPercent.TextAlign = ContentAlignment.MiddleCenter;
            lblProgressPercent.Size = new Size(680, 22);
            lblProgressPercent.Location = new Point(20, 395);

            panelCard.Controls.Add(lblDate);
            panelCard.Controls.Add(lblCurrentTime);
            panelCard.Controls.Add(lblStatusBadge);
            panelCard.Controls.Add(lblCountdownTitle);
            panelCard.Controls.Add(lblCountdown);
            panelCard.Controls.Add(panelProgressBar);
            panelCard.Controls.Add(lblProgressPercent);

            // ==========================================
            // 2. Боковая карточка со списком уроков (Справа)
            // ==========================================
            panelScheduleCard = new DoubleBufferedPanel();
            panelScheduleCard.Size = new Size(360, 480);
            panelScheduleCard.Paint += new PaintEventHandler(PanelScheduleCard_Paint);
            panelScheduleCard.ContextMenuStrip = contextMenuQuick;

            // Заголовок панели расписания
            panelScheduleHeader = new Panel();
            panelScheduleHeader.Dock = DockStyle.Top;
            panelScheduleHeader.Height = 60;
            panelScheduleHeader.BackColor = Color.Transparent;
            panelScheduleHeader.Padding = new Padding(14, 10, 14, 8);

            lblScheduleTitle = new Label();
            lblScheduleTitle.Text = "📋 РАСПИСАНИЕ УРОКОВ";
            lblScheduleTitle.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            lblScheduleTitle.AutoSize = true;
            lblScheduleTitle.Location = new Point(12, 10);

            lblScheduleSubtitle = new Label();
            lblScheduleSubtitle.Text = "Загрузка расписания...";
            lblScheduleSubtitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblScheduleSubtitle.AutoSize = true;
            lblScheduleSubtitle.Location = new Point(13, 33);

            btnScheduleHeaderEdit = new Button();
            btnScheduleHeaderEdit.Text = "⚙️ Настроить";
            btnScheduleHeaderEdit.Size = new Size(95, 28);
            btnScheduleHeaderEdit.Location = new Point(panelScheduleCard.Width - 110, 14);
            btnScheduleHeaderEdit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnScheduleHeaderEdit.FlatStyle = FlatStyle.Flat;
            btnScheduleHeaderEdit.FlatAppearance.BorderSize = 1;
            btnScheduleHeaderEdit.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            btnScheduleHeaderEdit.Cursor = Cursors.Hand;
            btnScheduleHeaderEdit.Click += new EventHandler(MenuSchedule_Click);

            panelScheduleHeader.Controls.Add(lblScheduleTitle);
            panelScheduleHeader.Controls.Add(lblScheduleSubtitle);
            panelScheduleHeader.Controls.Add(btnScheduleHeaderEdit);

            // Прокручиваемый контейнер элементов списка уроков
            panelScheduleScroll = new DoubleBufferedPanel();
            panelScheduleScroll.Dock = DockStyle.Fill;
            panelScheduleScroll.AutoScroll = true;
            panelScheduleScroll.Padding = new Padding(8);

            panelScheduleCard.Controls.Add(panelScheduleScroll);
            panelScheduleCard.Controls.Add(panelScheduleHeader);

            // Добавление карточек на форму
            this.Controls.Add(panelCard);
            this.Controls.Add(panelScheduleCard);
            this.Controls.Add(menuStripMain);

            // ==========================================
            // Системный таймер обновления данных (500 мс)
            // ==========================================
            timerTick = new Timer();
            timerTick.Interval = 500;
            timerTick.Tick += new EventHandler(TimerTick_Tick);

            // Обработчики событий окна
            this.Resize += new EventHandler(MainForm_Resize);
            this.KeyDown += new KeyEventHandler(MainForm_KeyDown);

            // Установка видимости меню из настроек
            if (settings.HideToolBar)
            {
                menuStripMain.Visible = false;
            }
        }

        /// <summary>
        /// Построение графических строк уроков внутри боковой панели расписания.
        /// </summary>
        private void BuildScheduleListItems()
        {
            panelScheduleScroll.SuspendLayout();
            panelScheduleScroll.Controls.Clear();
            lessonRowControls.Clear();

            int yOffset = 6;
            int itemWidth = Math.Max(220, panelScheduleScroll.ClientSize.Width - 16);

            if (settings.Lessons != null)
            {
                for (int i = 0; i < settings.Lessons.Count; i++)
                {
                    LessonItem item = settings.Lessons[i];
                    LessonRowControl row = new LessonRowControl(item, currentPalette);
                    row.Location = new Point(8, yOffset);
                    row.Size = new Size(itemWidth, 52);
                    row.DoubleClick += new EventHandler(MenuSchedule_Click);

                    panelScheduleScroll.Controls.Add(row);
                    lessonRowControls.Add(row);

                    yOffset += 58;
                }
            }

            panelScheduleScroll.ResumeLayout();
        }

        /// <summary>
        /// Загружает пользовательское фоновое изображение из файла.
        /// </summary>
        private void LoadCustomBackgroundImage()
        {
            if (loadedBackgroundImage != null)
            {
                loadedBackgroundImage.Dispose();
                loadedBackgroundImage = null;
            }

            if (settings.BgMode == BackgroundMode.Image && !string.IsNullOrEmpty(settings.BackgroundImagePath))
            {
                try
                {
                    if (File.Exists(settings.BackgroundImagePath))
                    {
                        byte[] fileBytes = File.ReadAllBytes(settings.BackgroundImagePath);
                        using (MemoryStream ms = new MemoryStream(fileBytes))
                        {
                            loadedBackgroundImage = Image.FromStream(ms);
                        }
                    }
                }
                catch (Exception)
                {
                    settings.BgMode = BackgroundMode.DefaultTheme;
                    loadedBackgroundImage = null;
                }
            }
        }

        /// <summary>
        /// Применяет цветовую палитру темы и настройки фона ко всем элементам окна.
        /// </summary>
        private void ApplyThemeAndBackground()
        {
            this.currentPalette = settings.GetCurrentPalette();

            // Верхнее меню
            menuStripMain.BackColor = currentPalette.ToolbarBackground;
            menuStripMain.ForeColor = currentPalette.ToolbarForeground;
            foreach (ToolStripItem item in menuStripMain.Items)
            {
                item.ForeColor = currentPalette.ToolbarForeground;
            }

            menuThemeDark.Checked = (settings.Theme == ClockThemeType.DarkOffice);
            menuThemeLight.Checked = (settings.Theme == ClockThemeType.LightOffice);
            menuScheduleListToggle.Checked = settings.ShowScheduleList;
            menuAlwaysOnTop.Checked = settings.AlwaysOnTop;
            ctxAlwaysOnTop.Checked = settings.AlwaysOnTop;

            // Фон окна
            if (settings.BgMode == BackgroundMode.Image && loadedBackgroundImage != null)
            {
                this.BackgroundImage = loadedBackgroundImage;
                this.BackgroundImageLayout = settings.BackgroundImageLayout;
                this.BackColor = currentPalette.WindowBackground;
            }
            else if (settings.BgMode == BackgroundMode.SolidColor)
            {
                this.BackgroundImage = null;
                this.BackColor = Color.FromArgb(settings.CustomBgColorArgb);
            }
            else
            {
                this.BackgroundImage = null;
                this.BackColor = currentPalette.WindowBackground;
            }

            // Центральная карточка (Часы)
            panelCard.BackColor = currentPalette.CardBackground;
            lblDate.ForeColor = currentPalette.SubtitleText;
            lblCurrentTime.ForeColor = currentPalette.ClockDigits;
            lblStatusBadge.ForeColor = currentPalette.StatusText;
            lblCountdownTitle.ForeColor = currentPalette.SubtitleText;
            lblCountdown.ForeColor = currentPalette.CountdownDigits;
            lblProgressPercent.ForeColor = currentPalette.SubtitleText;

            // Боковая карточка (Расписание)
            panelScheduleCard.BackColor = currentPalette.ScheduleCardBackground;
            lblScheduleTitle.ForeColor = currentPalette.ClockDigits;
            lblScheduleSubtitle.ForeColor = currentPalette.SubtitleText;

            btnScheduleHeaderEdit.BackColor = (settings.Theme == ClockThemeType.DarkOffice)
                ? Color.FromArgb(42, 48, 62)
                : Color.FromArgb(240, 243, 248);
            btnScheduleHeaderEdit.ForeColor = currentPalette.ToolbarForeground;
            btnScheduleHeaderEdit.FlatAppearance.BorderColor = currentPalette.CardBorder;

            // Обновление палитры для всех строк уроков в списке
            for (int i = 0; i < lessonRowControls.Count; i++)
            {
                lessonRowControls[i].Palette = currentPalette;
            }

            panelCard.Invalidate();
            panelProgressBar.Invalidate();
            panelScheduleCard.Invalidate();
            this.Invalidate();
        }

        /// <summary>
        /// Отрисовка рамки и фона центральной карточки часов.
        /// </summary>
        private void PanelCard_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Pen borderPen = new Pen(currentPalette.CardBorder, 2f))
            {
                Rectangle rect = new Rectangle(1, 1, panelCard.Width - 3, panelCard.Height - 3);
                g.DrawRectangle(borderPen, rect);
            }

            Rectangle badgeRect = new Rectangle(lblStatusBadge.Left - 10, lblStatusBadge.Top, lblStatusBadge.Width + 20, lblStatusBadge.Height);
            using (SolidBrush badgeBrush = new SolidBrush(currentPalette.StatusBadgeBackground))
            {
                g.FillRectangle(badgeBrush, badgeRect);
            }
            using (Pen badgeBorderPen = new Pen(currentPalette.CardBorder, 1f))
            {
                g.DrawRectangle(badgeBorderPen, badgeRect);
            }
        }

        /// <summary>
        /// Отрисовка рамки боковой карточки расписания и разделительной полосы заголовка.
        /// </summary>
        private void PanelScheduleCard_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Pen borderPen = new Pen(currentPalette.ScheduleCardBorder, 2f))
            {
                Rectangle rect = new Rectangle(1, 1, panelScheduleCard.Width - 3, panelScheduleCard.Height - 3);
                g.DrawRectangle(borderPen, rect);
            }

            // Тонкая линия под шапкой расписания
            using (Pen sepPen = new Pen(currentPalette.ScheduleCardBorder, 1f))
            {
                g.DrawLine(sepPen, 14, panelScheduleHeader.Height, panelScheduleCard.Width - 14, panelScheduleHeader.Height);
            }
        }

        /// <summary>
        /// Отрисовка прогресс-бара прошедшего времени урока/перемены.
        /// </summary>
        private void PanelProgressBar_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int width = panelProgressBar.Width;
            int height = panelProgressBar.Height;

            using (SolidBrush bgBrush = new SolidBrush(currentPalette.CardBorder))
            {
                g.FillRectangle(bgBrush, 0, 0, width, height);
            }

            int fillWidth = (int)((width * Math.Max(0, Math.Min(100, currentProgressPercent))) / 100.0);
            if (fillWidth > 0)
            {
                using (SolidBrush fillBrush = new SolidBrush(currentPalette.CountdownDigits))
                {
                    g.FillRectangle(fillBrush, 0, 0, fillWidth, height);
                }
            }
        }

        /// <summary>
        /// Тик таймера: обновление системного времени, обратного отсчета и подсветки в боковом списке уроков.
        /// </summary>
        private void TimerTick_Tick(object sender, EventArgs e)
        {
            UpdateClockAndSchedule();
        }

        /// <summary>
        /// Центральный метод расчета времени, определения текущего урока/перемены и обновления бокового списка.
        /// </summary>
        private void UpdateClockAndSchedule()
        {
            DateTime now = DateTime.Now;

            // 1. Дата и системное время
            lblDate.Text = now.ToString("D", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));
            lblCurrentTime.Text = now.ToString("HH:mm:ss");

            // 2. Расчет расписания
            TimeSpan currentTimeOfDay = now.TimeOfDay;
            List<LessonItem> lessons = settings.Lessons;

            if (lessons == null || lessons.Count == 0)
            {
                lblStatusBadge.Text = "Расписание уроков не задано";
                lblCountdownTitle.Text = "НАСТРОЙКА РАСПИСАНИЯ";
                lblCountdown.Text = "00:00";
                lblProgressPercent.Text = "Нажмите «📋 Расписание» в верхнем меню для добавления звонков.";
                lblScheduleSubtitle.Text = "Список уроков пуст";
                currentProgressPercent = 0;
                panelProgressBar.Invalidate();
                return;
            }

            bool isLessonActive = false;
            bool isBreakActive = false;
            LessonItem currentLesson = null;
            LessonItem nextLesson = null;
            TimeSpan targetTime = TimeSpan.Zero;
            TimeSpan intervalStart = TimeSpan.Zero;
            TimeSpan intervalEnd = TimeSpan.Zero;

            // Проверка первого урока
            TimeSpan firstStart, firstEnd;
            lessons[0].TryGetTimeSpans(out firstStart, out firstEnd);

            if (currentTimeOfDay < firstStart)
            {
                lblStatusBadge.Text = string.Format("До начала занятий (первый урок в {0})", lessons[0].StartTime);
                lblCountdownTitle.Text = "ДО ПЕРВОГО ЗВОНКА ОСТАЛОСЬ";
                targetTime = firstStart;
                TimeSpan diffBefore = targetTime - currentTimeOfDay;
                lblCountdown.Text = FormatCountdown(diffBefore);
                lblProgressPercent.Text = string.Format("Первый урок: {0} ({1} – {2})", lessons[0].Name, lessons[0].StartTime, lessons[0].EndTime);
                lblScheduleSubtitle.Text = string.Format("Всего уроков: {0} | Старт в {1}", lessons.Count, lessons[0].StartTime);
                currentProgressPercent = 0;
                panelProgressBar.Invalidate();

                UpdateScheduleListHighlight(null, lessons[0], false, currentTimeOfDay);
                return;
            }

            // Поиск активного урока или перемены
            for (int i = 0; i < lessons.Count; i++)
            {
                TimeSpan start, end;
                if (!lessons[i].TryGetTimeSpans(out start, out end))
                {
                    continue;
                }

                if (currentTimeOfDay >= start && currentTimeOfDay < end)
                {
                    isLessonActive = true;
                    currentLesson = lessons[i];
                    targetTime = end;
                    intervalStart = start;
                    intervalEnd = end;
                    break;
                }

                if (i < lessons.Count - 1)
                {
                    TimeSpan nextStart, nextEnd;
                    if (lessons[i + 1].TryGetTimeSpans(out nextStart, out nextEnd))
                    {
                        if (currentTimeOfDay >= end && currentTimeOfDay < nextStart)
                        {
                            isBreakActive = true;
                            currentLesson = lessons[i];
                            nextLesson = lessons[i + 1];
                            targetTime = nextStart;
                            intervalStart = end;
                            intervalEnd = nextStart;
                            break;
                        }
                    }
                }
            }

            if (isLessonActive && currentLesson != null)
            {
                lblStatusBadge.Text = string.Format("Идет {0} ({1} – {2})", currentLesson.Name, currentLesson.StartTime, currentLesson.EndTime);
                lblCountdownTitle.Text = "ДО КОНЦА УРОКА (ДО ЗВОНКА):";

                TimeSpan remaining = targetTime - currentTimeOfDay;
                lblCountdown.Text = FormatCountdown(remaining);

                double totalSec = (intervalEnd - intervalStart).TotalSeconds;
                double elapsedSec = (currentTimeOfDay - intervalStart).TotalSeconds;
                currentProgressPercent = totalSec > 0 ? (int)((elapsedSec / totalSec) * 100) : 0;
                currentProgressPercent = Math.Max(0, Math.Min(100, currentProgressPercent));

                lblProgressPercent.Text = string.Format("Прошло {0}% времени урока | Всего длится: {1} мин.",
                    currentProgressPercent, (int)(intervalEnd - intervalStart).TotalMinutes);

                lblScheduleSubtitle.Text = string.Format("Идет {0} из {1} уроков | Звонок в {2}",
                    currentLesson.Number, lessons.Count, currentLesson.EndTime);

                UpdateScheduleListHighlight(currentLesson, null, false, currentTimeOfDay);
            }
            else if (isBreakActive && nextLesson != null)
            {
                lblStatusBadge.Text = string.Format("Перемена перед {0} (до {1})", nextLesson.Name, nextLesson.StartTime);
                lblCountdownTitle.Text = "ДО НАЧАЛА УРОКА ОСТАЛОСЬ:";

                TimeSpan remaining = targetTime - currentTimeOfDay;
                lblCountdown.Text = FormatCountdown(remaining);

                double totalSec = (intervalEnd - intervalStart).TotalSeconds;
                double elapsedSec = (currentTimeOfDay - intervalStart).TotalSeconds;
                currentProgressPercent = totalSec > 0 ? (int)((elapsedSec / totalSec) * 100) : 0;
                currentProgressPercent = Math.Max(0, Math.Min(100, currentProgressPercent));

                lblProgressPercent.Text = string.Format("Прошло {0}% перемены | Длительность: {1} мин.",
                    currentProgressPercent, (int)(intervalEnd - intervalStart).TotalMinutes);

                lblScheduleSubtitle.Text = string.Format("Перемена | Следующий: {0} ({1})",
                    nextLesson.Name, nextLesson.StartTime);

                UpdateScheduleListHighlight(null, nextLesson, true, currentTimeOfDay);
            }
            else
            {
                lblStatusBadge.Text = "Уроки окончены";
                lblCountdownTitle.Text = "ЗАНЯТИЯ НА СЕГОДНЯ ЗАВЕРШЕНЫ";
                lblCountdown.Text = "00:00";
                lblProgressPercent.Text = "Учебный день завершен. Отличного отдыха!";
                lblScheduleSubtitle.Text = string.Format("Все {0} уроков на сегодня окончены", lessons.Count);
                currentProgressPercent = 100;

                UpdateScheduleListHighlight(null, null, false, currentTimeOfDay);
            }

            panelProgressBar.Invalidate();
        }

        /// <summary>
        /// Интеллектуальное обновление статусов подсветки в боковом списке уроков.
        /// </summary>
        private void UpdateScheduleListHighlight(LessonItem current, LessonItem next, bool isBreak, TimeSpan now)
        {
            for (int i = 0; i < lessonRowControls.Count; i++)
            {
                LessonRowControl row = lessonRowControls[i];
                LessonItem item = row.Lesson;
                TimeSpan start, end;
                if (!item.TryGetTimeSpans(out start, out end))
                {
                    continue;
                }

                if (current != null && item.Number == current.Number)
                {
                    row.State = LessonRowState.Active;
                }
                else if (isBreak && next != null && item.Number == next.Number)
                {
                    row.State = LessonRowState.NextBreak;
                }
                else if (now >= end)
                {
                    row.State = LessonRowState.Past;
                }
                else
                {
                    row.State = LessonRowState.Upcoming;
                }
            }
        }

        /// <summary>
        /// Форматирование интервала времени в "ММ:СС" или "ЧЧ:ММ:СС".
        /// </summary>
        private string FormatCountdown(TimeSpan span)
        {
            if (span.Ticks < 0)
            {
                return "00:00";
            }

            if (span.TotalHours >= 1)
            {
                return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)span.TotalHours, span.Minutes, span.Seconds);
            }

            return string.Format("{0:D2}:{1:D2}", span.Minutes, span.Seconds);
        }

        /// <summary>
        /// Обработчик изменения размера окна.
        /// </summary>
        private void MainForm_Resize(object sender, EventArgs e)
        {
            CenterClockCard();
        }

        /// <summary>
        /// Адаптивное центрирование карточки часов и бокового расписания на экране.
        /// </summary>
        private void CenterClockCard()
        {
            if (panelCard == null || panelScheduleCard == null) return;

            int menuHeight = (menuStripMain != null && menuStripMain.Visible) ? menuStripMain.Height : 0;
            int availW = this.ClientSize.Width;
            int availH = this.ClientSize.Height - menuHeight;

            bool showSideList = settings.ShowScheduleList && (availW >= 780);
            panelScheduleCard.Visible = showSideList;

            if (!showSideList)
            {
                // Режим отображения только часов по центру
                int cardWidth = Math.Min(availW - 40, 780);
                int cardHeight = Math.Min(availH - 40, 480);

                if (cardWidth < 500) cardWidth = Math.Max(320, availW - 10);
                if (cardHeight < 360) cardHeight = Math.Max(260, availH - 10);

                panelCard.Size = new Size(cardWidth, cardHeight);
                int posX = Math.Max(0, (availW - cardWidth) / 2);
                int posY = menuHeight + Math.Max(0, (availH - cardHeight) / 2);
                panelCard.Location = new Point(posX, posY);
            }
            else
            {
                // Режим совместного отображения: Часы (слева) + Расписание (справа)
                int gap = 20;
                int commonHeight = Math.Min(availH - 40, 500);
                if (commonHeight < 360) commonHeight = Math.Max(280, availH - 20);

                int maxTotalWidth = Math.Min(availW - 40, 1180);
                int scheduleWidth = Math.Max(300, Math.Min(380, (int)(maxTotalWidth * 0.35)));
                int clockWidth = maxTotalWidth - scheduleWidth - gap;

                if (clockWidth < 460)
                {
                    clockWidth = Math.Max(360, (int)(maxTotalWidth * 0.58));
                    scheduleWidth = maxTotalWidth - clockWidth - gap;
                }

                panelCard.Size = new Size(clockWidth, commonHeight);
                panelScheduleCard.Size = new Size(scheduleWidth, commonHeight);

                int startX = Math.Max(10, (availW - maxTotalWidth) / 2);
                int startY = menuHeight + Math.Max(10, (availH - commonHeight) / 2);

                panelCard.Location = new Point(startX, startY);
                panelScheduleCard.Location = new Point(startX + clockWidth + gap, startY);

                // Корректировка ширины строк внутри панели прокрутки расписания
                int rowWidth = Math.Max(180, panelScheduleScroll.ClientSize.Width - 16);
                for (int i = 0; i < lessonRowControls.Count; i++)
                {
                    lessonRowControls[i].Width = rowWidth;
                }
            }

            // Адаптивная подгонка элементов внутри карточки часов
            int contentWidth = panelCard.Width - 40;
            lblDate.Width = contentWidth;
            lblCurrentTime.Width = contentWidth;
            lblStatusBadge.Width = Math.Min(contentWidth, 680);
            lblStatusBadge.Left = (panelCard.Width - lblStatusBadge.Width) / 2;
            lblCountdownTitle.Width = contentWidth;
            lblCountdown.Width = contentWidth;
            panelProgressBar.Width = Math.Min(contentWidth - 60, 600);
            panelProgressBar.Left = (panelCard.Width - panelProgressBar.Width) / 2;
            lblProgressPercent.Width = contentWidth;
        }

        // =============================================================
        // Команды меню и переключения режимов
        // =============================================================

        /// <summary>
        /// Открытие диалогового окна расписания.
        /// </summary>
        private void MenuSchedule_Click(object sender, EventArgs e)
        {
            using (ScheduleForm form = new ScheduleForm(this.settings))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    BuildScheduleListItems();
                    ApplyThemeAndBackground();
                    CenterClockCard();
                    UpdateClockAndSchedule();
                }
            }
        }

        /// <summary>
        /// Включение / отключение боковой панели со списком уроков.
        /// </summary>
        private void MenuScheduleListToggle_Click(object sender, EventArgs e)
        {
            ToggleScheduleListVisibility();
        }

        /// <summary>
        /// Логика переключения видимости бокового списка уроков.
        /// </summary>
        private void ToggleScheduleListVisibility()
        {
            settings.ShowScheduleList = !settings.ShowScheduleList;
            menuScheduleListToggle.Checked = settings.ShowScheduleList;
            AppSettings.Save(settings, AppSettings.GetConfigFilePath());

            CenterClockCard();
            UpdateClockAndSchedule();
        }

        /// <summary>
        /// Переключение на темную офисную тему.
        /// </summary>
        private void MenuThemeDark_Click(object sender, EventArgs e)
        {
            settings.Theme = ClockThemeType.DarkOffice;
            AppSettings.Save(settings, AppSettings.GetConfigFilePath());
            ApplyThemeAndBackground();
        }

        /// <summary>
        /// Переключение на светлую офисную тему.
        /// </summary>
        private void MenuThemeLight_Click(object sender, EventArgs e)
        {
            settings.Theme = ClockThemeType.LightOffice;
            AppSettings.Save(settings, AppSettings.GetConfigFilePath());
            ApplyThemeAndBackground();
        }

        /// <summary>
        /// Быстрое переключение темы из контекстного меню.
        /// </summary>
        private void CtxThemeToggle_Click(object sender, EventArgs e)
        {
            if (settings.Theme == ClockThemeType.DarkOffice)
            {
                MenuThemeLight_Click(sender, e);
            }
            else
            {
                MenuThemeDark_Click(sender, e);
            }
        }

        /// <summary>
        /// Выбор цвета фона через стандартный диалог Windows.
        /// </summary>
        private void MenuBgColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog dlg = new ColorDialog())
            {
                dlg.Color = Color.FromArgb(settings.CustomBgColorArgb);
                dlg.FullOpen = true;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    settings.BgMode = BackgroundMode.SolidColor;
                    settings.CustomBgColorArgb = dlg.Color.ToArgb();
                    AppSettings.Save(settings, AppSettings.GetConfigFilePath());
                    ApplyThemeAndBackground();
                }
            }
        }

        /// <summary>
        /// Выбор пользовательского фонового изображения.
        /// </summary>
        private void MenuBgImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Выберите фоновое изображение для часов";
                dlg.Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Все файлы (*.*)|*.*";
                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    settings.BgMode = BackgroundMode.Image;
                    settings.BackgroundImagePath = dlg.FileName;
                    settings.BackgroundImageLayout = ImageLayout.Stretch;

                    AppSettings.Save(settings, AppSettings.GetConfigFilePath());
                    LoadCustomBackgroundImage();
                    ApplyThemeAndBackground();
                }
            }
        }

        /// <summary>
        /// Сброс фона окна к стандартному цвету выбранной темы.
        /// </summary>
        private void MenuBgReset_Click(object sender, EventArgs e)
        {
            settings.BgMode = BackgroundMode.DefaultTheme;
            settings.BackgroundImagePath = string.Empty;
            AppSettings.Save(settings, AppSettings.GetConfigFilePath());

            if (loadedBackgroundImage != null)
            {
                loadedBackgroundImage.Dispose();
                loadedBackgroundImage = null;
            }

            ApplyThemeAndBackground();
        }

        /// <summary>
        /// Обработчик пункта меню «Поверх всех окон».
        /// </summary>
        private void MenuAlwaysOnTop_Click(object sender, EventArgs e)
        {
            ToggleAlwaysOnTop();
        }

        /// <summary>
        /// Переключение режима удержания окна поверх всех остальных окон.
        /// Используется безопасное управляемое свойство .NET Windows Forms (TopMost),
        /// исключающее ложные срабатывания антивирусов.
        /// </summary>
        private void ToggleAlwaysOnTop()
        {
            settings.AlwaysOnTop = !settings.AlwaysOnTop;
            menuAlwaysOnTop.Checked = settings.AlwaysOnTop;
            ctxAlwaysOnTop.Checked = settings.AlwaysOnTop;
            this.TopMost = settings.AlwaysOnTop;

            if (settings.AlwaysOnTop)
            {
                this.BringToFront();
            }

            AppSettings.Save(settings, AppSettings.GetConfigFilePath());
        }

        /// <summary>
        /// Переключение полноэкранного режима (F11).
        /// </summary>
        private void MenuFullScreen_Click(object sender, EventArgs e)
        {
            ToggleFullScreen();
        }

        /// <summary>
        /// Переключение видимости верхнего меню.
        /// </summary>
        private void MenuToggleMenu_Click(object sender, EventArgs e)
        {
            ToggleMenuVisibility();
        }

        /// <summary>
        /// Логика разворачивания во весь экран и возврата в оконный режим.
        /// </summary>
        private void ToggleFullScreen()
        {
            if (!isFullScreenActive)
            {
                previousWindowState = this.WindowState;
                previousBorderStyle = this.FormBorderStyle;

                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                isFullScreenActive = true;
                menuFullScreen.Text = "⛶ Выйти из полноэкранного режима (F11/Esc)";
                ctxFullScreen.Text = "⛶ Выйти из полноэкранного режима (F11/Esc)";
            }
            else
            {
                this.FormBorderStyle = previousBorderStyle;
                this.WindowState = previousWindowState;
                isFullScreenActive = false;
                menuFullScreen.Text = "⛶ Полный экран (F11)";
                ctxFullScreen.Text = "⛶ Во весь экран (F11)";
            }

            CenterClockCard();
        }

        /// <summary>
        /// Скрытие или отображение панели меню.
        /// </summary>
        private void ToggleMenuVisibility()
        {
            menuStripMain.Visible = !menuStripMain.Visible;
            settings.HideToolBar = !menuStripMain.Visible;
            AppSettings.Save(settings, AppSettings.GetConfigFilePath());

            CenterClockCard();
        }

        /// <summary>
        /// Обработка горячих клавиш:
        /// F11 — полный экран,
        /// F10 — переключение режима «Поверх всех окон»,
        /// F9 — переключение списка уроков справа,
        /// Esc — выход из полноэкранного режима или возврат меню,
        /// Alt+M — переключение верхнего меню.
        /// </summary>
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F10)
            {
                ToggleAlwaysOnTop();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F9)
            {
                ToggleScheduleListVisibility();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                if (isFullScreenActive)
                {
                    ToggleFullScreen();
                    e.Handled = true;
                }
                else if (!menuStripMain.Visible)
                {
                    menuStripMain.Visible = true;
                    CenterClockCard();
                    e.Handled = true;
                }
            }
            else if (e.Alt && e.KeyCode == Keys.M)
            {
                ToggleMenuVisibility();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Освобождение системных ресурсов.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (timerTick != null)
                {
                    timerTick.Stop();
                    timerTick.Dispose();
                }
                if (loadedBackgroundImage != null)
                {
                    loadedBackgroundImage.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
