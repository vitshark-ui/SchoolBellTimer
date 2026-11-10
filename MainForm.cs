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
    /// Главное окно приложения SchoolBellTimer.
    /// Содержит центральные электронные часы, таймер обратного отсчета до звонка,
    /// панель управления, переключение тем и полноэкранный режим.
    /// </summary>
    public class MainForm : Form
    {
        // -------------------------------------------------------------
        // Элементы пользовательского интерфейса
        // -------------------------------------------------------------
        private MenuStrip menuStripMain;
        private ToolStripMenuItem menuSchedule;
        private ToolStripMenuItem menuTheme;
        private ToolStripMenuItem menuThemeDark;
        private ToolStripMenuItem menuThemeLight;
        private ToolStripMenuItem menuBackground;
        private ToolStripMenuItem menuBgColor;
        private ToolStripMenuItem menuBgImage;
        private ToolStripMenuItem menuBgReset;
        private ToolStripMenuItem menuFullScreen;
        private ToolStripMenuItem menuToggleMenu;

        private ContextMenuStrip contextMenuQuick;
        private ToolStripMenuItem ctxSchedule;
        private ToolStripMenuItem ctxThemeToggle;
        private ToolStripMenuItem ctxFullScreen;
        private ToolStripMenuItem ctxToggleMenu;
        private ToolStripMenuItem ctxExit;

        // Центральная карточка-виджет часов
        private DoubleBufferedPanel panelCard;
        private Label lblDate;
        private Label lblCurrentTime;
        private Label lblStatusBadge;
        private Label lblCountdownTitle;
        private Label lblCountdown;
        private DoubleBufferedPanel panelProgressBar;
        private Label lblProgressPercent;

        // Системный таймер обновления времени (интервал 500 мс для мгновенной реакции)
        private Timer timerTick;

        // Настройки приложения и текущее состояние
        private AppSettings settings;
        private ThemePalette currentPalette;
        private Image loadedBackgroundImage = null;

        // Сохранение состояния окна перед разворачиванием на весь экран
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
            // Включение двойной буферизации на уровне формы против мерцания
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            this.UpdateStyles();

            // Загрузка настроек из settings.xml (или создание демо-расписания)
            this.settings = AppSettings.Load(AppSettings.GetConfigFilePath());
            this.currentPalette = this.settings.GetCurrentPalette();

            // Создание и конфигурация контролов формы
            InitializeComponents();

            // Загрузка сохраненного фонового изображения, если задано
            LoadCustomBackgroundImage();

            // Применение темы и фона
            ApplyThemeAndBackground();

            // Первичный расчет и отображение времени до первого тика таймера
            UpdateClockAndSchedule();

            // Запуск таймера
            timerTick.Start();
        }

        /// <summary>
        /// Инициализация графических компонентов формы.
        /// </summary>
        private void InitializeComponents()
        {
            this.Text = "SchoolBellTimer — Электронные офисные часы и расписание уроков";
            this.Size = new Size(1000, 700);
            this.MinimumSize = new Size(640, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;

            // ==========================================
            // Верхнее главное меню (MenuStrip)
            // ==========================================
            menuStripMain = new MenuStrip();
            menuStripMain.Dock = DockStyle.Top;
            menuStripMain.Font = new Font("Segoe UI", 9.5F);

            // Меню: Расписание
            menuSchedule = new ToolStripMenuItem("📋 Расписание уроков");
            menuSchedule.Click += new EventHandler(MenuSchedule_Click);

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
                menuFullScreen,
                menuToggleMenu
            });

            // ==========================================
            // Контекстное меню по правому клику мыши
            // ==========================================
            contextMenuQuick = new ContextMenuStrip();
            ctxSchedule = new ToolStripMenuItem("📋 Настроить расписание...", null, new EventHandler(MenuSchedule_Click));
            ctxThemeToggle = new ToolStripMenuItem("🎨 Переключить тему (Светлая / Темная)", null, new EventHandler(CtxThemeToggle_Click));
            ctxFullScreen = new ToolStripMenuItem("⛶ Во весь экран (F11)", null, new EventHandler(MenuFullScreen_Click));
            ctxToggleMenu = new ToolStripMenuItem("👁️ Показать / скрыть верхнее меню", null, new EventHandler(MenuToggleMenu_Click));
            ctxExit = new ToolStripMenuItem("✖ Выход из программы", null, delegate { Application.Exit(); });

            contextMenuQuick.Items.AddRange(new ToolStripItem[] {
                ctxSchedule,
                ctxThemeToggle,
                new ToolStripSeparator(),
                ctxFullScreen,
                ctxToggleMenu,
                new ToolStripSeparator(),
                ctxExit
            });
            this.ContextMenuStrip = contextMenuQuick;

            // ==========================================
            // Центральная информационная карточка (Panel)
            // ==========================================
            panelCard = new DoubleBufferedPanel();
            panelCard.Size = new Size(760, 480);
            panelCard.Paint += new PaintEventHandler(PanelCard_Paint);
            panelCard.ContextMenuStrip = contextMenuQuick;

            // 1. Блок: Текущая дата и день недели
            lblDate = new Label();
            lblDate.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            lblDate.TextAlign = ContentAlignment.MiddleCenter;
            lblDate.Size = new Size(720, 26);
            lblDate.Location = new Point(20, 24);

            // 2. Блок: Текущее системное время (крупно)
            lblCurrentTime = new Label();
            lblCurrentTime.Font = new Font("Consolas", 46F, FontStyle.Bold);
            lblCurrentTime.TextAlign = ContentAlignment.MiddleCenter;
            lblCurrentTime.Size = new Size(720, 72);
            lblCurrentTime.Location = new Point(20, 52);
            lblCurrentTime.ContextMenuStrip = contextMenuQuick;

            // 3. Блок: Текущий статус (Идет 1 урок / Перемена / Уроки окончены)
            lblStatusBadge = new Label();
            lblStatusBadge.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblStatusBadge.TextAlign = ContentAlignment.MiddleCenter;
            lblStatusBadge.Size = new Size(700, 50);
            lblStatusBadge.Location = new Point(30, 138);

            // 4. Блок: Подзаголовок обратного отсчета
            lblCountdownTitle = new Label();
            lblCountdownTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblCountdownTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblCountdownTitle.Text = "ДО ЗВОНКА ОСТАЛОСЬ";
            lblCountdownTitle.Size = new Size(720, 24);
            lblCountdownTitle.Location = new Point(20, 206);

            // 5. Блок: Крупный таймер обратного отсчета (ММ:СС)
            lblCountdown = new Label();
            lblCountdown.Font = new Font("Consolas", 76F, FontStyle.Bold);
            lblCountdown.TextAlign = ContentAlignment.MiddleCenter;
            lblCountdown.Size = new Size(720, 120);
            lblCountdown.Location = new Point(20, 230);
            lblCountdown.ContextMenuStrip = contextMenuQuick;

            // 6. Блок: Визуальный индикатор прогресса (прошло времени урока/перемены)
            panelProgressBar = new DoubleBufferedPanel();
            panelProgressBar.Size = new Size(620, 12);
            panelProgressBar.Location = new Point(70, 375);
            panelProgressBar.Paint += new PaintEventHandler(PanelProgressBar_Paint);

            lblProgressPercent = new Label();
            lblProgressPercent.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            lblProgressPercent.TextAlign = ContentAlignment.MiddleCenter;
            lblProgressPercent.Size = new Size(720, 22);
            lblProgressPercent.Location = new Point(20, 395);

            // Сборка элементов в центральную карточку
            panelCard.Controls.Add(lblDate);
            panelCard.Controls.Add(lblCurrentTime);
            panelCard.Controls.Add(lblStatusBadge);
            panelCard.Controls.Add(lblCountdownTitle);
            panelCard.Controls.Add(lblCountdown);
            panelCard.Controls.Add(panelProgressBar);
            panelCard.Controls.Add(lblProgressPercent);

            // Добавление компонентов на главную форму
            this.Controls.Add(panelCard);
            this.Controls.Add(menuStripMain);

            // ==========================================
            // Таймер обновления данных (500 мс)
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
        /// Загружает пользовательское фоновое изображение из файла, если путь сохранен в настройках.
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
                        // Загружаем в MemoryStream, чтобы не блокировать файл на диске
                        byte[] fileBytes = File.ReadAllBytes(settings.BackgroundImagePath);
                        using (MemoryStream ms = new MemoryStream(fileBytes))
                        {
                            loadedBackgroundImage = Image.FromStream(ms);
                        }
                    }
                }
                catch (Exception)
                {
                    // В случае ошибки чтения возвращаем стандартный фоновый режим
                    settings.BgMode = BackgroundMode.DefaultTheme;
                    loadedBackgroundImage = null;
                }
            }
        }

        /// <summary>
        /// Применяет цветовую палитру темы и настройки фонового рисунка ко всем элементам окна.
        /// </summary>
        private void ApplyThemeAndBackground()
        {
            this.currentPalette = settings.GetCurrentPalette();

            // Настройка меню
            menuStripMain.BackColor = currentPalette.ToolbarBackground;
            menuStripMain.ForeColor = currentPalette.ToolbarForeground;
            foreach (ToolStripItem item in menuStripMain.Items)
            {
                item.ForeColor = currentPalette.ToolbarForeground;
            }

            // Отметки выбранной темы в меню
            menuThemeDark.Checked = (settings.Theme == ClockThemeType.DarkOffice);
            menuThemeLight.Checked = (settings.Theme == ClockThemeType.LightOffice);

            // Оформление фона окна
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

            // Цвета центральной панели-карточки
            panelCard.BackColor = currentPalette.CardBackground;
            lblDate.ForeColor = currentPalette.SubtitleText;
            lblCurrentTime.ForeColor = currentPalette.ClockDigits;
            lblStatusBadge.ForeColor = currentPalette.StatusText;
            lblCountdownTitle.ForeColor = currentPalette.SubtitleText;
            lblCountdown.ForeColor = currentPalette.CountdownDigits;
            lblProgressPercent.ForeColor = currentPalette.SubtitleText;

            // Перерисовка компонентов
            panelCard.Invalidate();
            panelProgressBar.Invalidate();
            this.Invalidate();
        }

        /// <summary>
        /// Отрисовка рамки и фона центральной карточки часов.
        /// </summary>
        private void PanelCard_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Рисуем мягкую границу карточки
            using (Pen borderPen = new Pen(currentPalette.CardBorder, 2f))
            {
                Rectangle rect = new Rectangle(1, 1, panelCard.Width - 3, panelCard.Height - 3);
                g.DrawRectangle(borderPen, rect);
            }

            // Отрисовка декоративного фона для плашки статуса
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
        /// Отрисовка современного плоского прогресс-бара прошедшего времени урока/перемены.
        /// </summary>
        private void PanelProgressBar_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int width = panelProgressBar.Width;
            int height = panelProgressBar.Height;

            // Фоновая полоса (незаполненная часть)
            using (SolidBrush bgBrush = new SolidBrush(currentPalette.CardBorder))
            {
                g.FillRectangle(bgBrush, 0, 0, width, height);
            }

            // Заполненная часть прогресса
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
        /// Тик таймера: обновляет текущее время, статус урока и значение обратного отсчета.
        /// </summary>
        private void TimerTick_Tick(object sender, EventArgs e)
        {
            UpdateClockAndSchedule();
        }

        /// <summary>
        /// Центральный метод расчета времени, определения текущего урока/перемены и форматирования строк.
        /// </summary>
        private void UpdateClockAndSchedule()
        {
            DateTime now = DateTime.Now;

            // 1. Обновление даты (например: "Вторник, 10 ноября 2026 г.")
            lblDate.Text = now.ToString("D", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));

            // 2. Обновление системного времени (ЧЧ:ММ:СС)
            lblCurrentTime.Text = now.ToString("HH:mm:ss");

            // 3. Расчет расписания звонков
            TimeSpan currentTimeOfDay = now.TimeOfDay;
            List<LessonItem> lessons = settings.Lessons;

            // Если список уроков пуст
            if (lessons == null || lessons.Count == 0)
            {
                lblStatusBadge.Text = "Расписание уроков не задано";
                lblCountdownTitle.Text = "НАСТРОЙКА РАСПИСАНИЯ";
                lblCountdown.Text = "00:00";
                lblProgressPercent.Text = "Нажмите «📋 Расписание уроков» в верхнем меню для добавления звонков.";
                currentProgressPercent = 0;
                panelProgressBar.Invalidate();
                return;
            }

            // Ищем состояние: идет ли урок, идет ли перемена, уроки еще не начались или уже окончены
            bool isLessonActive = false;
            bool isBreakActive = false;
            LessonItem currentLesson = null;
            LessonItem nextLesson = null;
            TimeSpan targetTime = TimeSpan.Zero;
            TimeSpan intervalStart = TimeSpan.Zero;
            TimeSpan intervalEnd = TimeSpan.Zero;

            // Проверяем первый урок (время до начала первого звонка)
            TimeSpan firstStart, firstEnd;
            lessons[0].TryGetTimeSpans(out firstStart, out firstEnd);

            if (currentTimeOfDay < firstStart)
            {
                // До начала первого урока
                lblStatusBadge.Text = string.Format("До начала занятий (первый урок в {0})", lessons[0].StartTime);
                lblCountdownTitle.Text = "ДО ПЕРВОГО ЗВОНКА ОСТАЛОСЬ";
                targetTime = firstStart;
                TimeSpan diffBefore = targetTime - currentTimeOfDay;
                lblCountdown.Text = FormatCountdown(diffBefore);
                lblProgressPercent.Text = string.Format("Первый урок: {0} ({1} – {2})", lessons[0].Name, lessons[0].StartTime, lessons[0].EndTime);
                currentProgressPercent = 0;
                panelProgressBar.Invalidate();
                return;
            }

            // Проверяем текущие уроки и перемены между ними
            for (int i = 0; i < lessons.Count; i++)
            {
                TimeSpan start, end;
                if (!lessons[i].TryGetTimeSpans(out start, out end))
                {
                    continue;
                }

                // Проверка: идет ли данный урок прямо сейчас
                if (currentTimeOfDay >= start && currentTimeOfDay < end)
                {
                    isLessonActive = true;
                    currentLesson = lessons[i];
                    targetTime = end;
                    intervalStart = start;
                    intervalEnd = end;
                    break;
                }

                // Проверка: идет ли перемена перед следующим уроком
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
                // Идет урок
                lblStatusBadge.Text = string.Format("Идет {0} ({1} – {2})", currentLesson.Name, currentLesson.StartTime, currentLesson.EndTime);
                lblCountdownTitle.Text = "ДО КОНЦА УРОКА (ДО ЗВОНКА):";

                TimeSpan remaining = targetTime - currentTimeOfDay;
                lblCountdown.Text = FormatCountdown(remaining);

                // Расчет процента завершения урока
                double totalSec = (intervalEnd - intervalStart).TotalSeconds;
                double elapsedSec = (currentTimeOfDay - intervalStart).TotalSeconds;
                currentProgressPercent = totalSec > 0 ? (int)((elapsedSec / totalSec) * 100) : 0;
                currentProgressPercent = Math.Max(0, Math.Min(100, currentProgressPercent));

                lblProgressPercent.Text = string.Format("Прошло {0}% времени урока | Всего длится: {1} мин.",
                    currentProgressPercent, (int)(intervalEnd - intervalStart).TotalMinutes);
            }
            else if (isBreakActive && nextLesson != null)
            {
                // Идет перемена
                lblStatusBadge.Text = string.Format("Перемена перед {0} (до {1})", nextLesson.Name, nextLesson.StartTime);
                lblCountdownTitle.Text = "ДО НАЧАЛА УРОКА ОСТАЛОСЬ:";

                TimeSpan remaining = targetTime - currentTimeOfDay;
                lblCountdown.Text = FormatCountdown(remaining);

                // Расчет процента завершения перемены
                double totalSec = (intervalEnd - intervalStart).TotalSeconds;
                double elapsedSec = (currentTimeOfDay - intervalStart).TotalSeconds;
                currentProgressPercent = totalSec > 0 ? (int)((elapsedSec / totalSec) * 100) : 0;
                currentProgressPercent = Math.Max(0, Math.Min(100, currentProgressPercent));

                lblProgressPercent.Text = string.Format("Прошло {0}% перемены | Длительность перемены: {1} мин.",
                    currentProgressPercent, (int)(intervalEnd - intervalStart).TotalMinutes);
            }
            else
            {
                // Время после окончания последнего урока дня
                lblStatusBadge.Text = "Уроки окончены";
                lblCountdownTitle.Text = "ЗАНЯТИЯ НА СЕГОДНЯ ЗАВЕРШЕНЫ";
                lblCountdown.Text = "00:00";
                lblProgressPercent.Text = "Учебный день завершен. Отличного отдыха!";
                currentProgressPercent = 100;
            }

            panelProgressBar.Invalidate();
        }

        /// <summary>
        /// Форматирует оставшееся время в строковый вид "ММ:СС" (или "ЧЧ:ММ:СС", если осталось более 1 часа).
        /// </summary>
        /// <param name="span">Отрезок времени.</param>
        /// <returns>Строка таймера обратного отсчета.</returns>
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
        /// Центрирование карточки часов при изменении размера окна или развороте на весь экран.
        /// </summary>
        private void MainForm_Resize(object sender, EventArgs e)
        {
            CenterClockCard();
        }

        /// <summary>
        /// Математический перерасчет расположения карточки часов строго по центру рабочей области формы.
        /// </summary>
        private void CenterClockCard()
        {
            if (panelCard == null) return;

            int menuHeight = (menuStripMain != null && menuStripMain.Visible) ? menuStripMain.Height : 0;
            int availableWidth = this.ClientSize.Width;
            int availableHeight = this.ClientSize.Height - menuHeight;

            // Адаптивный размер карточки в зависимости от разрешения экрана
            int cardWidth = Math.Min(availableWidth - 40, 780);
            int cardHeight = Math.Min(availableHeight - 40, 480);

            if (cardWidth < 500) cardWidth = Math.Max(320, availableWidth - 10);
            if (cardHeight < 360) cardHeight = Math.Max(260, availableHeight - 10);

            panelCard.Size = new Size(cardWidth, cardHeight);

            // Точное выравнивание по центру
            int posX = Math.Max(0, (availableWidth - cardWidth) / 2);
            int posY = menuHeight + Math.Max(0, (availableHeight - cardHeight) / 2);
            panelCard.Location = new Point(posX, posY);

            // Адаптивная подгонка внутренних элементов карточки по ширине
            int contentWidth = panelCard.Width - 40;
            lblDate.Width = contentWidth;
            lblDate.Left = 20;

            lblCurrentTime.Width = contentWidth;
            lblCurrentTime.Left = 20;

            lblStatusBadge.Width = Math.Min(contentWidth, 700);
            lblStatusBadge.Left = (panelCard.Width - lblStatusBadge.Width) / 2;

            lblCountdownTitle.Width = contentWidth;
            lblCountdownTitle.Left = 20;

            lblCountdown.Width = contentWidth;
            lblCountdown.Left = 20;

            panelProgressBar.Width = Math.Min(contentWidth - 60, 620);
            panelProgressBar.Left = (panelCard.Width - panelProgressBar.Width) / 2;

            lblProgressPercent.Width = contentWidth;
            lblProgressPercent.Left = 20;
        }

        // =============================================================
        // Обработчики команд меню и интерфейса
        // =============================================================

        /// <summary>
        /// Открытие диалогового окна редактирования расписания.
        /// </summary>
        private void MenuSchedule_Click(object sender, EventArgs e)
        {
            using (ScheduleForm form = new ScheduleForm(this.settings))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    // Расписание было обновлено и сохранено — немедленно пересчитываем состояние
                    UpdateClockAndSchedule();
                }
            }
        }

        /// <summary>
        /// Переключение на офисную темную тему.
        /// </summary>
        private void MenuThemeDark_Click(object sender, EventArgs e)
        {
            settings.Theme = ClockThemeType.DarkOffice;
            if (settings.BgMode == BackgroundMode.DefaultTheme)
            {
                settings.BgMode = BackgroundMode.DefaultTheme;
            }
            AppSettings.Save(settings, AppSettings.GetConfigFilePath());
            ApplyThemeAndBackground();
        }

        /// <summary>
        /// Переключение на офисную светлую тему.
        /// </summary>
        private void MenuThemeLight_Click(object sender, EventArgs e)
        {
            settings.Theme = ClockThemeType.LightOffice;
            if (settings.BgMode == BackgroundMode.DefaultTheme)
            {
                settings.BgMode = BackgroundMode.DefaultTheme;
            }
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
        /// Выбор произвольного однотонного цвета фона через стандартный ColorDialog.
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
        /// Выбор пользовательского фонового изображения (.jpg, .png, .bmp).
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
        /// Переключение полноэкранного режима (F11).
        /// </summary>
        private void MenuFullScreen_Click(object sender, EventArgs e)
        {
            ToggleFullScreen();
        }

        /// <summary>
        /// Переключение видимости верхнего меню для максимально чистого экранного отображения.
        /// </summary>
        private void MenuToggleMenu_Click(object sender, EventArgs e)
        {
            ToggleMenuVisibility();
        }

        /// <summary>
        /// Логика разворачивания на полный экран и выхода из него.
        /// </summary>
        private void ToggleFullScreen()
        {
            if (!isFullScreenActive)
            {
                // Запоминаем текущее состояние перед переходом в полный экран
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
                // Возврат в оконный режим
                this.FormBorderStyle = previousBorderStyle;
                this.WindowState = previousWindowState;
                isFullScreenActive = false;
                menuFullScreen.Text = "⛶ Полный экран (F11)";
                ctxFullScreen.Text = "⛶ Во весь экран (F11)";
            }

            CenterClockCard();
        }

        /// <summary>
        /// Переключение видимости верхнего меню.
        /// </summary>
        private void ToggleMenuVisibility()
        {
            menuStripMain.Visible = !menuStripMain.Visible;
            settings.HideToolBar = !menuStripMain.Visible;
            AppSettings.Save(settings, AppSettings.GetConfigFilePath());

            CenterClockCard();

            if (!menuStripMain.Visible)
            {
                // Напоминание пользователю, как вызвать меню обратно
                // (не блокирующее, информативное)
            }
        }

        /// <summary>
        /// Обработка горячих клавиш (F11 — полный экран, Esc — возврат из полного экрана или возврат меню, Alt — меню).
        /// </summary>
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                ToggleFullScreen();
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
        /// Освобождение ресурсов при закрытии формы.
        /// </summary>
        /// <param name="disposing">Указывает, освобождать ли управляемые ресурсы.</param>
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
