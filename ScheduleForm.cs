using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SchoolBellTimer
{
    /// <summary>
    /// Диалоговое окно для визуального просмотра, добавления, изменения и удаления уроков расписания.
    /// </summary>
    public class ScheduleForm : Form
    {
        private DataGridView gridLessons;
        private Button btnAdd;
        private Button btnDelete;
        private Button btnMoveUp;
        private Button btnMoveDown;
        private Button btnResetDefault;
        private Button btnSave;
        private Button btnCancel;
        private Label lblHeaderTitle;
        private Label lblHeaderSubtitle;
        private Panel panelHeader;
        private Panel panelButtons;

        private AppSettings currentSettings;

        /// <summary>
        /// Конструктор окна редактирования расписания.
        /// </summary>
        /// <param name="settings">Текущие настройки приложения с уроками.</param>
        public ScheduleForm(AppSettings settings)
        {
            this.currentSettings = settings;

            // Инициализация графических элементов
            InitializeComponents();

            // Заполнение таблицы текущими уроками
            LoadLessonsToGrid();
        }

        /// <summary>
        /// Программное создание и настройка элементов интерфейса окна.
        /// </summary>
        private void InitializeComponents()
        {
            this.Text = "Редактор расписания звонков и уроков";
            this.Size = new Size(680, 520);
            this.MinimumSize = new Size(580, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(248, 249, 251);

            // ==========================================
            // Верхняя информационная панель заголовка
            // ==========================================
            panelHeader = new Panel();
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Height = 70;
            panelHeader.BackColor = Color.FromArgb(238, 242, 248);
            panelHeader.Padding = new Padding(16, 12, 16, 10);

            lblHeaderTitle = new Label();
            lblHeaderTitle.Text = "Расписание школьных уроков и перемен";
            lblHeaderTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblHeaderTitle.ForeColor = Color.FromArgb(30, 41, 59);
            lblHeaderTitle.AutoSize = true;
            lblHeaderTitle.Location = new Point(14, 12);

            lblHeaderSubtitle = new Label();
            lblHeaderSubtitle.Text = "Укажите название и время в формате ЧЧ:ММ (24 часа). Время окончания должно быть позже начала.";
            lblHeaderSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblHeaderSubtitle.ForeColor = Color.FromArgb(100, 116, 139);
            lblHeaderSubtitle.AutoSize = true;
            lblHeaderSubtitle.Location = new Point(15, 38);

            panelHeader.Controls.Add(lblHeaderTitle);
            panelHeader.Controls.Add(lblHeaderSubtitle);

            // ==========================================
            // Таблица уроков (DataGridView)
            // ==========================================
            gridLessons = new DataGridView();
            gridLessons.Dock = DockStyle.Fill;
            gridLessons.BackgroundColor = Color.White;
            gridLessons.BorderStyle = BorderStyle.None;
            gridLessons.RowHeadersVisible = false;
            gridLessons.AllowUserToAddRows = false;
            gridLessons.AllowUserToDeleteRows = false;
            gridLessons.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridLessons.MultiSelect = false;
            gridLessons.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridLessons.RowTemplate.Height = 32;
            gridLessons.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 252);
            gridLessons.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 236, 255);
            gridLessons.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            gridLessons.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);

            // Настройка стилей колонок
            DataGridViewTextBoxColumn colNumber = new DataGridViewTextBoxColumn();
            colNumber.Name = "ColNumber";
            colNumber.HeaderText = "№";
            colNumber.FillWeight = 15;
            colNumber.ReadOnly = true;
            colNumber.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            DataGridViewTextBoxColumn colName = new DataGridViewTextBoxColumn();
            colName.Name = "ColName";
            colName.HeaderText = "Название урока";
            colName.FillWeight = 45;

            DataGridViewTextBoxColumn colStart = new DataGridViewTextBoxColumn();
            colStart.Name = "ColStart";
            colStart.HeaderText = "Начало (ЧЧ:ММ)";
            colStart.FillWeight = 20;
            colStart.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            DataGridViewTextBoxColumn colEnd = new DataGridViewTextBoxColumn();
            colEnd.Name = "ColEnd";
            colEnd.HeaderText = "Окончание (ЧЧ:ММ)";
            colEnd.FillWeight = 20;
            colEnd.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            gridLessons.Columns.AddRange(new DataGridViewColumn[] { colNumber, colName, colStart, colEnd });

            // ==========================================
            // Нижняя панель действий и кнопок
            // ==========================================
            panelButtons = new Panel();
            panelButtons.Dock = DockStyle.Bottom;
            panelButtons.Height = 100;
            panelButtons.BackColor = Color.FromArgb(243, 245, 249);
            panelButtons.Padding = new Padding(12);

            // Кнопка: Добавить
            btnAdd = CreateButton("➕ Добавить урок", 10, 12, 140, 34);
            btnAdd.Click += new EventHandler(BtnAdd_Click);

            // Кнопка: Удалить
            btnDelete = CreateButton("❌ Удалить", 158, 12, 105, 34);
            btnDelete.Click += new EventHandler(BtnDelete_Click);

            // Кнопка: Вверх
            btnMoveUp = CreateButton("▲ Вверх", 271, 12, 85, 34);
            btnMoveUp.Click += new EventHandler(BtnMoveUp_Click);

            // Кнопка: Вниз
            btnMoveDown = CreateButton("▼ Вниз", 364, 12, 85, 34);
            btnMoveDown.Click += new EventHandler(BtnMoveDown_Click);

            // Кнопка: Сброс в демо-расписание
            btnResetDefault = CreateButton("🔄 По умолчанию", 457, 12, 140, 34);
            btnResetDefault.Click += new EventHandler(BtnResetDefault_Click);

            // Кнопка: Сохранить
            btnSave = CreateButton("✔ Сохранить расписание", 330, 56, 190, 36);
            btnSave.BackColor = Color.FromArgb(24, 119, 242);
            btnSave.ForeColor = Color.White;
            btnSave.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnSave.Click += new EventHandler(BtnSave_Click);

            // Кнопка: Отмена
            btnCancel = CreateButton("Отмена", 530, 56, 110, 36);
            btnCancel.Click += new EventHandler(BtnCancel_Click);

            panelButtons.Controls.Add(btnAdd);
            panelButtons.Controls.Add(btnDelete);
            panelButtons.Controls.Add(btnMoveUp);
            panelButtons.Controls.Add(btnMoveDown);
            panelButtons.Controls.Add(btnResetDefault);
            panelButtons.Controls.Add(btnSave);
            panelButtons.Controls.Add(btnCancel);

            // Добавление контролов на форму
            this.Controls.Add(gridLessons);
            this.Controls.Add(panelButtons);
            this.Controls.Add(panelHeader);

            // Обработка закрытия формы по Esc и сохранения по Enter
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        /// <summary>
        /// Вспомогательный метод создания единообразно стилизованных кнопок.
        /// </summary>
        private Button CreateButton(string text, int x, int y, int width, int height)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(width, height);
            btn.FlatStyle = FlatStyle.System;
            btn.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        /// <summary>
        /// Загружает текущий список уроков в таблицу DataGridView.
        /// </summary>
        private void LoadLessonsToGrid()
        {
            gridLessons.Rows.Clear();
            if (currentSettings.Lessons != null)
            {
                for (int i = 0; i < currentSettings.Lessons.Count; i++)
                {
                    LessonItem item = currentSettings.Lessons[i];
                    gridLessons.Rows.Add(new object[]
                    {
                        (i + 1).ToString(),
                        item.Name,
                        item.StartTime,
                        item.EndTime
                    });
                }
            }
            RenumberRows();
        }

        /// <summary>
        /// Автоматическая перенумерация первой колонки в таблице.
        /// </summary>
        private void RenumberRows()
        {
            for (int i = 0; i < gridLessons.Rows.Count; i++)
            {
                gridLessons.Rows[i].Cells["ColNumber"].Value = (i + 1).ToString();
            }
        }

        /// <summary>
        /// Добавление новой строки урока с предварительно рассчитанным временем.
        /// </summary>
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            int nextNumber = gridLessons.Rows.Count + 1;
            string defaultStart = "11:05";
            string defaultEnd = "11:45";

            // Если есть предыдущий урок, подбираем время через 10-15 минут перемены
            if (gridLessons.Rows.Count > 0)
            {
                DataGridViewRow lastRow = gridLessons.Rows[gridLessons.Rows.Count - 1];
                string lastEndStr = Convert.ToString(lastRow.Cells["ColEnd"].Value);
                TimeSpan lastEnd;
                if (LessonItem.TryParseTimeString(lastEndStr, out lastEnd))
                {
                    TimeSpan newStart = lastEnd.Add(TimeSpan.FromMinutes(10));
                    TimeSpan newEnd = newStart.Add(TimeSpan.FromMinutes(40));
                    if (newEnd.TotalHours < 24)
                    {
                        defaultStart = string.Format("{0:D2}:{1:D2}", newStart.Hours, newStart.Minutes);
                        defaultEnd = string.Format("{0:D2}:{1:D2}", newEnd.Hours, newEnd.Minutes);
                    }
                }
            }

            gridLessons.Rows.Add(new object[]
            {
                nextNumber.ToString(),
                string.Format("{0} урок", nextNumber),
                defaultStart,
                defaultEnd
            });

            RenumberRows();

            // Выделяем добавленную строку
            int newIndex = gridLessons.Rows.Count - 1;
            gridLessons.ClearSelection();
            gridLessons.Rows[newIndex].Selected = true;
            gridLessons.CurrentCell = gridLessons.Rows[newIndex].Cells["ColName"];
        }

        /// <summary>
        /// Удаление выбранного урока из таблицы.
        /// </summary>
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridLessons.SelectedRows.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выделите строку урока для удаления.", "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int index = gridLessons.SelectedRows[0].Index;
            gridLessons.Rows.RemoveAt(index);
            RenumberRows();

            if (gridLessons.Rows.Count > 0)
            {
                int nextIndex = Math.Min(index, gridLessons.Rows.Count - 1);
                gridLessons.Rows[nextIndex].Selected = true;
            }
        }

        /// <summary>
        /// Перемещение выбранного урока выше по списку.
        /// </summary>
        private void BtnMoveUp_Click(object sender, EventArgs e)
        {
            if (gridLessons.SelectedRows.Count == 0) return;
            int index = gridLessons.SelectedRows[0].Index;
            if (index <= 0) return;

            SwapRows(index, index - 1);
            gridLessons.ClearSelection();
            gridLessons.Rows[index - 1].Selected = true;
        }

        /// <summary>
        /// Перемещение выбранного урока ниже по списку.
        /// </summary>
        private void BtnMoveDown_Click(object sender, EventArgs e)
        {
            if (gridLessons.SelectedRows.Count == 0) return;
            int index = gridLessons.SelectedRows[0].Index;
            if (index >= gridLessons.Rows.Count - 1) return;

            SwapRows(index, index + 1);
            gridLessons.ClearSelection();
            gridLessons.Rows[index + 1].Selected = true;
        }

        /// <summary>
        /// Обмен значениями между двумя строками таблицы.
        /// </summary>
        private void SwapRows(int indexA, int indexB)
        {
            DataGridViewRow rowA = gridLessons.Rows[indexA];
            DataGridViewRow rowB = gridLessons.Rows[indexB];

            object nameA = rowA.Cells["ColName"].Value;
            object startA = rowA.Cells["ColStart"].Value;
            object endA = rowA.Cells["ColEnd"].Value;

            rowA.Cells["ColName"].Value = rowB.Cells["ColName"].Value;
            rowA.Cells["ColStart"].Value = rowB.Cells["ColStart"].Value;
            rowA.Cells["ColEnd"].Value = rowB.Cells["ColEnd"].Value;

            rowB.Cells["ColName"].Value = nameA;
            rowB.Cells["ColStart"].Value = startA;
            rowB.Cells["ColEnd"].Value = endA;

            RenumberRows();
        }

        /// <summary>
        /// Сброс расписания к стандартному демонстрационному набору из 3 уроков.
        /// </summary>
        private void BtnResetDefault_Click(object sender, EventArgs e)
        {
            DialogResult confirm = MessageBox.Show(
                "Сбросить текущее расписание к демонстрационному варианту по умолчанию?\n(1 урок: 08:30-09:10, 2 урок: 09:25-10:05, 3 урок: 10:15-10:55)",
                "Подтверждение сброса",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                currentSettings.Lessons.Clear();
                currentSettings.Lessons.Add(new LessonItem(1, "1 урок", "08:30", "09:10"));
                currentSettings.Lessons.Add(new LessonItem(2, "2 урок", "09:25", "10:05"));
                currentSettings.Lessons.Add(new LessonItem(3, "3 урок", "10:15", "10:55"));
                LoadLessonsToGrid();
            }
        }

        /// <summary>
        /// Валидация введенных данных, сохранение расписания в настройки и файл.
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Завершаем редактирование текущей ячейки
            gridLessons.EndEdit();

            List<LessonItem> validatedList = new List<LessonItem>();

            for (int i = 0; i < gridLessons.Rows.Count; i++)
            {
                DataGridViewRow row = gridLessons.Rows[i];
                string name = Convert.ToString(row.Cells["ColName"].Value);
                string startStr = Convert.ToString(row.Cells["ColStart"].Value);
                string endStr = Convert.ToString(row.Cells["ColEnd"].Value);

                if (string.IsNullOrEmpty(name))
                {
                    name = string.Format("{0} урок", i + 1);
                }

                TimeSpan start;
                if (!LessonItem.TryParseTimeString(startStr, out start))
                {
                    MessageBox.Show(
                        string.Format("Строка {0}: Некорректный формат времени начала '{1}'. Используйте формат ЧЧ:ММ (например, 08:30).", i + 1, startStr),
                        "Ошибка валидации времени",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    gridLessons.ClearSelection();
                    row.Cells["ColStart"].Selected = true;
                    return;
                }

                TimeSpan end;
                if (!LessonItem.TryParseTimeString(endStr, out end))
                {
                    MessageBox.Show(
                        string.Format("Строка {0}: Некорректный формат времени окончания '{1}'. Используйте формат ЧЧ:ММ (например, 09:10).", i + 1, endStr),
                        "Ошибка валидации времени",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    gridLessons.ClearSelection();
                    row.Cells["ColEnd"].Selected = true;
                    return;
                }

                if (start >= end)
                {
                    MessageBox.Show(
                        string.Format("Строка {0}: Время окончания ({1}) должно быть строго позже времени начала ({2}).", i + 1, endStr, startStr),
                        "Логическая ошибка расписания",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    gridLessons.ClearSelection();
                    row.Cells["ColEnd"].Selected = true;
                    return;
                }

                // Стандартизируем строковое представление в "ЧЧ:ММ"
                string formattedStart = string.Format("{0:D2}:{1:D2}", start.Hours, start.Minutes);
                string formattedEnd = string.Format("{0:D2}:{1:D2}", end.Hours, end.Minutes);

                validatedList.Add(new LessonItem(i + 1, name.Trim(), formattedStart, formattedEnd));
            }

            // Автоматическая сортировка уроков по времени начала
            validatedList.Sort(delegate(LessonItem a, LessonItem b)
            {
                TimeSpan sa, ea, sb, eb;
                a.TryGetTimeSpans(out sa, out ea);
                b.TryGetTimeSpans(out sb, out eb);
                return sa.CompareTo(sb);
            });

            // Проверка на пересечение интервалов уроков
            for (int i = 0; i < validatedList.Count - 1; i++)
            {
                TimeSpan s1, e1, s2, e2;
                validatedList[i].TryGetTimeSpans(out s1, out e1);
                validatedList[i + 1].TryGetTimeSpans(out s2, out e2);

                if (s2 < e1)
                {
                    string msg = string.Format(
                        "Обнаружено пересечение уроков:\n'{0}' ({1} - {2})\nи '{3}' ({4} - {5}).\n\nСледующий урок не может начинаться до окончания предыдущего.",
                        validatedList[i].Name, validatedList[i].StartTime, validatedList[i].EndTime,
                        validatedList[i + 1].Name, validatedList[i + 1].StartTime, validatedList[i + 1].EndTime);

                    MessageBox.Show(msg, "Конфликт времени уроков", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Перенумеровываем после сортировки
            for (int i = 0; i < validatedList.Count; i++)
            {
                validatedList[i].Number = i + 1;
            }

            // Применяем сохраненный список
            currentSettings.Lessons = validatedList;
            AppSettings.Save(currentSettings, AppSettings.GetConfigFilePath());

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Закрытие окна без сохранения изменений.
        /// </summary>
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
