using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("DS Hub")]
[assembly: AssemblyProduct("DS Hub")]
[assembly: AssemblyDescription("DS Hub 快速启动器")]
[assembly: AssemblyVersion("3.14.0.0")]

namespace DeepSeekHub
{
    internal static class Ui
    {
        public static float Scale = 1f;
        public static int S(int v) { return (int)Math.Round(v * Scale); }
        public static float SF(float v) { return v * Scale; }
        public static bool Dark = false;

        /// <summary>Per-user data folder (token, history, logs) — created on demand.</summary>
        public static string DataDir()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DS Hub");
            try { Directory.CreateDirectory(dir); } catch { }
            return dir;
        }

        /// <summary>Rounded chip behind the top-right icon buttons so they stay visible.</summary>
        public static void DrawChip(Graphics g, Control c)
        {
            Color chipBg = Ui.Dark ? Color.FromArgb(38, 42, 52) : Color.White;
            Color chipBorder = Ui.Dark ? Color.FromArgb(62, 66, 76) : Color.FromArgb(222, 226, 235);
            using (GraphicsPath cp = new GraphicsPath())
            {
                int d = Ui.S(12);
                Rectangle cr = new Rectangle(Ui.S(2), Ui.S(2), c.Width - Ui.S(4), c.Height - Ui.S(4));
                cp.AddArc(cr.X, cr.Y, d, d, 180, 90);
                cp.AddArc(cr.Right - d, cr.Y, d, d, 270, 90);
                cp.AddArc(cr.Right - d, cr.Bottom - d, d, d, 0, 90);
                cp.AddArc(cr.X, cr.Bottom - d, d, d, 90, 90);
                cp.CloseFigure();
                using (SolidBrush b = new SolidBrush(chipBg)) g.FillPath(b, cp);
                using (Pen p = new Pen(chipBorder)) g.DrawPath(p, cp);
            }
        }
    }

    internal static class Program
    {
                [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr dc, int index);

        [STAThread]
        private static void Main(string[] args)
        {
            // DPI-aware: text renders natively at the display DPI (sharp).
            // Layout is defined in 96-DPI units and scaled by Ui.S() to the
            // actual DPI, so text and layout always match (no clipping).
            try { SetProcessDPIAware(); } catch { }
            try
            {
                IntPtr dc = GetDC(IntPtr.Zero);
                if (dc != IntPtr.Zero)
                {
                    int dpi = GetDeviceCaps(dc, 88); // LOGPIXELSX
                    ReleaseDC(IntPtr.Zero, dc);
                    if (dpi > 0) Ui.Scale = dpi / 96f;
                }
            }
            catch { }
            try { System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12; } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            }
            catch { }
            Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e)
            {
                try { System.IO.File.AppendAllText(Path.Combine(Ui.DataDir(), "dsh-hub-crash.log"), DateTime.Now + " THREAD ERR: " + e.Exception + "\n"); } catch { }
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
            {
                try { System.IO.File.AppendAllText(Path.Combine(Ui.DataDir(), "dsh-hub-crash.log"), DateTime.Now + " APP ERR: " + e.ExceptionObject + "\n"); } catch { }
            };
            bool shot = args != null && args.Length > 0 && args[0] == "--screenshot";
            bool dark = args != null && Array.IndexOf(args, "--dark") >= 0;
            bool light = args != null && Array.IndexOf(args, "--light") >= 0;
            bool dock = args != null && Array.IndexOf(args, "--dock") >= 0;
            bool translateTest = args != null && Array.IndexOf(args, "--translate-test") >= 0;
            bool dumpLayout = args != null && Array.IndexOf(args, "--dump-layout") >= 0;
            bool rechargeTest = args != null && Array.IndexOf(args, "--recharge-test") >= 0;
            Application.Run(new MainForm(shot, dark, light, dock, translateTest, dumpLayout, rechargeTest));
        }
    }

    internal sealed class RoundButton : Control
    {
        private readonly Font mainFont;
        private Color baseColor = Color.FromArgb(77, 107, 254);
        private Color hoverColor = Color.FromArgb(96, 123, 255);
        private Color downColor = Color.FromArgb(60, 88, 214);
        private bool hover, pressed;

        /// <summary>Dark theme: adjusts the disabled-state palette.</summary>
        public bool Dark { get; set; }

        public RoundButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            mainFont = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        }

        /// <summary>false = blue "start/open" palette, true = red "stop" palette.</summary>
        public void SetMode(bool stopMode)
        {
            if (stopMode)
            {
                baseColor = Color.FromArgb(220, 76, 76);
                hoverColor = Color.FromArgb(236, 105, 105);
                downColor = Color.FromArgb(189, 58, 58);
            }
            else
            {
                baseColor = Color.FromArgb(77, 107, 254);
                hoverColor = Color.FromArgb(96, 123, 255);
                downColor = Color.FromArgb(60, 88, 214);
            }
            Invalidate();
        }

        /// <summary>Orange "restart" palette.</summary>
        public void SetRestartMode()
        {
            baseColor = Color.FromArgb(217, 119, 6);
            hoverColor = Color.FromArgb(232, 140, 40);
            downColor = Color.FromArgb(190, 100, 2);
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                mainFont.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill;
            Color textColor;
            if (!Enabled)
            {
                fill = Dark ? Color.FromArgb(42, 46, 56) : Color.FromArgb(226, 230, 238);
                textColor = Dark ? Color.FromArgb(118, 126, 142) : Color.FromArgb(158, 166, 182);
            }
            else
            {
                fill = pressed ? downColor : (hover ? hoverColor : baseColor);
                textColor = Color.White;
            }

            Rectangle sh = new Rectangle(1, 4, Width - 3, Height - 4);
            using (GraphicsPath sp = RoundRect(sh, 15))
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(Enabled ? 26 : 14, 30, 50, 90)))
            {
                g.FillPath(sb, sp);
            }

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 6);
            using (GraphicsPath gp = RoundRect(r, 15))
            using (SolidBrush b = new SolidBrush(fill))
            {
                g.FillPath(b, gp);
            }

            using (StringFormat sf = new StringFormat())
            using (SolidBrush tb = new SolidBrush(textColor))
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                RectangleF tr = new RectangleF(0, 4, Width, Height - 6);
                g.DrawString(Text, mainFont, tb, tr, sf);
            }
        }

        private static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    internal sealed class ThemeToggle : Control
    {
        private bool hover;

        /// <summary>true = the app is currently dark, so the button shows the sun (click for light).</summary>
        public bool DarkTheme { get; set; }

        public ThemeToggle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (hover)
            {
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(new Rectangle(1, 1, Width - 3, Height - 3));
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(38, 110, 120, 140)))
                    {
                        g.FillPath(b, gp);
                    }
                }
            }
            Color icon = Color.FromArgb(148, 156, 175);
            using (SolidBrush brush = new SolidBrush(icon))
            using (Pen pen = new Pen(icon, 1.4F))
            {
                Ui.DrawChip(g, this);
                if (DarkTheme)
                {
                    // sun: click to switch to light
                    float cx = Width / 2F;
                    float cy = Height / 2F;
                    g.FillEllipse(brush, cx - 6F, cy - 6F, 12F, 12F);
                    for (int i = 0; i < 8; i++)
                    {
                        double a = i * Math.PI / 4.0;
                        float x1 = cx + (float)Math.Cos(a) * 10F;
                        float y1 = cy + (float)Math.Sin(a) * 10F;
                        float x2 = cx + (float)Math.Cos(a) * 13.5F;
                        float y2 = cy + (float)Math.Sin(a) * 13.5F;
                        g.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
                else
                {
                    // moon: click to switch to dark
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        path.FillMode = FillMode.Alternate;
                        path.AddEllipse(5F, 4.5F, 9F, 9F);
                        path.AddEllipse(8.5F, 3F, 9F, 9F);
                        g.FillPath(brush, path);
                    }
                }
            }
        }
    }

    internal sealed class LogoBox : Control
    {
        private readonly Image logo;

        /// <summary>Dark theme: the official whale is drawn white (as the harness does).</summary>
        public bool Dark { get; set; }

        public LogoBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            logo = LoadLogo();
        }

        private static Image LoadLogo()
        {
            try
            {
                byte[] data = Convert.FromBase64String(LogoImage.Base64);
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(data))
                {
                    return new Bitmap(ms);
                }
            }
            catch
            {
                return null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && logo != null) logo.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (logo == null) { base.OnPaint(e); return; }
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            float pad = 2F;
            float side = Math.Min(Width, Height) - 2 * pad;
            float x = (Width - side) / 2F;
            float y = (Height - side) / 2F;
            if (Dark)
            {
                // map the black whale to white, keeping its alpha
                using (ImageAttributes ia = new ImageAttributes())
                {
                    ColorMatrix cm = new ColorMatrix();
                    cm.Matrix00 = 0; cm.Matrix01 = 0; cm.Matrix02 = 0;
                    cm.Matrix10 = 0; cm.Matrix11 = 0; cm.Matrix12 = 0;
                    cm.Matrix20 = 0; cm.Matrix21 = 0; cm.Matrix22 = 0;
                    cm.Matrix33 = 1F;
                    cm.Matrix40 = 255F; cm.Matrix41 = 255F; cm.Matrix42 = 255F;
                    ia.SetColorMatrix(cm);
                    g.DrawImage(logo, new Rectangle((int)x, (int)y, (int)side, (int)side),
                        0, 0, logo.Width, logo.Height, GraphicsUnit.Pixel, ia);
                }
            }
            else
            {
                g.DrawImage(logo, x, y, side, side);
            }
        }
    }

    /// <summary>Small pill button (used for the collapse-to-drawer control).</summary>
    internal sealed class PillButton : Control
    {
        private bool hover;
        private readonly Font font;

        /// <summary>Selected (segmented-control) state.</summary>
        public bool Selected { get; set; }

        public PillButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            font = new Font("Microsoft YaHei UI", 7.5F, FontStyle.Regular);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) font.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath gp = new GraphicsPath())
            {
                int d = r.Height;
                gp.AddArc(r.X, r.Y, d, d, 180, 90);
                gp.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                gp.CloseFigure();
                Color fill = Selected ? Color.FromArgb(34, 92, 118, 255) :
                    (hover ? Color.FromArgb(48, 110, 120, 140) : Color.FromArgb(28, 110, 120, 140));
                using (SolidBrush b = new SolidBrush(fill))
                {
                    g.FillPath(b, gp);
                }
            }
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                using (SolidBrush tb = new SolidBrush(Selected ? Color.FromArgb(92, 118, 255) : Color.FromArgb(150, 158, 175)))
                {
                    g.DrawString(Text, font, tb, new RectangleF(0, 0, Width, Height), sf);
                }
            }
        }
    }

    /// <summary>Collapse-to-drawer icon button (arrow up into a bar).</summary>
    internal sealed class CollapseIcon : Control
    {
        private bool hover;

        public CollapseIcon()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Ui.DrawChip(g, this);
            if (hover)
            {
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(new Rectangle(1, 1, Width - 3, Height - 3));
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(38, 110, 120, 140)))
                    {
                        g.FillPath(b, gp);
                    }
                }
            }
            Color c = Color.FromArgb(148, 156, 175);
            using (Pen pen = new Pen(c, 1.5F))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                // top bar + up chevron: collapse into the top drawer
                g.DrawLine(pen, Ui.SF(6.5F), Ui.SF(5F), Ui.SF(15.5F), Ui.SF(5F));
                g.DrawLine(pen, Ui.SF(11F), Ui.SF(15.5F), Ui.SF(7F), Ui.SF(11.5F));
                g.DrawLine(pen, Ui.SF(11F), Ui.SF(15.5F), Ui.SF(15F), Ui.SF(11.5F));
                g.DrawLine(pen, Ui.SF(7F), Ui.SF(18.5F), Ui.SF(15F), Ui.SF(18.5F));
            }
        }
    }

    /// <summary>Pin (always-on-top) toggle button.</summary>
    internal sealed class PinToggle : Control
    {
        private bool hover;

        /// <summary>Whether always-on-top is currently enabled.</summary>
        public bool Active { get; set; }

        public PinToggle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Ui.DrawChip(g, this);
            if (hover)
            {
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(new Rectangle(1, 1, Width - 3, Height - 3));
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(38, 110, 120, 140)))
                    {
                        g.FillPath(b, gp);
                    }
                }
            }
            Color c = Active ? Color.FromArgb(92, 118, 255) : Color.FromArgb(148, 156, 175);
            using (SolidBrush brush = new SolidBrush(c))
            {
                // thumbtack: head, neck, base
                g.FillEllipse(brush, Ui.SF(10.5F), Ui.SF(5.5F), Ui.SF(10F), Ui.SF(10F));
                g.FillRectangle(brush, Ui.SF(14.8F), Ui.SF(14F), Ui.SF(1.8F), Ui.SF(5F));
                g.FillEllipse(brush, Ui.SF(9F), Ui.SF(19F), Ui.SF(12F), Ui.SF(5F));
            }
        }
    }

    /// <summary>QQ-style top-edge drawer: a small floating whale icon that pops the app out on click.</summary>
    internal sealed class DrawerForm : Form
    {
        private readonly LogoBox whale;
        private readonly ContextMenuStrip menu;
        private readonly ToolStripMenuItem opacityItem;
        private float opacityLevel = 1F;
        private bool hover;
        private bool leftDown;
        private bool dragging;
        private Point downScreen;
        private int grabOffsetX;

        /// <summary>Left-click on the whale icon releases (pops out) the app window; dragging moves the drawer.</summary>
        public event Action ReleaseRequested;
        public event Action ExitRequested;

        public DrawerForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(Ui.S(96), Ui.S(44));
            ClientSize = new Size(Ui.S(96), Ui.S(44));
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            Cursor = Cursors.Hand;

            whale = new LogoBox();
            whale.Size = new Size(Ui.S(34), Ui.S(34));
            whale.Location = new Point(Ui.S(31), Ui.S(5));
            whale.Cursor = Cursors.Hand;
            whale.MouseEnter += delegate { hover = true; Invalidate(); };
            whale.MouseLeave += delegate { hover = false; Invalidate(); };
            Controls.Add(whale);

            // drag anywhere on the drawer (whale or pill); a click without movement releases
            MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) StartDrag();
            };
            MouseMove += delegate { MoveDrag(); };
            MouseUp += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) EndDrag();
            };
            whale.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) StartDrag();
            };
            whale.MouseMove += delegate { MoveDrag(); };
            whale.MouseUp += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) EndDrag();
            };
            MouseEnter += delegate { hover = true; Invalidate(); };
            MouseLeave += delegate { hover = false; Invalidate(); };

            menu = new ContextMenuStrip();
            opacityItem = new ToolStripMenuItem("透明度");
            foreach (int pct in new int[] { 100, 90, 80, 70, 60, 50 })
            {
                int p = pct;
                ToolStripMenuItem item = new ToolStripMenuItem(pct + "%", null, delegate { SetOpacity(p / 100F); });
                opacityItem.DropDownItems.Add(item);
            }
            menu.Items.Add(opacityItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("释放", null, delegate { if (ReleaseRequested != null) ReleaseRequested(); });
            menu.Items.Add("退出应用", null, delegate { if (ExitRequested != null) ExitRequested(); });
            menu.Opening += delegate
            {
                foreach (ToolStripItem it in opacityItem.DropDownItems)
                {
                    ToolStripMenuItem mi = it as ToolStripMenuItem;
                    if (mi != null) mi.Checked = mi.Text == ((int)Math.Round(opacityLevel * 100)) + "%";
                }
            };
            ContextMenuStrip = menu;
            whale.ContextMenuStrip = menu;
        }

        private void StartDrag()
        {
            leftDown = true;
            dragging = false;
            downScreen = Cursor.Position;
            grabOffsetX = Cursor.Position.X - Left;
            Capture = true;
        }

        private void MoveDrag()
        {
            if (!leftDown) return;
            Point p = Cursor.Position;
            if (!dragging && Math.Abs(p.X - downScreen.X) + Math.Abs(p.Y - downScreen.Y) > 6)
            {
                dragging = true;
            }
            if (dragging)
            {
                Rectangle wa = Screen.FromControl(this).WorkingArea;
                int x = p.X - grabOffsetX;
                x = Math.Max(wa.Left, Math.Min(x, wa.Right - Width));
                Location = new Point(x, wa.Top);
            }
        }

        private void EndDrag()
        {
            bool wasDrag = dragging;
            leftDown = false;
            dragging = false;
            Capture = false;
            if (wasDrag)
            {
                // after a drag, require a fresh hover before auto-popping again
                SuppressHoverUntil = DateTime.UtcNow.AddSeconds(1.5);
            }
            else if (ReleaseRequested != null)
            {
                ReleaseRequested();
            }
        }

        /// <summary>True while the user is pressing/dragging the drawer (hover-pop must wait).</summary>
        public bool IsDragging
        {
            get { return leftDown || dragging; }
        }

        /// <summary>Hover auto-pop is suppressed until this moment (e.g. right after a drag).</summary>
        public DateTime SuppressHoverUntil { get; set; }

        public void SetTheme(bool dark)
        {
            whale.Dark = dark;
            whale.Invalidate();
            Invalidate();
        }

        public void SetOpacity(float level)
        {
            opacityLevel = level;
            Opacity = level;
        }

        public void SaveShot(string path)
        {
            try
            {
                using (Bitmap bmp = new Bitmap(Width, Height))
                {
                    bmp.SetResolution(Ui.Scale * 96f, Ui.Scale * 96f);
                    DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height));
                    bmp.Save(path, ImageFormat.Png);
                }
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle shadow = new Rectangle(Ui.S(28), Ui.S(8), Ui.S(42), Ui.S(38));
            using (GraphicsPath sp = RoundRect(shadow, Ui.S(15)))
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(70, 20, 24, 32)))
            {
                g.FillPath(sb, sp);
            }
            Rectangle pill = new Rectangle(Ui.S(26), Ui.S(3), Ui.S(44), Ui.S(40));
            using (GraphicsPath gp = RoundRect(pill, Ui.S(15)))
            {
                bool dark = whale.Dark;
                using (SolidBrush b = new SolidBrush(dark ? Color.FromArgb(30, 33, 40) : Color.FromArgb(250, 250, 253)))
                {
                    g.FillPath(b, gp);
                }
                using (Pen p = new Pen(hover ? Color.FromArgb(120, 148, 156, 175) :
                                            (dark ? Color.FromArgb(62, 66, 76) : Color.FromArgb(222, 226, 235)), hover ? 1.6F : 1F))
                {
                    g.DrawPath(p, gp);
                }
            }
        }

        private static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    /// <summary>Small modal dialog to enter the DeepSeek Platform API key.</summary>
    internal sealed class TokenDialog : Form
    {
        private readonly TextBox box;

        public string TokenValue
        {
            get { return box.Text.Trim(); }
        }

        public TokenDialog(string initial)
        {
            Text = "DeepSeek API Token";
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Microsoft YaHei UI", 9F);
            ClientSize = new Size(Ui.S(372), Ui.S(158));
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Label hint = new Label();
            hint.Text = "输入 DeepSeek Platform 的 API Key（sk-...），\n用于查询实时余额。仅保存在本机。";
            hint.Font = new Font("Microsoft YaHei UI", 8.5F);
            hint.ForeColor = Color.FromArgb(140, 148, 165);
            hint.SetBounds(Ui.S(18), Ui.S(12), Ui.S(336), Ui.S(36));

            box = new TextBox();
            box.Text = initial ?? "";
            box.Font = new Font("Microsoft YaHei UI", 9F);
            box.SetBounds(Ui.S(18), Ui.S(54), Ui.S(336), Ui.S(26));

            Button ok = new Button();
            ok.Text = "保存";
            ok.SetBounds(Ui.S(372) - Ui.S(18) - Ui.S(148), Ui.S(110), Ui.S(70), Ui.S(32));
            ok.DialogResult = DialogResult.OK;

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.SetBounds(Ui.S(372) - Ui.S(18) - Ui.S(72), Ui.S(110), Ui.S(70), Ui.S(32));
            cancel.DialogResult = DialogResult.Cancel;

            Controls.Add(cancel);
            Controls.Add(ok);
            Controls.Add(box);
            Controls.Add(hint);
            AcceptButton = ok;
            CancelButton = cancel;
            box.Select();
        }
    }

    /// <summary>DeepSeek API price table: peak vs off-peak, Flash vs Pro (yuan / 1M tokens).</summary>
    internal sealed class PriceForm : Form
    {
        public PriceForm(bool dark, bool peakNow)
        {
            Text = "DeepSeek API 价格";
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Microsoft YaHei UI", 9F);
            ClientSize = new Size(Ui.S(430), Ui.S(246));
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = dark ? Color.FromArgb(24, 26, 32) : Color.White;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Color head = dark ? Color.FromArgb(150, 158, 175) : Color.FromArgb(110, 118, 132);
            Color cell = dark ? Color.FromArgb(206, 212, 226) : Color.FromArgb(30, 35, 45);
            Color hot = Color.FromArgb(217, 119, 6);
            Color cool = Color.FromArgb(34, 154, 88);

            Label title = new Label();
            title.Text = "DeepSeek API 价格（元 / 百万 tokens）";
            title.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            title.ForeColor = cell;
            title.SetBounds(Ui.S(16), Ui.S(12), Ui.S(398), Ui.S(22));
            Controls.Add(title);

            string[] heads = { "模型", "时段", "输入·命中", "输入·未命中", "输出" };
            int[] xs = { 16, 90, 170, 270, 370 };
            for (int i = 0; i < heads.Length; i++)
            {
                Label l = new Label();
                l.Text = heads[i];
                l.Font = new Font("Microsoft YaHei UI", 8F);
                l.ForeColor = head;
                l.SetBounds(Ui.S(xs[i]), Ui.S(40), Ui.S(80), Ui.S(18));
                Controls.Add(l);
            }

            string[,] rows = {
                { "Flash", "高峰", "0.10", "3.0", "9.0" },
                { "Flash", "空闲", "0.05", "1.5", "4.5" },
                { "Pro",   "高峰", "0.30", "9.0", "27.0" },
                { "Pro",   "空闲", "0.15", "4.5", "13.5" }
            };
            bool[] rowPeak = { true, false, true, false };
            int y = 62;
            for (int r = 0; r < 4; r++)
            {
                bool current = rowPeak[r] == peakNow;
                for (int c = 0; c < 5; c++)
                {
                    Label l = new Label();
                    l.Text = rows[r, c];
                    FontStyle fs = FontStyle.Regular;
                    Color fc = cell;
                    if (c == 0) fs = FontStyle.Bold;
                    if (current) { fs = FontStyle.Bold; fc = rowPeak[r] ? hot : cool; }
                    l.Font = new Font("Microsoft YaHei UI", 8F, fs);
                    l.ForeColor = fc;
                    l.SetBounds(Ui.S(xs[c]), Ui.S(y), Ui.S(80), Ui.S(18));
                    Controls.Add(l);
                }
                y += 22;
            }

            Label note = new Label();
            note.Text = "高峰时段：每日 09:00-12:00、14:00-18:00（北京时间）\n价格随官方调整，以 api-docs.deepseek.com 为准";
            note.Font = new Font("Microsoft YaHei UI", 8F);
            note.ForeColor = head;
            note.SetBounds(Ui.S(16), Ui.S(156), Ui.S(398), Ui.S(36));
            Controls.Add(note);

            Button close = new Button();
            close.Text = "关闭";
            close.SetBounds(Ui.S(430) - Ui.S(110), Ui.S(198), Ui.S(80), Ui.S(30));
            close.Click += delegate { Close(); };
            Controls.Add(close);
        }
    }

    /// <summary>Small borderless toast for translation results (click or Esc to close).</summary>
    internal sealed class ToastForm : Form
    {
        public ToastForm(string title, string body, bool dark)
        {
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = dark ? Color.FromArgb(32, 35, 42) : Color.FromArgb(252, 252, 254);

            Label t = new Label();
            t.Text = title;
            t.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            t.ForeColor = dark ? Color.FromArgb(210, 216, 228) : Color.FromArgb(30, 35, 45);
            t.SetBounds(Ui.S(18), Ui.S(12), Ui.S(424), Ui.S(20));

            Label b = new Label();
            b.Text = body;
            b.Font = new Font("Microsoft YaHei UI", 10F);
            b.ForeColor = dark ? Color.FromArgb(190, 197, 212) : Color.FromArgb(50, 55, 66);
            b.AutoSize = true;
            b.MaximumSize = new Size(Ui.S(424), Ui.S(320));
            b.SetBounds(Ui.S(18), Ui.S(38), Ui.S(424), Ui.S(24));

            Label f = new Label();
            f.Text = "已复制到剪贴板 · 点击或按 Esc 关闭";
            f.Font = new Font("Microsoft YaHei UI", 8.5F);
            f.ForeColor = Color.FromArgb(140, 148, 165);
            f.SetBounds(Ui.S(18), Ui.S(0), Ui.S(424), Ui.S(20));

            Controls.Add(f);
            Controls.Add(b);
            Controls.Add(t);

            Load += delegate
            {
                int bh = b.Height;
                int w = 460;
                int h = 38 + bh + 28;
                Size = new Size(w, h);
                f.SetBounds(Ui.S(18), Ui.S(38) + bh + Ui.S(4), Ui.S(424), Ui.S(20));
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                Location = new Point(wa.Right - w - Ui.S(14), wa.Bottom - h - Ui.S(14));
            };

            KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); };
            Click += delegate { Close(); };
            t.Click += delegate { Close(); };
            b.Click += delegate { Close(); };
            f.Click += delegate { Close(); };
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }
    }

    /// <summary>Translation history dialog: pick an entry to copy its translation.</summary>
    internal sealed class HistoryForm : Form
    {
        private readonly ListBox list;
        private readonly List<string> dsts = new List<string>();

        public HistoryForm(List<Dictionary<string, object>> history)
        {
            Text = "翻译历史";
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Microsoft YaHei UI", 9F);
            ClientSize = new Size(Ui.S(480), Ui.S(330));
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            list = new ListBox();
            list.SetBounds(Ui.S(14), Ui.S(14), Ui.S(452), Ui.S(260));
            list.HorizontalScrollbar = true;
            foreach (var d in history)
            {
                string src = Convert.ToString(d["src"]);
                string dst = Convert.ToString(d["dst"]);
                string t = Convert.ToString(d["time"]);
                dsts.Add(dst);
                list.Items.Add("[" + t + "] " + src + "  →  " + dst);
            }

            Button copy = new Button();
            copy.Text = "复制译文";
            copy.SetBounds(Ui.S(14), Ui.S(284), Ui.S(100), Ui.S(32));
            copy.DialogResult = DialogResult.OK;

            Button close = new Button();
            close.Text = "关闭";
            close.SetBounds(Ui.S(124), Ui.S(284), Ui.S(70), Ui.S(32));
            close.DialogResult = DialogResult.Cancel;

            Controls.Add(close);
            Controls.Add(copy);
            Controls.Add(list);
            AcceptButton = copy;
            CancelButton = close;
        }

        public string SelectedTranslation()
        {
            int i = list.SelectedIndex;
            if (i >= 0 && i < dsts.Count) return dsts[i];
            return null;
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly Label status;
        private readonly Label timeStatus;
        private readonly Label timeDetail;
        private readonly LogoBox logoBox;
        private readonly RoundButton btnChat;
        private readonly RoundButton btnHarness;
        private readonly RoundButton btnRestart;
        private readonly RoundButton btnPlatform;
        private readonly RoundButton btnStop;
        private readonly System.Windows.Forms.Timer poll;
        private readonly System.Windows.Forms.Timer clock;
        private readonly System.Windows.Forms.Timer state;
        private readonly bool forceDark;
        private readonly bool forceLight;
        private readonly bool dockTest;
        private readonly bool translateTest;
        private readonly bool dumpLayout;
        private readonly ThemeToggle themeToggle;
        private readonly PinToggle pinToggle;
        private readonly CollapseIcon collapseBtn;
        private readonly ContextMenuStrip themeMenu;
        private readonly ToolTip tips;
        private readonly DrawerForm drawer;
        private readonly System.Windows.Forms.Timer animTimer;
        private readonly System.Windows.Forms.Timer pollTimer;
        private readonly Label balanceValue;
        private readonly Label balanceSub;
        private readonly System.Windows.Forms.Timer balanceTimer;
        private readonly Panel hairline;
        private readonly TextBox inputBox;
        private readonly TextBox outputBox;
        private readonly Label copyFeedback;
        private readonly PillButton btnDirAuto;
        private readonly PillButton btnDirEn;
        private readonly PillButton btnDirZh;
        private readonly Panel balanceArea;
        private readonly PillButton btnRefresh;
        private readonly PillButton btnSetToken;
        private readonly PillButton btnRecharge;
        private readonly PillButton priceChip;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private Panel rechargeBar;
        private Size preRechargeSize;
        private readonly Panel bottomArea;
        private readonly Panel translatePanel;
        private readonly PillButton btnDoTranslate;
        private readonly PillButton btnCopy;
        private readonly PillButton btnClear;
        private readonly PillButton btnHistory;
        private readonly System.Windows.Forms.Timer feedbackTimer;
        private readonly string tokenPath;
        private readonly string historyPath;
        private readonly List<Dictionary<string, object>> history = new List<Dictionary<string, object>>();
        private ToastForm toast;
        private bool translating;
        private bool hotkeyRegistered;
        private bool pinEnabled;
        private int translateDir; // 0 auto, 1 zh->en, 2 en->zh
        private Size lastSize;
        private string currentToken;
        private bool balanceBusy;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 0xDB01;
        private Panel root;
        private DateTime deadline;
        private bool polling;
        private bool stopping;
        private bool restartAfterStop;
        private bool currentDark;
        private int themeMode; // 0 = follow system, 1 = force dark, 2 = force light
        private bool docked;
        private bool expanded;
        private int animState; // 0 idle, 1 slide down, 2 slide up
        private int animTarget;
        private int dockX;
        private DateTime expandedAt;
        private float opacityLevel = 1F;
        private IntPtr mouseHook;
        private HookProc hookProc;

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern int GetMessagePos();
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string moduleName);

        public MainForm(bool screenshot, bool forceDark, bool forceLight, bool dockTest, bool translateTest, bool dumpLayout, bool rechargeTest)
        {
            this.forceDark = forceDark;
            this.forceLight = forceLight;
            this.dockTest = dockTest;
            this.translateTest = translateTest;
            this.dumpLayout = dumpLayout;
            tokenPath = Path.Combine(Ui.DataDir(), "ds-balance-token.txt");
            historyPath = Path.Combine(Ui.DataDir(), "ds-translate-history.json");
            Text = "DS Hub";
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Microsoft YaHei UI", 9F);
            ClientSize = new Size(Ui.S(320), Ui.S(538));
            MinimumSize = new Size(Ui.S(320), Ui.S(538));
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            BackColor = Color.White;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            if (screenshot)
            {
                StartPosition = FormStartPosition.Manual;
                Location = new Point(Ui.S(-5000), Ui.S(-5000));
                ShowInTaskbar = false;
            }
            else
            {
                StartPosition = FormStartPosition.CenterScreen;
            }

            Panel root = new Panel();
            this.root = root;
            root.Dock = DockStyle.Fill;
            root.BackColor = Color.White;
            root.Padding = new Padding(Ui.S(14), Ui.S(12), Ui.S(14), Ui.S(10));
            Controls.Add(root);

            Label timeStatus = new Label();
            this.timeStatus = timeStatus;
            timeStatus.Dock = DockStyle.Top;
            timeStatus.Height = Ui.S(20);
            timeStatus.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            timeStatus.TextAlign = ContentAlignment.MiddleCenter;
            timeStatus.BackColor = Color.Transparent;

            Label timeDetail = new Label();
            this.timeDetail = timeDetail;
            timeDetail.Dock = DockStyle.Top;
            timeDetail.Height = Ui.S(16);
            timeDetail.Font = new Font("Microsoft YaHei UI", 8F);
            timeDetail.TextAlign = ContentAlignment.MiddleCenter;
            timeDetail.BackColor = Color.Transparent;

            LogoBox logo = new LogoBox();
            this.logoBox = logo;
            logo.Dock = DockStyle.Top;
            logo.Height = Ui.S(48);

            RoundButton btnChat = MakeButton("DeepSeek 网页版");
            this.btnChat = btnChat;
            RoundButton btnHarness = MakeButton("启动 DeepSeek Harness");
            this.btnHarness = btnHarness;
            RoundButton btnRestart = MakeButton("重启 DeepSeek Harness");
            btnRestart.SetRestartMode();
            btnRestart.Enabled = false;
            this.btnRestart = btnRestart;
            RoundButton btnPlatform = MakeButton("DeepSeek Platform");
            this.btnPlatform = btnPlatform;
            RoundButton btnStop = MakeButton("关闭 DeepSeek Harness");
            btnStop.SetMode(true);
            btnStop.Enabled = false;
            this.btnStop = btnStop;

            status = new Label();
            status.Dock = DockStyle.Top;
            status.Height = Ui.S(18);
            status.Text = "点击按钮，快速进入 DeepSeek";
            status.Font = new Font("Microsoft YaHei UI", 8F);
            status.ForeColor = Color.FromArgb(140, 148, 165);
            status.TextAlign = ContentAlignment.MiddleCenter;

            // API balance strip
            tips = new ToolTip();

            Panel balanceArea = new Panel();
            this.balanceArea = balanceArea;
            balanceArea.Dock = DockStyle.Top;
            balanceArea.Height = Ui.S(72);
            balanceArea.BackColor = Color.White;

            Label balTitle = new Label();
            balTitle.Text = "API 余额";
            balTitle.Font = new Font("Microsoft YaHei UI", 7.5F);
            balTitle.ForeColor = Color.FromArgb(150, 158, 175);
            balTitle.SetBounds(Ui.S(14), Ui.S(6), Ui.S(180), Ui.S(16));

            Label balValue = new Label();
            this.balanceValue = balValue;
            balValue.Text = "—";
            balValue.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            balValue.ForeColor = Color.FromArgb(30, 35, 45);
            balValue.SetBounds(Ui.S(14), Ui.S(24), Ui.S(220), Ui.S(26));

            Label balSub = new Label();
            this.balanceSub = balSub;
            balSub.Text = "获取中…";
            balSub.Font = new Font("Microsoft YaHei UI", 7.5F);
            balSub.ForeColor = Color.FromArgb(150, 158, 175);
            balSub.SetBounds(Ui.S(14), Ui.S(52), Ui.S(220), Ui.S(16));

            PillButton btnRefresh = new PillButton();
            this.btnRefresh = btnRefresh;
            btnRefresh.Text = "刷新";
            btnRefresh.SetBounds(Ui.S(118), Ui.S(26), Ui.S(56), Ui.S(26));
            btnRefresh.Click += delegate { RefreshBalance(); };

            PillButton btnSetToken = new PillButton();
            this.btnSetToken = btnSetToken;
            btnSetToken.Text = "设置";
            btnSetToken.SetBounds(Ui.S(174), Ui.S(26), Ui.S(56), Ui.S(26));
            btnSetToken.Click += delegate { ShowTokenDialog(); };

            PillButton btnRecharge = new PillButton();
            this.btnRecharge = btnRecharge;
            btnRecharge.Text = "充值";
            btnRecharge.SetBounds(Ui.S(230), Ui.S(26), Ui.S(56), Ui.S(26));
            btnRecharge.Click += delegate { ShowRecharge(); };

            tips.SetToolTip(btnRefresh, "刷新 API 余额（每分钟自动刷新）");
            tips.SetToolTip(btnSetToken, "设置 DeepSeek API Token");
            tips.SetToolTip(btnRecharge, "打开 Platform 充值页面");

            balanceArea.Controls.Add(btnRecharge);
            balanceArea.Controls.Add(btnRefresh);
            balanceArea.Controls.Add(btnSetToken);
            balanceArea.Controls.Add(balSub);
            balanceArea.Controls.Add(balValue);
            balanceArea.Controls.Add(balTitle);

            // ===== bottom tool section: tab bar + switchable tool panels =====
            Panel bottomArea = new Panel();
            this.bottomArea = bottomArea;
            bottomArea.Dock = DockStyle.Top;
            bottomArea.Height = Ui.S(140);
            bottomArea.BackColor = Color.White;

            // horizontal feature row: a docked tab strip so the tool host sits below it
            // ---- translate tool ----
            Panel translatePanel = new Panel();
            this.translatePanel = translatePanel;
            translatePanel.Dock = DockStyle.Fill;

            btnDirAuto = MakeDirPill("自动", 0, 14);
            btnDirEn = MakeDirPill("中→英", 1, 60);
            btnDirZh = MakeDirPill("英→中", 2, 106);
            RefreshDirPills();
            translatePanel.Controls.Add(btnDirZh);
            translatePanel.Controls.Add(btnDirEn);
            translatePanel.Controls.Add(btnDirAuto);

            TextBox inputBox = new TextBox();
            this.inputBox = inputBox;
            inputBox.Multiline = true;
            inputBox.ScrollBars = ScrollBars.Vertical;
            inputBox.AcceptsReturn = true;
            inputBox.Font = new Font("Microsoft YaHei UI", 8F);
            inputBox.BackColor = Color.White;
            inputBox.ForeColor = Color.FromArgb(30, 35, 45);
            inputBox.SetBounds(Ui.S(14), Ui.S(28), Ui.S(264), Ui.S(36));

            TextBox outputBox = new TextBox();
            this.outputBox = outputBox;
            outputBox.Multiline = true;
            outputBox.ScrollBars = ScrollBars.Vertical;
            outputBox.ReadOnly = true;
            outputBox.Font = new Font("Microsoft YaHei UI", 8F);
            outputBox.BackColor = Color.FromArgb(250, 251, 253);
            outputBox.ForeColor = Color.FromArgb(30, 35, 45);
            outputBox.SetBounds(Ui.S(14), Ui.S(68), Ui.S(264), Ui.S(36));

            Label copyFeedback = new Label();
            this.copyFeedback = copyFeedback;
            copyFeedback.Text = "";
            copyFeedback.Font = new Font("Microsoft YaHei UI", 7.5F);
            copyFeedback.ForeColor = Color.FromArgb(34, 154, 88);
            copyFeedback.SetBounds(Ui.S(14), Ui.S(110), Ui.S(64), Ui.S(18));

            feedbackTimer = new System.Windows.Forms.Timer();
            feedbackTimer.Interval = 2000;
            feedbackTimer.Tick += delegate { feedbackTimer.Stop(); copyFeedback.Text = ""; };

            PillButton btnDoTranslate = new PillButton();
            this.btnDoTranslate = btnDoTranslate;
            btnDoTranslate.Text = "翻译";
            btnDoTranslate.SetBounds(Ui.S(22), Ui.S(108), Ui.S(56), Ui.S(24));
            btnDoTranslate.Click += delegate { OnTranslateClick(); };

            PillButton btnCopy = new PillButton();
            this.btnCopy = btnCopy;
            btnCopy.Text = "复制";
            btnCopy.SetBounds(Ui.S(86), Ui.S(108), Ui.S(56), Ui.S(24));
            btnCopy.Click += delegate
            {
                if (outputBox.Text.Length > 0)
                {
                    try { Clipboard.SetText(outputBox.Text); } catch { }
                    ShowCopyFeedback();
                }
            };

            PillButton btnClear = new PillButton();
            this.btnClear = btnClear;
            btnClear.Text = "清空";
            btnClear.SetBounds(Ui.S(150), Ui.S(108), Ui.S(56), Ui.S(24));
            btnClear.Click += delegate { inputBox.Clear(); outputBox.Clear(); };

            PillButton btnHistory = new PillButton();
            this.btnHistory = btnHistory;
            btnHistory.Text = "历史";
            btnHistory.SetBounds(Ui.S(214), Ui.S(108), Ui.S(56), Ui.S(24));
            btnHistory.Click += delegate { ShowHistory(); };

            tips.SetToolTip(btnDoTranslate, "翻译输入框内容；输入框为空则翻译剪贴板（Ctrl+Alt+T）");
            tips.SetToolTip(btnCopy, "复制译文（中译英复制英文，英译中复制中文）");
            tips.SetToolTip(btnClear, "清空输入和显示");
            tips.SetToolTip(btnHistory, "查看最近 100 条翻译历史");

            translatePanel.Controls.Add(btnHistory);
            translatePanel.Controls.Add(btnClear);
            translatePanel.Controls.Add(btnCopy);
            translatePanel.Controls.Add(btnDoTranslate);
            translatePanel.Controls.Add(copyFeedback);
            translatePanel.Controls.Add(outputBox);
            translatePanel.Controls.Add(inputBox);

            bottomArea.Controls.Add(translatePanel);
            LoadHistory();
            Resize += delegate { OnFormResize(); };

            Panel hairline = new Panel();
            this.hairline = hairline;
            hairline.Dock = DockStyle.Top;
            hairline.Height = Ui.S(1);
            hairline.BackColor = Color.FromArgb(229, 234, 246);

            Panel hairline2 = new Panel();
            hairline2.Dock = DockStyle.Top;
            hairline2.Height = Ui.S(1);
            hairline2.BackColor = Color.FromArgb(229, 234, 246);

            // DockStyle.Top: add bottom-most first. Spacer panels provide the gaps
            // (Top-dock ignores Margin.Bottom in this WinForms version).
            root.Controls.Add(bottomArea);
            root.Controls.Add(hairline2);
            root.Controls.Add(status);
            root.Controls.Add(balanceArea);
            root.Controls.Add(hairline);
            root.Controls.Add(btnStop);
            root.Controls.Add(MakeSpacer());
            root.Controls.Add(btnPlatform);
            root.Controls.Add(MakeSpacer());
            root.Controls.Add(btnRestart);
            root.Controls.Add(MakeSpacer());
            root.Controls.Add(btnHarness);
            root.Controls.Add(MakeSpacer());
            root.Controls.Add(btnChat);
            root.Controls.Add(logo);
            root.Controls.Add(timeDetail);
            root.Controls.Add(timeStatus);

            btnChat.Click += delegate { OpenUrl("https://chat.deepseek.com"); };
            btnPlatform.Click += delegate { OpenUrl("https://platform.deepseek.com"); };
            btnHarness.Click += delegate { OpenOrStartHarness(); };
            btnRestart.Click += delegate { RestartHarness(); };
            btnStop.Click += delegate { StopHarness(); };
            tips.SetToolTip(btnRestart, "重启：关闭浏览器与终端后，重新启动 Harness 并自动打开浏览器");

            // API price chip in the top-left free area (click opens the price table)
            PillButton priceChip = new PillButton();
            this.priceChip = priceChip;
            priceChip.Text = "价格";
            priceChip.SetBounds(Ui.S(16), Ui.S(18), Ui.S(56), Ui.S(16));
            priceChip.Click += delegate { ShowPriceTable(); };
            tips.SetToolTip(priceChip, "查看 DeepSeek API 价格（高峰 / 空闲，Flash / Pro）");
            root.Controls.Add(priceChip);
            priceChip.BringToFront();

            // manual theme toggle in the top-right corner (left click flips,
            // right click picks follow-system / dark / light)
            themeToggle = new ThemeToggle();
            themeToggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            themeToggle.Location = new Point(root.Width - Ui.S(32), Ui.S(6));
            themeToggle.Size = new Size(Ui.S(22), Ui.S(22));
            themeToggle.Click += delegate { ToggleTheme(); };

            themeMenu = new ContextMenuStrip();
            themeMenu.Items.Add("跟随系统", null, delegate { SetThemeMode(0); });
            themeMenu.Items.Add("深色", null, delegate { SetThemeMode(1); });
            themeMenu.Items.Add("浅色", null, delegate { SetThemeMode(2); });
            themeToggle.ContextMenuStrip = themeMenu;

            tips.SetToolTip(themeToggle, "切换深浅色主题（右键：跟随系统 / 深色 / 浅色）");

            // pin (always-on-top) toggle in the top-right cluster
            pinToggle = new PinToggle();
            pinToggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pinToggle.Location = new Point(root.Width - Ui.S(58), Ui.S(6));
            pinToggle.Size = new Size(Ui.S(22), Ui.S(22));
            pinToggle.Click += delegate
            {
                pinEnabled = !pinEnabled;
                if (!docked) TopMost = pinEnabled;
                pinToggle.Active = pinEnabled;
                tips.SetToolTip(pinToggle, pinEnabled ? "已置顶（点击取消）" : "置顶窗口（点击开启）");
            };
            tips.SetToolTip(pinToggle, "置顶窗口（点击开启）");
            root.Controls.Add(pinToggle);

            // collapse-to-drawer icon button (compact, right-aligned with fixed gaps)
            collapseBtn = new CollapseIcon();
            collapseBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            collapseBtn.Location = new Point(root.Width - Ui.S(84), Ui.S(6));
            collapseBtn.Size = new Size(Ui.S(22), Ui.S(22));
            collapseBtn.Click += delegate { ToggleDrawer(); };
            tips.SetToolTip(collapseBtn, "收起：收纳到桌面顶部抽屉（点击鲸鱼释放）");
            root.Controls.Add(themeToggle);
            root.Controls.Add(pinToggle);
            root.Controls.Add(collapseBtn);

            // top-right icon buttons must paint above the docked labels:
            // index 0 is the TOP of the z-order here, so bring them to the front
            themeToggle.BringToFront();
            pinToggle.BringToFront();
            collapseBtn.BringToFront();

            // QQ-style top-edge drawer
            drawer = new DrawerForm();
            drawer.ReleaseRequested += delegate { PopOut(); };
            drawer.ExitRequested += delegate { Application.Exit(); };

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 15;
            animTimer.Tick += delegate { OnAnimTick(); };

            pollTimer = new System.Windows.Forms.Timer();
            pollTimer.Interval = 120;
            pollTimer.Tick += delegate { OnDockPoll(); };

            poll = new System.Windows.Forms.Timer();
            poll.Interval = 1500;
            poll.Tick += delegate { OnPollTick(); };

            clock = new System.Windows.Forms.Timer();
            clock.Interval = 10000;
            clock.Tick += delegate { UpdateTimeStatus(); };
            UpdateTimeStatus();
            clock.Start();

            state = new System.Windows.Forms.Timer();
            state.Interval = 2000;
            state.Tick += delegate { UpdateHarnessState(); };
            UpdateHarnessState();
            state.Start();

            LoadToken();
            balanceTimer = new System.Windows.Forms.Timer();
            balanceTimer.Interval = 60000;
            balanceTimer.Tick += delegate { RefreshBalance(); };
            balanceTimer.Start();
            RefreshBalance();

            if (translateTest)
            {
                // stage 1: clipboard path
                System.Windows.Forms.Timer tt = new System.Windows.Forms.Timer();
                tt.Interval = 1500;
                tt.Tick += delegate
                {
                    tt.Stop();
                    try
                    {
                        Clipboard.SetText("DeepSeek is a powerful AI assistant that supports code generation, text analysis, and real-time search.");
                    }
                    catch { }
                    TranslateClipboard();
                    // stage 2: input-box path (zh -> en, forced direction)
                    System.Windows.Forms.Timer tt2 = new System.Windows.Forms.Timer();
                    tt2.Interval = 14000;
                    tt2.Tick += delegate
                    {
                        tt2.Stop();
                        inputBox.Text = "人工智能正在深刻改变软件开发的方式。";
                        translateDir = 1;
                        RefreshDirPills();
                        OnTranslateClick();
                        System.Windows.Forms.Timer tt3 = new System.Windows.Forms.Timer();
                        tt3.Interval = 14000;
                        tt3.Tick += delegate { tt3.Stop(); Application.Exit(); };
                        tt3.Start();
                    };
                    tt2.Start();
                };
                tt.Start();
            }

            ApplyTheme(EffectiveDark());

            if (rechargeTest)
            {
                System.Windows.Forms.Timer rt = new System.Windows.Forms.Timer();
                rt.Interval = 1500;
                rt.Tick += delegate
                {
                    rt.Stop();
                    ShowRecharge();
                    System.Windows.Forms.Timer rt2 = new System.Windows.Forms.Timer();
                    rt2.Interval = 12000;
                    rt2.Tick += delegate
                    {
                        rt2.Stop();
                        try
                        {
                            bool ok = webView != null && webView.CoreWebView2 != null;
                            System.IO.File.WriteAllText(Path.Combine(Ui.DataDir(), "recharge-test.log"),
                                DateTime.Now + " webview ready=" + ok + " url=" + (ok ? webView.Source.ToString() : "n/a") + "\n");
                        }
                        catch { }
                        Application.Exit();
                    };
                    rt2.Start();
                };
                rt.Start();
            }

            if (screenshot && dockTest)
            {
                System.Windows.Forms.Timer dockShot = new System.Windows.Forms.Timer();
                dockShot.Interval = 500;
                int step = 0;
                dockShot.Tick += delegate
                {
                    step++;
                    if (step == 1)
                    {
                        EnterDrawerMode();
                    }
                    else if (step == 2)
                    {
                        drawer.SaveShot("D:\\DSH_start\\hub-preview-drawer.png");
                        PopOut();
                    }
                    else if (step == 3)
                    {
                        dockShot.Stop();
                        try
                        {
                            using (Bitmap bmp = new Bitmap(root.Width, root.Height))
                            {
                                bmp.SetResolution(Ui.Scale * 96f, Ui.Scale * 96f);
                            root.DrawToBitmap(bmp, new Rectangle(0, 0, root.Width, root.Height));
                                bmp.Save("D:\\DSH_start\\hub-preview.png", ImageFormat.Png);
                            }
                        }
                        catch { }
                        Application.Exit();
                    }
                };
                dockShot.Start();
            }
            else if (screenshot)
            {
                System.Windows.Forms.Timer shot = new System.Windows.Forms.Timer();
                shot.Interval = 400;
                shot.Tick += delegate
                {
                    shot.Stop();
                    try
                    {
                        using (Bitmap bmp = new Bitmap(root.Width, root.Height))
                        {
                            bmp.SetResolution(Ui.Scale * 96f, Ui.Scale * 96f);
                            root.DrawToBitmap(bmp, new Rectangle(0, 0, root.Width, root.Height));
                            bmp.Save("D:\\DSH_start\\hub-preview.png", ImageFormat.Png);
                        }
                    }
                    catch { }
                    Application.Exit();
                };
                shot.Start();
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (dumpLayout)
            {
                try
                {
                    StringBuilder lb = new StringBuilder();
                    DumpControls(root, lb, 0);
                    System.IO.File.WriteAllText("D:\\DSH_start\\layout-dump.txt", lb.ToString());
                }
                catch { }
            }
            // ensure the window fits the current screen's work area (DPI-aware):
            // clamp size to the work area and re-assert the minimum, then center.
            try
            {
                Rectangle wa = Screen.FromControl(this).WorkingArea;
                int chromeW = Width - ClientSize.Width;
                int chromeH = Height - ClientSize.Height;
                int w = Math.Min(Width, wa.Width - chromeW);
                int h = Math.Min(Height, wa.Height - chromeH);
                if (w < MinimumSize.Width) w = MinimumSize.Width;
                if (h < MinimumSize.Height) h = MinimumSize.Height;
                Size = new Size(w, h);
                Left = wa.Left + (wa.Width - Width) / 2;
                Top = wa.Top + (wa.Height - Height) / 2;
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                poll.Stop();
                poll.Dispose();
                clock.Stop();
                clock.Dispose();
                state.Stop();
                state.Dispose();
                animTimer.Stop();
                animTimer.Dispose();
                pollTimer.Stop();
                pollTimer.Dispose();
                balanceTimer.Stop();
                balanceTimer.Dispose();
                feedbackTimer.Stop();
                feedbackTimer.Dispose();
                UninstallMouseHook();
                if (hotkeyRegistered)
                {
                    try { UnregisterHotKey(Handle, HOTKEY_ID); } catch { }
                    hotkeyRegistered = false;
                }
            }
            base.Dispose(disposing);
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private static bool SystemIsDark()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object value = key != null ? key.GetValue("AppsUseLightTheme") : null;
                    if (value is int && (int)value == 0) return true;
                }
            }
            catch { }
            return false;
        }

        private void ApplyTheme(bool dark)
        {
            if (root == null || btnChat == null || drawer == null || inputBox == null || bottomArea == null) return; // WM_CREATE may land mid-ctor
            currentDark = dark;
            Ui.Dark = dark;
            Color bg = dark ? Color.FromArgb(24, 26, 32) : Color.White;
            BackColor = bg;
            root.BackColor = bg;
            foreach (Control c in root.Controls)
            {
                Panel p = c as Panel;
                if (p != null) p.BackColor = bg;
            }
            bottomArea.BackColor = bg;
            foreach (Control c in bottomArea.Controls)
            {
                Panel p = c as Panel;
                if (p != null)
                {
                    p.BackColor = bg;
                    foreach (Control cc in p.Controls)
                    {
                        Panel pp = cc as Panel;
                        if (pp != null) pp.BackColor = bg;
                    }
                }
            }
            hairline.BackColor = dark ? Color.FromArgb(44, 48, 58) : Color.FromArgb(229, 234, 246);
            inputBox.BackColor = dark ? Color.FromArgb(38, 42, 52) : Color.White;
            inputBox.ForeColor = dark ? Color.FromArgb(206, 212, 226) : Color.FromArgb(30, 35, 45);
            outputBox.BackColor = dark ? Color.FromArgb(38, 42, 52) : Color.FromArgb(250, 251, 253);
            outputBox.ForeColor = dark ? Color.FromArgb(206, 212, 226) : Color.FromArgb(30, 35, 45);
            copyFeedback.ForeColor = Color.FromArgb(34, 154, 88);
            foreach (Control c in root.Controls)
            {
                if (c != hairline && c.Height == 1)
                {
                    c.BackColor = dark ? Color.FromArgb(44, 48, 58) : Color.FromArgb(229, 234, 246);
                }
            }
            btnChat.Dark = dark;
            btnHarness.Dark = dark;
            btnRestart.Dark = dark;
            btnPlatform.Dark = dark;
            btnStop.Dark = dark;
            logoBox.Dark = dark;
            themeToggle.DarkTheme = dark;
            themeToggle.Invalidate();
            drawer.SetTheme(dark);
            balanceValue.ForeColor = dark ? Color.FromArgb(206, 212, 226) : Color.FromArgb(30, 35, 45);
            if (status.Text == "点击按钮，快速进入 DeepSeek")
            {
                status.ForeColor = dark ? Color.FromArgb(156, 166, 190) : Color.FromArgb(140, 148, 165);
            }
            if (IsHandleCreated)
            {
                int v = dark ? 1 : 0;
                try { DwmSetWindowAttribute(Handle, 20, ref v, 4); } catch { }
            }
            Invalidate(true);
        }

        private bool EffectiveDark()
        {
            if (forceDark) return true;
            if (forceLight) return false;
            if (themeMode == 1) return true;
            if (themeMode == 2) return false;
            return SystemIsDark();
        }

        private void SetThemeMode(int mode)
        {
            themeMode = mode;
            ApplyTheme(EffectiveDark());
        }

        private void ToggleTheme()
        {
            if (themeMode == 0)
            {
                // leaving follow-system: switch to the opposite of the current look
                themeMode = currentDark ? 2 : 1;
            }
            else
            {
                themeMode = themeMode == 1 ? 2 : 1;
            }
            ApplyTheme(EffectiveDark());
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyTheme(currentDark);
            if (!hotkeyRegistered)
            {
                try { hotkeyRegistered = RegisterHotKey(Handle, HOTKEY_ID, 0x0001 | 0x0002, (uint)Keys.T); } catch { }
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                TranslateClipboard();
                return;
            }
            base.WndProc(ref m);
            if (m.Msg == 0x001A) // WM_SETTINGCHANGE: theme switched
            {
                if (themeMode == 0)
                {
                    bool dark = SystemIsDark();
                    if (dark != currentDark) ApplyTheme(dark);
                }
            }
        }

        private static DateTime BeijingNow()
        {
            // Beijing time is UTC+8 with no DST.
            return DateTime.UtcNow.AddHours(8);
        }

        private static bool IsPeakHour(DateTime t)
        {
            int h = t.Hour;
            return (h >= 9 && h < 12) || (h >= 14 && h < 18);
        }

        private void UpdateTimeStatus()
        {
            DateTime now = BeijingNow();
            DateTime next;
            if (IsPeakHour(now))
            {
                timeStatus.Text = "当前为高峰时段";
                timeStatus.ForeColor = Color.FromArgb(217, 119, 6);
                // within peak, the idle boundary is 12:00 or 18:00
                next = new DateTime(now.Year, now.Month, now.Day, now.Hour < 12 ? 12 : 18, 0, 0);
                timeDetail.Text = string.Format("{0:HH}:00 进入空闲时段 · 剩余 {1}", next, FormatRemaining(next - now));
                timeDetail.ForeColor = Color.FromArgb(217, 119, 6);
            }
            else
            {
                timeStatus.Text = "当前为空闲时段";
                timeStatus.ForeColor = Color.FromArgb(22, 163, 74);
                // within idle, the next peak boundary is 9:00 / 14:00 / tomorrow 9:00
                if (now.Hour < 9) next = new DateTime(now.Year, now.Month, now.Day, 9, 0, 0);
                else if (now.Hour < 14) next = new DateTime(now.Year, now.Month, now.Day, 14, 0, 0);
                else next = new DateTime(now.Year, now.Month, now.Day, 9, 0, 0).AddDays(1);
                timeDetail.Text = string.Format("{0:HH}:00 进入高峰时段 · 剩余 {1}", next, FormatRemaining(next - now));
                timeDetail.ForeColor = Color.FromArgb(22, 163, 74);
            }
        }

        private static string FormatRemaining(TimeSpan span)
        {
            int minutes = Math.Max(0, (int)Math.Ceiling(span.TotalMinutes));
            int h = minutes / 60;
            int m = minutes % 60;
            if (h > 0) return string.Format("{0} 小时 {1} 分", h, m);
            return string.Format("{0} 分", m);
        }

        private static bool ContainsCjk(string text)
        {
            int cjk = 0;
            foreach (char ch in text)
            {
                if (ch >= 0x2E80 && ch <= 0x9FFF) cjk++;
            }
            return cjk > 0 && (double)cjk / Math.Max(1, text.Length) >= 0.12;
        }

        private void TranslateClipboard()
        {
            if (translating) return;
            string text = "";
            try { text = Clipboard.GetText(); } catch { }
            if (string.IsNullOrWhiteSpace(text))
            {
                ShowToast("提示", "剪贴板为空，请先复制要翻译的文本。", currentDark);
                return;
            }
            text = text.Trim();
            if (text.Length > 6000) text = text.Substring(0, 6000);
            StartTranslate(text, ResolveTarget(text), false);
        }

        private void OnTranslateClick()
        {
            if (translating) return;
            string text = inputBox.Text.Trim();
            if (text.Length == 0)
            {
                // input box empty -> translate the clipboard instead
                TranslateClipboard();
                return;
            }
            if (text.Length > 6000) text = text.Substring(0, 6000);
            StartTranslate(text, ResolveTarget(text), true);
        }

        private string ResolveTarget(string text)
        {
            if (translateDir == 1) return "英文";
            if (translateDir == 2) return "中文";
            return ContainsCjk(text) ? "英文" : "中文";
        }

        private void StartTranslate(string text, string target, bool inputMode)
        {
            if (translating) return;
            if (string.IsNullOrEmpty(currentToken))
            {
                ShowToast("提示", "未配置 API Key：请在余额卡点「设置」填入。", currentDark);
                return;
            }
            translating = true;
            TranslateAsync(text, target, inputMode);
        }

        private PillButton MakeDirPill(string text, int dir, int x)
        {
            PillButton p = new PillButton();
            p.Text = text;
            p.SetBounds(Ui.S(x), Ui.S(3), Ui.S(46), Ui.S(24));
            p.Click += delegate
            {
                translateDir = dir;
                RefreshDirPills();
            };
            return p;
        }

        private void RefreshDirPills()
        {
            btnDirAuto.Selected = translateDir == 0;
            btnDirEn.Selected = translateDir == 1;
            btnDirZh.Selected = translateDir == 2;
            btnDirAuto.Invalidate();
            btnDirEn.Invalidate();
            btnDirZh.Invalidate();
        }

        private async void TranslateAsync(string text, string target, bool inputMode)
        {
            string result = null;
            try
            {
                var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
                var payload = new Dictionary<string, object>();
                payload["model"] = "deepseek-chat";
                var messages = new List<object>();
                messages.Add(new Dictionary<string, object>
                {
                    { "role", "system" },
                    { "content", "你是一个专业的翻译引擎。只输出翻译结果，不要任何解释、注释或额外内容。" }
                });
                messages.Add(new Dictionary<string, object>
                {
                    { "role", "user" },
                    { "content", "把下面的文本翻译成" + target + "：\n" + text }
                });
                payload["messages"] = messages;
                payload["temperature"] = 0.3;
                payload["max_tokens"] = 2048;
                byte[] body = Encoding.UTF8.GetBytes(ser.Serialize(payload));

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.deepseek.com/chat/completions");
                req.Method = "POST";
                req.ContentType = "application/json; charset=utf-8";
                req.Headers["Authorization"] = "Bearer " + currentToken;
                req.Timeout = 25000;
                using (Stream s = await req.GetRequestStreamAsync()) s.Write(body, 0, body.Length);
                using (WebResponse resp = await req.GetResponseAsync())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string json = await sr.ReadToEndAsync();
                    var o = ser.Deserialize<Dictionary<string, object>>(json);
                    var choices = o["choices"] as System.Collections.ArrayList;
                    if (choices != null && choices.Count > 0)
                    {
                        var c0 = choices[0] as Dictionary<string, object>;
                        if (c0 != null)
                        {
                            var msg = c0["message"] as Dictionary<string, object>;
                            if (msg != null) result = Convert.ToString(msg["content"]).Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                translating = false;
                ShowToast("翻译失败", ex.Message, currentDark);
                return;
            }
            translating = false;
            if (string.IsNullOrEmpty(result))
            {
                ShowToast("翻译失败", "API 未返回有效结果，请稍后重试。", currentDark);
                return;
            }
            AddHistory(text, result);
            if (inputMode)
            {
                outputBox.Text = result;
                if (translateTest)
                {
                    try { File.WriteAllText("D:\\DSH_start\\translate-test-input.log", result); } catch { }
                }
                else
                {
                    try { Clipboard.SetText(result); } catch { }
                    ShowCopyFeedback();
                }
            }
            else
            {
                if (translateTest)
                {
                    try { File.WriteAllText("D:\\DSH_start\\translate-test.log", result); } catch { }
                    return;
                }
                try { Clipboard.SetText(result); } catch { }
                ShowToast("翻译结果", result, currentDark);
            }
        }

        private void ShowToast(string title, string body, bool dark)
        {
            if (toast != null && !toast.IsDisposed) toast.Close();
            toast = new ToastForm(title, body, dark);
            toast.Show();
        }

        private void OnFormResize()
        {
            // keep the fixed-position controls pinned to the right edge as the window grows
            if (balanceArea == null || bottomArea == null) return;
            int bw = balanceArea.Width;
            btnRefresh.Location = new Point(bw - Ui.S(174), Ui.S(26));
            btnSetToken.Location = new Point(bw - Ui.S(118), Ui.S(26));
            btnRecharge.Location = new Point(bw - Ui.S(62), Ui.S(26));
            int tw = bottomArea.Width;
            inputBox.Width = tw - Ui.S(28);
            outputBox.Width = tw - Ui.S(28);
            btnDoTranslate.Location = new Point(tw - Ui.S(270), Ui.S(108));
            btnCopy.Location = new Point(tw - Ui.S(206), Ui.S(108));
            btnClear.Location = new Point(tw - Ui.S(142), Ui.S(108));
            btnHistory.Location = new Point(tw - Ui.S(78), Ui.S(108));
        }

        private static void DumpControls(Control c, StringBuilder sb, int depth)
        {
            string pad = new string(' ', depth * 2);
            sb.AppendLine(pad + c.GetType().Name + " [" + c.Text + "] " + c.Bounds + " vis=" + c.Visible + " dock=" + c.Dock);
            foreach (Control ch in c.Controls) DumpControls(ch, sb, depth + 1);
        }

        private void ShowCopyFeedback()
        {
            copyFeedback.Text = "✓ 已复制";
            feedbackTimer.Stop();
            feedbackTimer.Start();
        }

        private void LoadHistory()
        {
            try
            {
                if (File.Exists(historyPath))
                {
                    var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
                    var arr = ser.Deserialize<System.Collections.ArrayList>(File.ReadAllText(historyPath));
                    if (arr != null)
                    {
                        foreach (object o in arr)
                        {
                            var d = o as Dictionary<string, object>;
                            if (d != null) history.Add(d);
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveHistory()
        {
            try
            {
                var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
                File.WriteAllText(historyPath, ser.Serialize(history));
            }
            catch { }
        }

        private void AddHistory(string src, string dst)
        {
            try
            {
                var d = new Dictionary<string, object>();
                d["src"] = src.Length > 200 ? src.Substring(0, 200) : src;
                d["dst"] = dst.Length > 500 ? dst.Substring(0, 500) : dst;
                d["time"] = DateTime.Now.ToString("MM-dd HH:mm");
                history.Insert(0, d);
                while (history.Count > 100) history.RemoveAt(history.Count - 1);
                SaveHistory();
            }
            catch { }
        }

        private void ShowHistory()
        {
            using (HistoryForm f = new HistoryForm(history))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    string dst = f.SelectedTranslation();
                    if (dst != null)
                    {
                        try { Clipboard.SetText(dst); } catch { }
                        ShowCopyFeedback();
                    }
                }
            }
        }

        private void LoadToken()
        {
            try
            {
                if (File.Exists(tokenPath))
                {
                    string t = File.ReadAllText(tokenPath).Trim();
                    if (t.Length > 0)
                    {
                        currentToken = t;
                        return;
                    }
                }
            }
            catch { }
            // fall back to the harness credentials file (~/.dsh/.credentials.yaml)
            try
            {
                string cred = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".dsh", ".credentials.yaml");
                if (File.Exists(cred))
                {
                    foreach (string line in File.ReadAllLines(cred))
                    {
                        int start = line.IndexOf("sk-");
                        if (start < 0) continue;
                        string rest = line.Substring(start + 3);
                        int end = rest.Length;
                        for (int k = 0; k < rest.Length; k++)
                        {
                            char ch = rest[k];
                            if (ch == '"' || ch == '\'' || ch == ',' || ch == ' ' || ch == '\t' ||
                                ch == '\r' || ch == '\n' || ch == ':' || ch == '#' || ch == '}')
                            {
                                end = k;
                                break;
                            }
                        }
                        string t = "sk-" + rest.Substring(0, end);
                        if (t.Length > 6)
                        {
                            currentToken = t;
                            return;
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveToken(string token)
        {
            currentToken = token;
            try { File.WriteAllText(tokenPath, token); } catch { }
        }

        private void ShowPriceTable()
        {
            using (PriceForm f = new PriceForm(currentDark, IsPeakNow()))
            {
                f.ShowDialog(this);
            }
        }

        /// <summary>In-app recharge: embedded WebView2 showing the Platform top-up page.</summary>
        private async void ShowRecharge()
        {
            if (webView == null)
            {
                webView = new Microsoft.Web.WebView2.WinForms.WebView2();
                webView.Dock = DockStyle.Fill;
                webView.Visible = false;

                rechargeBar = new Panel();
                rechargeBar.Dock = DockStyle.Top;
                rechargeBar.Height = Ui.S(34);
                rechargeBar.BackColor = currentDark ? Color.FromArgb(24, 26, 32) : Color.White;
                rechargeBar.Visible = false;

                Button back = new Button();
                back.Text = "← 返回";
                back.FlatStyle = FlatStyle.Flat;
                back.SetBounds(Ui.S(6), Ui.S(3), Ui.S(80), Ui.S(28));
                back.Click += delegate { HideRecharge(); };
                rechargeBar.Controls.Add(back);

                Label title = new Label();
                title.Text = "DeepSeek Platform 充值";
                title.ForeColor = currentDark ? Color.FromArgb(206, 212, 226) : Color.FromArgb(30, 35, 45);
                title.SetBounds(Ui.S(96), Ui.S(7), Ui.S(400), Ui.S(20));
                rechargeBar.Controls.Add(title);

                Controls.Add(webView);
                Controls.Add(rechargeBar);
                webView.BringToFront();
                rechargeBar.BringToFront();

                try
                {
                    string dataDir = Path.Combine(Ui.DataDir(), "WebView2");
                    var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, dataDir);
                    await webView.EnsureCoreWebView2Async(env);
                    webView.Source = new Uri("https://platform.deepseek.com/top_up");
                }
                catch
                {
                    // WebView2 runtime unavailable: fall back to the default browser
                    webView.Visible = false;
                    rechargeBar.Visible = false;
                    OpenUrl("https://platform.deepseek.com/top_up");
                    return;
                }
            }
            preRechargeSize = Size;
            Size = new Size(Ui.S(680), Ui.S(520));
            CenterToScreen();
            webView.Visible = true;
            rechargeBar.Visible = true;
            webView.BringToFront();
            rechargeBar.BringToFront();
        }

        private void HideRecharge()
        {
            webView.Visible = false;
            rechargeBar.Visible = false;
            Size = preRechargeSize;
            CenterToScreen();
        }

        /// <summary>Beijing-time peak window: 09:00-12:00 and 14:00-18:00.</summary>
        private static bool IsPeakNow()
        {
            DateTime bj = DateTime.UtcNow.AddHours(8);
            int mins = bj.Hour * 60 + bj.Minute;
            return (mins >= 540 && mins < 720) || (mins >= 840 && mins < 1080);
        }

        private void ShowTokenDialog()
        {
            using (TokenDialog dlg = new TokenDialog(currentToken))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string t = dlg.TokenValue;
                    if (t.Length == 0)
                    {
                        balanceSub.Text = "Token 不能为空";
                        balanceSub.ForeColor = Color.FromArgb(200, 60, 60);
                        return;
                    }
                    SaveToken(t);
                    RefreshBalance();
                }
            }
        }

        private async void RefreshBalance()
        {
            if (balanceBusy) return;
            if (string.IsNullOrEmpty(currentToken))
            {
                balanceValue.Text = "—";
                balanceSub.Text = "未设置 Token，点「设置」";
                balanceSub.ForeColor = Color.FromArgb(150, 158, 175);
                return;
            }
            balanceBusy = true;
            balanceSub.Text = "获取中…";
            balanceSub.ForeColor = Color.FromArgb(150, 158, 175);
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.deepseek.com/user/balance");
                req.Method = "GET";
                req.Headers["Authorization"] = "Bearer " + currentToken;
                req.Timeout = 12000;
                using (WebResponse resp = await req.GetResponseAsync())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                {
                    string json = await sr.ReadToEndAsync();
                    var ser = new System.Web.Script.Serialization.JavaScriptSerializer();
                    var obj = ser.Deserialize<Dictionary<string, object>>(json);
                    bool available = obj != null && obj.ContainsKey("is_available") && Convert.ToBoolean(obj["is_available"]);
                    string text = "";
                    if (obj != null && obj.ContainsKey("balance_infos"))
                    {
                        var infos = obj["balance_infos"] as System.Collections.ArrayList;
                        if (infos != null)
                        {
                            foreach (object item in infos)
                            {
                                var d = item as Dictionary<string, object>;
                                if (d == null) continue;
                                string cur = d.ContainsKey("currency") ? Convert.ToString(d["currency"]) : "";
                                string total = d.ContainsKey("total_balance") ? Convert.ToString(d["total_balance"]) : "";
                                string sym = cur == "CNY" ? "¥" : cur == "USD" ? "$" : cur + " ";
                                if (text.Length > 0) text += " · ";
                                text += sym + " " + total;
                            }
                        }
                    }
                    balanceValue.Text = text.Length > 0 ? text : "—";
                    balanceSub.Text = available ? "可用" : "不可用";
                    balanceSub.ForeColor = available ? Color.FromArgb(34, 154, 88) : Color.FromArgb(200, 60, 60);
                }
            }
            catch (WebException ex)
            {
                int code = 0;
                HttpWebResponse hr = ex.Response as HttpWebResponse;
                if (hr != null) code = (int)hr.StatusCode;
                if (code == 401) { balanceSub.Text = "Token 无效（401）"; balanceSub.ForeColor = Color.FromArgb(200, 60, 60); }
                else if (code == 402) { balanceSub.Text = "余额不可用（402）"; balanceSub.ForeColor = Color.FromArgb(200, 60, 60); }
                else { balanceSub.Text = "获取失败：" + ex.Message; balanceSub.ForeColor = Color.FromArgb(200, 60, 60); }
            }
            catch (Exception ex)
            {
                balanceSub.Text = "获取失败：" + ex.Message;
                balanceSub.ForeColor = Color.FromArgb(200, 60, 60);
            }
            finally
            {
                balanceBusy = false;
            }
        }

        private void UpdateHarnessState()
        {
            bool running = PortOpen();
            btnHarness.Text = running ? "打开 Harness 界面" : "启动 DeepSeek Harness";
            btnRestart.Enabled = running;
            btnStop.Enabled = running;
            if (running && !polling && !stopping &&
                (status.Text == "点击按钮，快速进入 DeepSeek" || status.Text == "✓ DeepSeek Harness 已关闭"))
            {
                status.Text = "✓ DeepSeek Harness 正在运行";
                status.ForeColor = Color.FromArgb(34, 154, 88);
            }
            else if (!running && !polling && !stopping && status.Text == "✓ DeepSeek Harness 正在运行")
            {
                status.Text = "点击按钮，快速进入 DeepSeek";
                status.ForeColor = Color.FromArgb(140, 148, 165);
            }
        }

        private void OpenOrStartHarness()
        {
            if (PortOpen())
            {
                // server already running: just reopen the web UI in the browser
                status.Text = "✓ 已在浏览器打开 Harness 界面";
                status.ForeColor = Color.FromArgb(34, 154, 88);
                OpenUrl("http://127.0.0.1:3080");
                return;
            }
            StartHarnessAfterStop();
        }

        private void RestartHarness()
        {
            // 1) close the browser window showing the harness (best effort)
            CloseHarnessBrowser();
            // 2) stop the terminal process; when the port is free, auto-start again
            status.Text = "正在重启 DeepSeek Harness…";
            status.ForeColor = Color.FromArgb(217, 119, 6);
            restartAfterStop = true;
            try
            {
                Process.Start(new ProcessStartInfo("powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + Path.Combine(Application.StartupPath, "DSH-Web-Stop.ps1") + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch
            {
                status.Text = "重启失败：找不到停止脚本";
                status.ForeColor = Color.FromArgb(200, 60, 60);
                restartAfterStop = false;
                return;
            }
            stopping = true;
            polling = true;
            deadline = DateTime.UtcNow.AddSeconds(15);
            poll.Start();
        }

        private void StartHarnessAfterStop()
        {
            status.Text = "正在启动 DeepSeek Harness，浏览器将自动打开…";
            status.ForeColor = Color.FromArgb(77, 107, 254);
            try
            {
                Process.Start(new ProcessStartInfo("powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + Path.Combine(Application.StartupPath, "DSH-Web-Launcher.ps1") + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch
            {
                status.Text = "重启失败：找不到启动脚本";
                status.ForeColor = Color.FromArgb(200, 60, 60);
                polling = false;
                poll.Stop();
                return;
            }
            polling = true;
            deadline = DateTime.UtcNow.AddMinutes(3);
            poll.Start();
        }

        private static void CloseHarnessBrowser()
        {
            // Best effort: ask browsers whose window title mentions the harness
            // to close (WM_CLOSE), so the restart opens a fresh tab.
            try
            {
                string[] browsers = { "msedge", "chrome", "firefox", "opera", "brave", "msedgewebview2" };
                foreach (Process p in Process.GetProcesses())
                {
                    if (Array.IndexOf(browsers, p.ProcessName) < 0) continue;
                    try
                    {
                        if (p.MainWindowHandle != IntPtr.Zero &&
                            p.MainWindowTitle.IndexOf("Harness", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            PostMessage(p.MainWindowHandle, 0x0010, IntPtr.Zero, IntPtr.Zero); // WM_CLOSE
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void StopHarness()
        {
            if (!PortOpen())
            {
                status.Text = "DeepSeek Harness 未在运行";
                status.ForeColor = Color.FromArgb(140, 148, 165);
                return;
            }
            status.Text = "正在关闭 DeepSeek Harness…";
            status.ForeColor = Color.FromArgb(200, 60, 60);
            try
            {
                Process.Start(new ProcessStartInfo("powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + Path.Combine(Application.StartupPath, "DSH-Web-Stop.ps1") + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch
            {
                status.Text = "关闭失败：找不到停止脚本";
                status.ForeColor = Color.FromArgb(200, 60, 60);
                return;
            }
            stopping = true;
            polling = true;
            deadline = DateTime.UtcNow.AddSeconds(15);
            poll.Start();
        }

        private void OnPollTick()
        {
            if (!polling)
            {
                poll.Stop();
                return;
            }
            if (stopping)
            {
                if (!PortOpen())
                {
                    stopping = false;
                    if (restartAfterStop)
                    {
                        restartAfterStop = false;
                        StartHarnessAfterStop();
                        return;
                    }
                    polling = false;
                    poll.Stop();
                    status.Text = "✓ DeepSeek Harness 已关闭";
                    status.ForeColor = Color.FromArgb(140, 148, 165);
                }
                else if (DateTime.UtcNow > deadline)
                {
                    stopping = false;
                    restartAfterStop = false;
                    polling = false;
                    poll.Stop();
                    status.Text = "关闭超时，请检查进程或重启电脑";
                    status.ForeColor = Color.FromArgb(200, 60, 60);
                }
                return;
            }
            if (PortOpen())
            {
                polling = false;
                poll.Stop();
                status.Text = "✓ DeepSeek Harness 已运行 · 浏览器已打开";
                status.ForeColor = Color.FromArgb(34, 154, 88);
            }
            else if (DateTime.UtcNow > deadline)
            {
                polling = false;
                poll.Stop();
                status.Text = "启动超时，请查看 “DSH Web” 窗口中的日志";
                status.ForeColor = Color.FromArgb(200, 60, 60);
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return docked; }
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            if (docked)
            {
                // dragging the popped-out window away from the top exits drawer mode
                Rectangle wa = Screen.FromControl(this).WorkingArea;
                if (expanded && animState == 0 && Top > wa.Top + 40) ExitDrawerMode();
            }
        }

        private void InstallMouseHook()
        {
            if (mouseHook != IntPtr.Zero) return;
            try
            {
                hookProc = MouseHookProc;
                mouseHook = SetWindowsHookEx(14 /*WH_MOUSE_LL*/, hookProc, GetModuleHandle(null), 0);
            }
            catch { }
        }

        private void UninstallMouseHook()
        {
            if (mouseHook == IntPtr.Zero) return;
            try { UnhookWindowsHookEx(mouseHook); } catch { }
            mouseHook = IntPtr.Zero;
        }

        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam.ToInt64() == 0x0201 /*WM_LBUTTONDOWN*/)
            {
                int pos = GetMessagePos();
                int x = (short)(pos & 0xFFFF);
                int y = (short)((pos >> 16) & 0xFFFF);
                if (docked && expanded && animState == 0 &&
                    (DateTime.UtcNow - expandedAt).TotalMilliseconds > 600)
                {
                    Point p = new Point(x, y);
                    if (!Bounds.Contains(p)) Collapse();
                }
            }
            return CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        private void OnDockPoll()
        {
            // hover the whale -> pop out instantly, no hold time
            if (!docked || animState != 0 || expanded) return;
            if (!drawer.Visible) return;
            if (drawer.IsDragging) return;
            if (DateTime.UtcNow < drawer.SuppressHoverUntil) return;
            Rectangle hot = drawer.Bounds;
            hot.Inflate(12, 12);
            if (hot.Contains(Cursor.Position)) PopOut();
        }

        private void ToggleDrawer()
        {
            if (!docked)
            {
                EnterDrawerMode();
            }
            else if (expanded && animState == 0)
            {
                Collapse();
            }
        }

        private void EnterDrawerMode()
        {
            if (docked) return;
            docked = true;
            lastSize = Size;
            expanded = false;
            animState = 0;
            animTimer.Stop();
            dockX = Left + Width / 2;
            drawer.SetTheme(currentDark);
            drawer.SetOpacity(opacityLevel);
            drawer.Location = new Point(dockX - drawer.Width / 2, Screen.FromControl(this).WorkingArea.Top);
            drawer.Show();
            drawer.BringToFront();
            drawer.Size = new Size(Ui.S(96), Ui.S(44)); // re-assert: first Show may apply AutoScale
            drawer.ClientSize = new Size(Ui.S(96), Ui.S(44));
            Hide();
            InstallMouseHook();
            pollTimer.Start();
        }

        private void ExitDrawerMode()
        {
            if (!docked) return;
            docked = false;
            expanded = false;
            animState = 0;
            animTimer.Stop();
            pollTimer.Stop();
            UninstallMouseHook();
            drawer.Hide();
            TopMost = pinEnabled;
            Size = lastSize;
            Show();
            Location = new Point(dockX - Width / 2, Screen.FromControl(this).WorkingArea.Top + Ui.S(24));
        }

        private void PopOut()
        {
            if (!docked || expanded || animState != 0) return;
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            drawer.Hide();
            // pop out above the drawer's current position
            dockX = drawer.Left + drawer.Width / 2;
            Show();
            TopMost = true;
            Opacity = opacityLevel;
            animState = 1;
            animTarget = wa.Top;
            Location = new Point(dockX - Width / 2, wa.Top - Height);
            animTimer.Start();
        }

        private void Collapse()
        {
            if (!docked || !expanded || animState != 0) return;
            // the drawer lands under the window's current position
            // (dragging the popped window horizontally repositions the whale)
            dockX = Left + Width / 2;
            animState = 2;
            animTarget = Screen.FromControl(this).WorkingArea.Top - Height;
            animTimer.Start();
        }

        private void OnAnimTick()
        {
            if (animState == 1)
            {
                int y = Location.Y + 32;
                if (y >= animTarget)
                {
                    y = animTarget;
                    animTimer.Stop();
                    animState = 0;
                    expanded = true;
                    expandedAt = DateTime.UtcNow;
                }
                Location = new Point(Location.X, y);
            }
            else if (animState == 2)
            {
                int y = Location.Y - 32;
                if (y <= animTarget)
                {
                    y = animTarget;
                    animTimer.Stop();
                    animState = 0;
                    expanded = false;
                    Hide();
                    drawer.Show();
                    drawer.BringToFront();
                }
                Location = new Point(Location.X, y);
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                status.Text = "无法打开：" + url;
                status.ForeColor = Color.FromArgb(200, 60, 60);
            }
        }

        private static RoundButton MakeButton(string text)
        {
            RoundButton b = new RoundButton();
            b.Text = text;
            b.Dock = DockStyle.Top;
            b.Height = Ui.S(36);
            b.TabStop = false;
            return b;
        }

        private static Panel MakeSpacer()
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Top;
            p.Height = Ui.S(5);
            p.BackColor = Color.White;
            return p;
        }

        private static bool PortOpen()
        {
            try
            {
                using (TcpClient c = new TcpClient())
                {
                    IAsyncResult ar = c.BeginConnect("127.0.0.1", 3080, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(600)) return false;
                    c.EndConnect(ar);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
