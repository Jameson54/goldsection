using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace goldsection
{
  public partial class Form1 : Form
  {
    private TextBox textBoxFunction;
    private TextBox textBoxA;
    private TextBox textBoxB;
    private TextBox textBoxEpsilon;
    private Label labelResult;
    private PictureBox pictureBoxGraph;
    private MenuStrip menuStripMain;

    public Form1()
    {
      BuildInterface();
    }

    private void BuildInterface()
    {
      this.Text = "Лабораторная работа №1 — Метод золотого сечения";
      this.Width = 1000;
      this.Height = 700;
      this.StartPosition = FormStartPosition.CenterScreen;

      menuStripMain = new MenuStrip();

      var menuActions = new ToolStripMenuItem("Действия");
      var menuCalculate = new ToolStripMenuItem("Рассчитать", null, OnCalculateClick);
      var menuClear = new ToolStripMenuItem("Очистить", null, OnClearClick);
      var menuExit = new ToolStripMenuItem("Выход", null, (s, e) => this.Close());

      menuActions.DropDownItems.Add(menuCalculate);
      menuActions.DropDownItems.Add(menuClear);
      menuActions.DropDownItems.Add(new ToolStripSeparator());
      menuActions.DropDownItems.Add(menuExit);

      menuStripMain.Items.Add(menuActions);
      this.MainMenuStrip = menuStripMain;
      this.Controls.Add(menuStripMain);

      var inputPanel = new TableLayoutPanel
      {
        Location = new Point(10, 30),
        Size = new Size(960, 170),
        ColumnCount = 2,
        RowCount = 5,
        AutoSize = true
      };
      inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
      inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

      inputPanel.Controls.Add(new Label { Text = "Функция f(x):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
      textBoxFunction = new TextBox { Text = "x*x - 4*x + 4", Width = 700, Anchor = AnchorStyles.Left | AnchorStyles.Right };
      inputPanel.Controls.Add(textBoxFunction, 1, 0);

      inputPanel.Controls.Add(new Label { Text = "Левая граница a:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
      textBoxA = new TextBox { Text = "0", Width = 200, Anchor = AnchorStyles.Left };
      inputPanel.Controls.Add(textBoxA, 1, 1);

      inputPanel.Controls.Add(new Label { Text = "Правая граница b:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
      textBoxB = new TextBox { Text = "5", Width = 200, Anchor = AnchorStyles.Left };
      inputPanel.Controls.Add(textBoxB, 1, 2);

      inputPanel.Controls.Add(new Label { Text = "Точность ε:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
      textBoxEpsilon = new TextBox { Text = "0,0001", Width = 200, Anchor = AnchorStyles.Left };
      inputPanel.Controls.Add(textBoxEpsilon, 1, 3);

      inputPanel.Controls.Add(new Label { Text = "Результат:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);
      labelResult = new Label { Text = "—", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
      inputPanel.Controls.Add(labelResult, 1, 4);

      this.Controls.Add(inputPanel);

      pictureBoxGraph = new PictureBox
      {
        Location = new Point(10, 210),
        Size = new Size(960, 430),
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Color.White,
        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
      };
      this.Controls.Add(pictureBoxGraph);
    }

    private static bool TryParseDouble(string text, out double value)
    {
      value = 0;
      if (string.IsNullOrWhiteSpace(text)) return false;

      text = text.Trim().Replace(',', '.');
      return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
             && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private double EvaluateFunction(string expression, double x)
    {
      string prepared = expression
          .Replace(" ", "")
          .Replace("X", "x")
          .Replace("x", "(" + x.ToString("R", CultureInfo.InvariantCulture) + ")");

      int pos = 0;
      double result = ParseExpression(prepared, ref pos);
      SkipSpaces(prepared, ref pos);
      if (pos != prepared.Length)
        throw new FormatException($"Не удалось разобрать формулу около позиции {pos}: '{prepared.Substring(pos)}'");
      return result;
    }

    private static void SkipSpaces(string s, ref int pos)
    {
      while (pos < s.Length && s[pos] == ' ') pos++;
    }

    private static double ParseExpression(string s, ref int pos)
    {
      double value = ParseTerm(s, ref pos);
      while (true)
      {
        SkipSpaces(s, ref pos);
        if (pos < s.Length && (s[pos] == '+' || s[pos] == '-'))
        {
          char op = s[pos++];
          double right = ParseTerm(s, ref pos);
          value = op == '+' ? value + right : value - right;
        }
        else break;
      }
      return value;
    }

    private static double ParseTerm(string s, ref int pos)
    {
      double value = ParseFactor(s, ref pos);
      while (true)
      {
        SkipSpaces(s, ref pos);
        if (pos < s.Length && (s[pos] == '*' || s[pos] == '/'))
        {
          char op = s[pos++];
          double right = ParseFactor(s, ref pos);
          if (op == '/' && Math.Abs(right) < 1e-15)
            throw new DivideByZeroException("Деление на ноль в формуле.");
          value = op == '*' ? value * right : value / right;
        }
        else break;
      }
      return value;
    }

    private static double ParseFactor(string s, ref int pos)
    {
      double value = ParseUnary(s, ref pos);
      SkipSpaces(s, ref pos);
      if (pos < s.Length && s[pos] == '^')
      {
        pos++;
        double exp = ParseFactor(s, ref pos);
        value = Math.Pow(value, exp);
      }
      return value;
    }

    private static double ParseUnary(string s, ref int pos)
    {
      SkipSpaces(s, ref pos);
      if (pos < s.Length && s[pos] == '-')
      {
        pos++;
        return -ParseUnary(s, ref pos);
      }
      if (pos < s.Length && s[pos] == '+')
      {
        pos++;
        return ParseUnary(s, ref pos);
      }
      return ParsePrimary(s, ref pos);
    }

    private static double ParsePrimary(string s, ref int pos)
    {
      SkipSpaces(s, ref pos);
      if (pos >= s.Length) throw new FormatException("Неожиданный конец формулы.");

      if (s[pos] == '(')
      {
        pos++;
        double value = ParseExpression(s, ref pos);
        SkipSpaces(s, ref pos);
        if (pos >= s.Length || s[pos] != ')')
          throw new FormatException("Отсутствует закрывающая скобка.");
        pos++;
        return value;
      }

      if (char.IsDigit(s[pos]) || s[pos] == '.')
      {
        int start = pos;
        while (pos < s.Length && (char.IsDigit(s[pos]) || s[pos] == '.')) pos++;
        string num = s.Substring(start, pos - start);
        if (!double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
          throw new FormatException($"Некорректное число: '{num}'");
        return value;
      }

      if (char.IsLetter(s[pos]))
      {
        int start = pos;
        while (pos < s.Length && char.IsLetter(s[pos])) pos++;
        string name = s.Substring(start, pos - start).ToLowerInvariant();

        SkipSpaces(s, ref pos);
        if (pos >= s.Length || s[pos] != '(')
          throw new FormatException($"Ожидалась '(' после функции '{name}'.");
        pos++;
        double arg = ParseExpression(s, ref pos);
        SkipSpaces(s, ref pos);
        if (pos >= s.Length || s[pos] != ')')
          throw new FormatException($"Отсутствует ')' после аргумента функции '{name}'.");
        pos++;

        switch (name)
        {
          case "sin": return Math.Sin(arg);
          case "cos": return Math.Cos(arg);
          case "tan": return Math.Tan(arg);
          case "exp": return Math.Exp(arg);
          case "log": return Math.Log(arg);
          case "sqrt": return Math.Sqrt(arg);
          case "abs": return Math.Abs(arg);
          default: throw new FormatException($"Неизвестная функция: '{name}'.");
        }
      }

      throw new FormatException($"Неожиданный символ: '{s[pos]}'.");
    }

    private double GoldenSectionMinimize(Func<double, double> func, double a, double b, double epsilon, out double fxMin)
    {
      const double phi = 0.6180339887498949;

      double x1 = b - phi * (b - a);
      double x2 = a + phi * (b - a);
      double f1 = func(x1);
      double f2 = func(x2);

      while (Math.Abs(b - a) > epsilon)
      {
        if (f1 < f2)
        {
          b = x2;
          x2 = x1;
          f2 = f1;
          x1 = b - phi * (b - a);
          f1 = func(x1);
        }
        else
        {
          a = x1;
          x1 = x2;
          f1 = f2;
          x2 = a + phi * (b - a);
          f2 = func(x2);
        }
      }

      double xmin = (a + b) / 2.0;
      fxMin = func(xmin);
      return xmin;
    }

    private void OnCalculateClick(object sender, EventArgs e)
    {
      labelResult.Text = "—";
      if (pictureBoxGraph.Image != null)
      {
        pictureBoxGraph.Image.Dispose();
        pictureBoxGraph.Image = null;
      }

      if (!TryParseDouble(textBoxA.Text, out double a))
      {
        MessageBox.Show("Некорректное значение левой границы a. Введите вещественное число.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }
      if (!TryParseDouble(textBoxB.Text, out double b))
      {
        MessageBox.Show("Некорректное значение правой границы b. Введите вещественное число.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }
      if (!TryParseDouble(textBoxEpsilon.Text, out double epsilon) || epsilon <= 0)
      {
        MessageBox.Show("Точность ε должна быть положительным вещественным числом.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }
      if (a >= b)
      {
        MessageBox.Show("Левая граница a должна быть меньше правой границы b.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      string formula = textBoxFunction.Text?.Trim();
      if (string.IsNullOrWhiteSpace(formula))
      {
        MessageBox.Show("Введите формулу функции f(x).", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      Func<double, double> func = x => EvaluateFunction(formula, x);
      try
      {
        func(a);
        func(b);
        func((a + b) / 2.0);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка в формуле f(x): " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }

      double xmin, fxmin;
      try
      {
        xmin = GoldenSectionMinimize(func, a, b, epsilon, out fxmin);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка при вычислении: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
      }

      labelResult.Text = string.Format(CultureInfo.InvariantCulture,
          "Минимум: x* = {0:F6}, f(x*) = {1:F6}, ε = {2}", xmin, fxmin, epsilon);

      try
      {
        DrawGraph(func, a, b, xmin, fxmin);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Не удалось построить график: " + ex.Message, "График", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
    }

    private void DrawGraph(Func<double, double> func, double a, double b, double xmin, double fxmin)
    {
      int width = pictureBoxGraph.Width;
      int height = pictureBoxGraph.Height;
      if (width <= 10 || height <= 10) return;

      var bmp = new Bitmap(width, height);
      using (var g = Graphics.FromImage(bmp))
      {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.White);

        const int margin = 40;
        int plotWidth = width - 2 * margin;
        int plotHeight = height - 2 * margin;

        int samples = Math.Max(200, plotWidth);
        double yMin = double.MaxValue, yMax = double.MinValue;
        var points = new PointF[samples + 1];

        for (int i = 0; i <= samples; i++)
        {
          double x = a + (b - a) * i / samples;
          double y = func(x);
          if (double.IsNaN(y) || double.IsInfinity(y)) y = 0;
          if (y < yMin) yMin = y;
          if (y > yMax) yMax = y;
        }

        if (Math.Abs(yMax - yMin) < 1e-12)
        {
          yMax += 1;
          yMin -= 1;
        }

        double yPad = (yMax - yMin) * 0.1;
        yMax += yPad;
        yMin -= yPad;

        Func<double, float> mapX = x => (float)(margin + (x - a) / (b - a) * plotWidth);
        Func<double, float> mapY = y => (float)(margin + (yMax - y) / (yMax - yMin) * plotHeight);

        using (var axisPen = new Pen(Color.Gray, 1))
        {
          if (yMin <= 0 && yMax >= 0)
          {
            float y0 = mapY(0);
            g.DrawLine(axisPen, margin, y0, margin + plotWidth, y0);
          }
          if (a <= 0 && b >= 0)
          {
            float x0 = mapX(0);
            g.DrawLine(axisPen, x0, margin, x0, margin + plotHeight);
          }

          g.DrawRectangle(axisPen, margin, margin, plotWidth, plotHeight);
        }

        using (var font = new Font("Segoe UI", 8))
        using (var brush = new SolidBrush(Color.Black))
        {
          g.DrawString($"a = {a:F3}", font, brush, margin, height - margin + 5);
          g.DrawString($"b = {b:F3}", font, brush, width - margin - 60, height - margin + 5);
          g.DrawString($"yMin = {yMin:F3}", font, brush, 2, margin);
          g.DrawString($"yMax = {yMax:F3}", font, brush, 2, margin - 15);
        }

        for (int i = 0; i <= samples; i++)
        {
          double x = a + (b - a) * i / samples;
          double y = func(x);
          if (double.IsNaN(y) || double.IsInfinity(y)) y = 0;
          points[i] = new PointF(mapX(x), mapY(y));
        }
        using (var curvePen = new Pen(Color.SteelBlue, 2))
        {
          g.DrawLines(curvePen, points);
        }

        float px = mapX(xmin);
        float py = mapY(fxmin);
        using (var pointBrush = new SolidBrush(Color.Red))
        {
          g.FillEllipse(pointBrush, px - 5, py - 5, 10, 10);
        }
        using (var textBrush = new SolidBrush(Color.DarkRed))
        using (var font = new Font("Segoe UI", 9, FontStyle.Bold))
        {
          g.DrawString($"min ({xmin:F4}; {fxmin:F4})", font, textBrush, px + 8, py - 20);
        }
      }

      pictureBoxGraph.Image = bmp;
    }

    private void OnClearClick(object sender, EventArgs e)
    {
      textBoxFunction.Text = "";
      textBoxA.Text = "";
      textBoxB.Text = "";
      textBoxEpsilon.Text = "";
      labelResult.Text = "—";
      if (pictureBoxGraph.Image != null)
      {
        pictureBoxGraph.Image.Dispose();
        pictureBoxGraph.Image = null;
      }
    }
  }
}