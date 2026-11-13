using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SchoolBellTimer
{
    /// <summary>
    /// Перечисление встроенных тем оформления электронных часов.
    /// </summary>
    public enum ClockThemeType
    {
        /// <summary>
        /// Офисная темная тема: темно-графитовый фон, бирюзовые/светло-зеленые электронные цифры.
        /// </summary>
        DarkOffice = 0,

        /// <summary>
        /// Офисная светлая тема: светло-серый фон, строгие темно-синие и графитовые ЖК-цифры.
        /// </summary>
        LightOffice = 1
    }

    /// <summary>
    /// Режим отображения заднего фона главного окна.
    /// </summary>
    public enum BackgroundMode
    {
        /// <summary>
        /// Фоновый цвет определяется выбранной темой часов.
        /// </summary>
        DefaultTheme = 0,

        /// <summary>
        /// Однотонный пользовательский цвет.
        /// </summary>
        SolidColor = 1,

        /// <summary>
        /// Фоновое изображение (картинка).
        /// </summary>
        Image = 2
    }

    /// <summary>
    /// Модель отдельного урока в школьном расписании.
    /// </summary>
    [Serializable]
    public class LessonItem
    {
        /// <summary>
        /// Порядковый номер урока (1, 2, 3...).
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Название урока или пояснение (например, "1 урок" или "Математика").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Время начала урока в формате "ЧЧ:ММ" (24-часовой формат).
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// Время окончания урока в формате "ЧЧ:ММ" (24-часовой формат).
        /// </summary>
        public string EndTime { get; set; }

        /// <summary>
        /// Конструктор по умолчанию (необходим для XML-сериализации).
        /// </summary>
        public LessonItem()
        {
            Number = 1;
            Name = "1 урок";
            StartTime = "08:30";
            EndTime = "09:10";
        }

        /// <summary>
        /// Параметризованный конструктор для быстрого создания урока.
        /// </summary>
        /// <param name="number">Номер урока.</param>
        /// <param name="name">Название урока.</param>
        /// <param name="startTime">Время начала в формате ЧЧ:ММ.</param>
        /// <param name="endTime">Время окончания в формате ЧЧ:ММ.</param>
        public LessonItem(int number, string name, string startTime, string endTime)
        {
            Number = number;
            Name = name;
            StartTime = startTime;
            EndTime = endTime;
        }

        /// <summary>
        /// Пытается распарсить строки времени в объекты TimeSpan.
        /// </summary>
        /// <param name="start">Выходное время начала.</param>
        /// <param name="end">Выходное время окончания.</param>
        /// <returns>True, если оба значения корректны, иначе False.</returns>
        public bool TryGetTimeSpans(out TimeSpan start, out TimeSpan end)
        {
            bool isStartValid = TryParseTimeString(StartTime, out start);
            bool isEndValid = TryParseTimeString(EndTime, out end);
            return isStartValid && isEndValid;
        }

        /// <summary>
        /// Разбор строки вида "Ч:М", "ЧЧ:ММ", "ЧЧ:ММ:СС" в структуру TimeSpan.
        /// </summary>
        /// <param name="value">Входная строка.</param>
        /// <param name="result">Результат разбора.</param>
        /// <returns>Успешность операции.</returns>
        public static bool TryParseTimeString(string value, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string[] parts = value.Trim().Split(':', '-', '.');
            if (parts.Length < 2)
            {
                return false;
            }

            int hours;
            int minutes;
            if (!int.TryParse(parts[0], out hours) || !int.TryParse(parts[1], out minutes))
            {
                return false;
            }

            if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59)
            {
                return false;
            }

            int seconds = 0;
            if (parts.Length >= 3)
            {
                int.TryParse(parts[2], out seconds);
                if (seconds < 0 || seconds > 59)
                {
                    seconds = 0;
                }
            }

            result = new TimeSpan(hours, minutes, seconds);
            return true;
        }

        /// <summary>
        /// Клонирование объекта урока для безопасного редактирования.
        /// </summary>
        /// <returns>Новая копия урока.</returns>
        public LessonItem Clone()
        {
            return new LessonItem(this.Number, this.Name, this.StartTime, this.EndTime);
        }
    }

    /// <summary>
    /// Конфигурация цветовой палитры темы оформления.
    /// </summary>
    public class ThemePalette
    {
        /// <summary>
        /// Цвет фона окна по умолчанию для темы.
        /// </summary>
        public Color WindowBackground { get; set; }

        /// <summary>
        /// Цвет фона центральной информационной панели-карточки.
        /// </summary>
        public Color CardBackground { get; set; }

        /// <summary>
        /// Цвет границы центральной карточки.
        /// </summary>
        public Color CardBorder { get; set; }

        /// <summary>
        /// Цвет текущего времени (ЧЧ:ММ:СС).
        /// </summary>
        public Color ClockDigits { get; set; }

        /// <summary>
        /// Цвет крупного таймера обратного отсчета (ММ:СС).
        /// </summary>
        public Color CountdownDigits { get; set; }

        /// <summary>
        /// Цвет текста статуса (например, "Идет 1 урок").
        /// </summary>
        public Color StatusText { get; set; }

        /// <summary>
        /// Фоновый цвет бейджа статуса.
        /// </summary>
        public Color StatusBadgeBackground { get; set; }

        /// <summary>
        /// Цвет второстепенных подписей и даты.
        /// </summary>
        public Color SubtitleText { get; set; }

        /// <summary>
        /// Цвет фона верхней панели инструментов.
        /// </summary>
        public Color ToolbarBackground { get; set; }

        /// <summary>
        /// Цвет текста элементов панели инструментов.
        /// </summary>
        public Color ToolbarForeground { get; set; }

        /// <summary>
        /// Цвет фона карточки списка уроков.
        /// </summary>
        public Color ScheduleCardBackground { get; set; }

        /// <summary>
        /// Цвет рамки карточки списка уроков.
        /// </summary>
        public Color ScheduleCardBorder { get; set; }

        /// <summary>
        /// Цвет фона активного урока в списке.
        /// </summary>
        public Color ScheduleActiveItemBackground { get; set; }

        /// <summary>
        /// Цвет рамки активного урока в списке.
        /// </summary>
        public Color ScheduleActiveItemBorder { get; set; }

        /// <summary>
        /// Цвет левой акцентной полосы активного урока.
        /// </summary>
        public Color ScheduleActiveItemAccent { get; set; }

        /// <summary>
        /// Цвет текста активного урока.
        /// </summary>
        public Color ScheduleActiveItemText { get; set; }

        /// <summary>
        /// Обычный цвет фона строки урока в списке.
        /// </summary>
        public Color ScheduleItemBackground { get; set; }

        /// <summary>
        /// Обычный цвет рамки строки урока в списке.
        /// </summary>
        public Color ScheduleItemBorder { get; set; }

        /// <summary>
        /// Цвет текста обычного урока.
        /// </summary>
        public Color ScheduleItemText { get; set; }

        /// <summary>
        /// Цвет текста завершенного (прошедшего) урока.
        /// </summary>
        public Color ScheduleItemPastText { get; set; }

        /// <summary>
        /// Цвет интервала времени урока (ЧЧ:ММ – ЧЧ:ММ).
        /// </summary>
        public Color ScheduleItemTime { get; set; }

        /// <summary>
        /// Цвет фона плашки с номером урока.
        /// </summary>
        public Color ScheduleNumberBadgeBackground { get; set; }

        /// <summary>
        /// Цвет цифры номера урока.
        /// </summary>
        public Color ScheduleNumberBadgeText { get; set; }

        /// <summary>
        /// Фабричный метод получения палитры для темной офисной темы.
        /// </summary>
        /// <returns>Палитра темной темы.</returns>
        public static ThemePalette GetDarkOfficePalette()
        {
            ThemePalette p = new ThemePalette();
            p.WindowBackground = Color.FromArgb(20, 22, 28);           // Глубокий темный графит
            p.CardBackground = Color.FromArgb(30, 34, 44);             // Темно-графитовая карточка
            p.CardBorder = Color.FromArgb(50, 56, 72);                 // Мягкая контрастная окантовка
            p.ClockDigits = Color.FromArgb(245, 247, 250);             // Чистый яркий белый
            p.CountdownDigits = Color.FromArgb(0, 230, 153);          // Неоновый мятно-зеленый (отличная видимость)
            p.StatusText = Color.FromArgb(0, 212, 255);               // Яркий бирюзовый
            p.StatusBadgeBackground = Color.FromArgb(16, 42, 60);      // Полупрозрачный темно-бирюзовый
            p.SubtitleText = Color.FromArgb(150, 160, 180);           // Спокойный серый
            p.ToolbarBackground = Color.FromArgb(26, 29, 38);          // Верхняя панель
            p.ToolbarForeground = Color.FromArgb(220, 225, 235);       // Текст меню

            // Цвета панели расписания (темная тема)
            p.ScheduleCardBackground = Color.FromArgb(30, 34, 44);
            p.ScheduleCardBorder = Color.FromArgb(50, 56, 72);
            p.ScheduleActiveItemBackground = Color.FromArgb(18, 46, 62);
            p.ScheduleActiveItemBorder = Color.FromArgb(0, 212, 255);
            p.ScheduleActiveItemAccent = Color.FromArgb(0, 230, 153);
            p.ScheduleActiveItemText = Color.FromArgb(245, 247, 250);
            p.ScheduleItemBackground = Color.FromArgb(24, 27, 35);
            p.ScheduleItemBorder = Color.FromArgb(42, 47, 60);
            p.ScheduleItemText = Color.FromArgb(220, 225, 235);
            p.ScheduleItemPastText = Color.FromArgb(110, 120, 140);
            p.ScheduleItemTime = Color.FromArgb(0, 212, 255);
            p.ScheduleNumberBadgeBackground = Color.FromArgb(40, 46, 58);
            p.ScheduleNumberBadgeText = Color.FromArgb(200, 210, 230);
            return p;
        }

        /// <summary>
        /// Фабричный метод получения палитры для светлой офисной темы.
        /// </summary>
        /// <returns>Палитра светлой темы.</returns>
        public static ThemePalette GetLightOfficePalette()
        {
            ThemePalette p = new ThemePalette();
            p.WindowBackground = Color.FromArgb(238, 241, 246);         // Светло-серый офисный фон
            p.CardBackground = Color.FromArgb(255, 255, 255);          // Чистый белый фон карточки
            p.CardBorder = Color.FromArgb(205, 212, 224);              // Светлая окантовка карточки
            p.ClockDigits = Color.FromArgb(30, 41, 59);                // Темно-графитовый цвет
            p.CountdownDigits = Color.FromArgb(15, 60, 150);           // Строгий темно-синий цвет ЖК-индикатора
            p.StatusText = Color.FromArgb(20, 80, 190);               // Насыщенный синий цвет
            p.StatusBadgeBackground = Color.FromArgb(232, 240, 254);   // Нежно-голубой бейдж
            p.SubtitleText = Color.FromArgb(100, 116, 139);           // Сбалансированный серый цвет
            p.ToolbarBackground = Color.FromArgb(248, 249, 251);       // Светлая панель инструментов
            p.ToolbarForeground = Color.FromArgb(40, 50, 70);          // Текст меню

            // Цвета панели расписания (светлая тема)
            p.ScheduleCardBackground = Color.FromArgb(255, 255, 255);
            p.ScheduleCardBorder = Color.FromArgb(205, 212, 224);
            p.ScheduleActiveItemBackground = Color.FromArgb(234, 242, 255);
            p.ScheduleActiveItemBorder = Color.FromArgb(59, 130, 246);
            p.ScheduleActiveItemAccent = Color.FromArgb(37, 99, 235);
            p.ScheduleActiveItemText = Color.FromArgb(15, 23, 42);
            p.ScheduleItemBackground = Color.FromArgb(248, 250, 252);
            p.ScheduleItemBorder = Color.FromArgb(226, 232, 240);
            p.ScheduleItemText = Color.FromArgb(30, 41, 59);
            p.ScheduleItemPastText = Color.FromArgb(148, 163, 184);
            p.ScheduleItemTime = Color.FromArgb(20, 80, 190);
            p.ScheduleNumberBadgeBackground = Color.FromArgb(224, 231, 255);
            p.ScheduleNumberBadgeText = Color.FromArgb(30, 58, 138);
            return p;
        }
    }

    /// <summary>
    /// Главный класс настроек приложения, сохраняемый в локальный XML-файл.
    /// </summary>
    [Serializable]
    public class AppSettings
    {
        /// <summary>
        /// Выбранная тема оформления электронных часов.
        /// </summary>
        public ClockThemeType Theme { get; set; }

        /// <summary>
        /// Режим фонового оформления окна.
        /// </summary>
        public BackgroundMode BgMode { get; set; }

        /// <summary>
        /// Пользовательский цвет фона в формате ARGB.
        /// </summary>
        public int CustomBgColorArgb { get; set; }

        /// <summary>
        /// Полный или относительный путь к файлу фонового изображения.
        /// </summary>
        public string BackgroundImagePath { get; set; }

        /// <summary>
        /// Режим масштабирования фоновой картинки (Stretch, Zoom, Tile и т.д.).
        /// </summary>
        public ImageLayout BackgroundImageLayout { get; set; }

        /// <summary>
        /// Флаг полноэкранного режима при запуске.
        /// </summary>
        public bool IsFullScreen { get; set; }

        /// <summary>
        /// Флаг скрытия верхней панели инструментов (только часы на экране).
        /// </summary>
        public bool HideToolBar { get; set; }

        /// <summary>
        /// Флаг отображения боковой панели с расписанием уроков на главном экране.
        /// </summary>
        public bool ShowScheduleList { get; set; }

        /// <summary>
        /// Флаг принудительного удержания окна поверх всех остальных окон системы (Always on Top).
        /// Гарантирует, что часы не будут перекрыты другими окнами или всплывающими уведомлениями.
        /// </summary>
        public bool AlwaysOnTop { get; set; }

        /// <summary>
        /// Список уроков школьного расписания.
        /// </summary>
        public List<LessonItem> Lessons { get; set; }

        /// <summary>
        /// Конструктор по умолчанию с начальной инициализацией.
        /// </summary>
        public AppSettings()
        {
            Theme = ClockThemeType.DarkOffice;
            BgMode = BackgroundMode.DefaultTheme;
            CustomBgColorArgb = Color.FromArgb(20, 22, 28).ToArgb();
            BackgroundImagePath = string.Empty;
            BackgroundImageLayout = ImageLayout.Stretch;
            IsFullScreen = false;
            HideToolBar = false;
            ShowScheduleList = true;
            AlwaysOnTop = true;
            Lessons = new List<LessonItem>();
        }

        /// <summary>
        /// Создает экземпляр настроек с демонстрационным расписанием по умолчанию.
        /// </summary>
        /// <returns>Объект настроек по умолчанию.</returns>
        public static AppSettings CreateDefault()
        {
            AppSettings settings = new AppSettings();
            settings.Theme = ClockThemeType.DarkOffice;
            settings.BgMode = BackgroundMode.DefaultTheme;

            // Демонстрационное расписание согласно техническому заданию:
            // 1 урок: 08:30 - 09:10
            // 2 урок: 09:25 - 10:05
            // 3 урок: 10:15 - 10:55
            settings.Lessons.Add(new LessonItem(1, "1 урок", "08:30", "09:10"));
            settings.Lessons.Add(new LessonItem(2, "2 урок", "09:25", "10:05"));
            settings.Lessons.Add(new LessonItem(3, "3 урок", "10:15", "10:55"));

            return settings;
        }

        /// <summary>
        /// Возвращает палитру цветов для текущей выбранной темы.
        /// </summary>
        /// <returns>Объект палитры цветов.</returns>
        public ThemePalette GetCurrentPalette()
        {
            if (this.Theme == ClockThemeType.LightOffice)
            {
                return ThemePalette.GetLightOfficePalette();
            }
            return ThemePalette.GetDarkOfficePalette();
        }

        /// <summary>
        /// Стандартное имя файла конфигурации рядом с исполняемым файлом.
        /// </summary>
        public const string DefaultFileName = "settings.xml";

        /// <summary>
        /// Получает абсолютный путь к файлу конфигурации рядом с .exe.
        /// </summary>
        /// <returns>Полный путь к settings.xml.</returns>
        public static string GetConfigFilePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, DefaultFileName);
        }

        /// <summary>
        /// Загружает настройки из XML-файла. Если файл отсутствует, создает его с демо-расписанием.
        /// </summary>
        /// <param name="filePath">Путь к файлу настроек. Если null, используется стандартный путь.</param>
        /// <returns>Загруженный или созданный по умолчанию объект настроек.</returns>
        public static AppSettings Load(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = GetConfigFilePath();
            }

            // Если файл не найден - создаем демо-конфигурацию и сразу сохраняем на диск
            if (!File.Exists(filePath))
            {
                AppSettings defaultSettings = CreateDefault();
                Save(defaultSettings, filePath);
                return defaultSettings;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    AppSettings loaded = (AppSettings)serializer.Deserialize(stream);
                    if (loaded.Lessons == null)
                    {
                        loaded.Lessons = new List<LessonItem>();
                    }
                    return loaded;
                }
            }
            catch (Exception)
            {
                // При ошибке повреждения файла создаем конфигурацию по умолчанию,
                // чтобы приложение гарантированно запустилось
                AppSettings fallback = CreateDefault();
                return fallback;
            }
        }

        /// <summary>
        /// Сохраняет настройки в локальный XML-файл.
        /// </summary>
        /// <param name="settings">Объект настроек для сохранения.</param>
        /// <param name="filePath">Путь к файлу. Если null, используется стандартный путь.</param>
        /// <returns>True при успехе, иначе False.</returns>
        public static bool Save(AppSettings settings, string filePath)
        {
            if (settings == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                filePath = GetConfigFilePath();
            }

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                using (StreamWriter writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    serializer.Serialize(writer, settings);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
