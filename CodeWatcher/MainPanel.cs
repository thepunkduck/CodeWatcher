﻿using ChartLib;
using ClipperLib;
using Syncfusion.Windows.Forms.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;

namespace CodeWatcher
{
    [Flags]
    public enum MainArea
    {
        NONE = 0,
        PROJECT_COLUMN = 1,
        PROJECT_LABEL = 2,
        PROJECT_ROW = 4,
        PRESENT_AREA = 8,

        DAYBOX = 32,
        DAYINFO = 64,
        MAIN = 128,
        TOPHEADER = 512,

        SELECTED_WHOLE = 8192,
        SELECTED_START = 16384,
        SELECTED_END = 32768,
    }

    public enum TimePeriod
    {
        [Description("1 minute")]
        ONEMINUTE = 1,
        [Description("5 minutes")]
        FIVEMINUTES = 5,
        [Description("1 hour")]
        ONEHOUR = 60,
        [Description("2 hours")]
        TWOHOURS = 120,
        [Description("8 hours")]
        EIGHTHOURS = 480,
        [Description("1 day")]
        ONEDAY = 1440,
        [Description("1 week")]
        ONEWEEK = 10080,
        [Description("1 month")]
        ONEMONTH = 43200,
        [Description("3 months")]
        THREEMONTHS = 129600,
        [Description("6 months")]
        SIXMONTHS = 259200,
        [Description("1 year")]
        ONEYEAR = 525600,
        [Description("All time")]
        ALLTIME = -1,
    }

    class MainPanel
    {
        readonly FileChangeWatcher _fcWatcher;
        DoubleBuffer _doubleBuffer;
        readonly ThemeColors _theme;
        readonly Font _font;
        readonly TooltipContainer _ttC;
        bool _toPresent = true;
        DateTime _dt0;
        DateTime _dt1;
        int _iDaySpan = 30;
        double _dDaySpan = 30;
        int _rowHeight;
        int _jOffset, _maxJOffset = 10;
        int _scrollDays;
        private Point[] ptsLeftTri = new Point[] { new Point(0, 0), new Point(0, -10), new Point(10, -10), };
        private Point[] ptsRightTri = new Point[] { new Point(0, 0), new Point(0, -10), new Point(-10, -10), };
        const int BORDER = 10;
        const int TOPHEADINGHEIGHT = 60;
        const int PROJCOLWIDTH = 100;
        const int INFOCOLUMNWIDTH = 70;
        const int MINBOXW = 2;
        const int MINGAPPEDBOXSPACE = 7;
        const int TINYSPACE = 2;
        const int FUTCOLWIDTH = 20;
        const int ROWHEIGHTMAX = 100;
        const int ROWHEIGHTINC = 3;
        const int THINBANNERHEIGHT = 4;

        Brush _transLightenBr;

        private Brush txtBgBrush, txtFgBrush;

        private Pen txtUlPen, txtBrPen;
        private Pen transLitePen1;
        private Pen transLitePen2;
        private Pen transDarkPen1;
        private Pen transDarkPen2;
        //TextBox tBox;
        readonly StatusStrip _statusStrip;
        StatusStripLabel _label1;
        StatusStripLabel _label2;
        private bool _showInfoColumn = true;
        private bool _showIdleLine = false;

        private SortButton sortButton;


        // private string _toCode(MainArea.PROJECT_COLUMN) = "projColumn";
        // private string CODE_PROJLABEL = "projLabel";
        // private string CODE_PROJROW = "projRow";
        // private string CODE_FUTLABEL = "futLabel";
        // private string CODE_TIMEHEADER = "timeHead";
        // private string CODE_DAYBOX = "dayBox";
        // private string CODE_DAYINFO = "dayInfo";
        // // private string CODE_SORT = "sort";


        // Brush zoneREDbr;
        // Brush zoneORANGEbr;
        // Brush zoneYELLOWbr;
        // Brush zoneGREENbr;
        // Brush zoneBLUEbr;

        Brush _getToneBrush(Color c) { return (new SolidBrush(ThemeColors.LimitColor(c))); }

        string _toCode(MainArea ma) { return ma.ToString(); }


        public MainPanel(FileChangeWatcher fcw, DoubleBuffer doubleBuffer, ThemeColors theme)
        {
            _fcWatcher = fcw;
            _doubleBuffer = doubleBuffer;
            _theme = theme;
            _font = _doubleBuffer.Font;

            // zoneREDbr = _getToneBrush(Color.Red);
            // zoneORANGEbr = _getToneBrush(Color.Orange);
            // zoneYELLOWbr = _getToneBrush(Color.Yellow);
            // zoneGREENbr = _getToneBrush(Color.ForestGreen);
            // zoneBLUEbr = _getToneBrush(Color.DeepSkyBlue);
            txtBgBrush = new SolidBrush(Color.FromArgb(20, 20, 20));
            txtFgBrush = new SolidBrush(Color.FromArgb(230, 230, 230));
            txtUlPen = new Pen(Color.FromArgb(80, 80, 80));
            txtBrPen = new Pen(Color.FromArgb(180, 180, 180));

            transLitePen1 = new Pen(Color.FromArgb(150, Color.White));
            transLitePen2 = new Pen(Color.FromArgb(50, Color.White));
            transDarkPen1 = new Pen(Color.FromArgb(50, Color.Black));
            transDarkPen2 = new Pen(Color.FromArgb(150, Color.Black));

            _doubleBuffer.MouseWheel += DoubleBuffer1_MouseWheel;
            _doubleBuffer.MouseMove += _doubleBuffer_MouseMove;
            _doubleBuffer.MouseDown += _doubleBuffer_MouseDown;
            _doubleBuffer.MouseUp += _doubleBuffer_MouseUp;
            _doubleBuffer.MouseDoubleClick += _doubleBuffer_MouseDoubleClick;
            _doubleBuffer.PaintEvent += _doubleBuffer_PaintEvent;
            _ttC = new TooltipContainer(_doubleBuffer);
            MidnightNotifier.DayChanged += (s, e) => { _doubleBuffer.Invalidate(); };// make sure it ticks over to next day

            _statusStrip = new StatusStrip();
            _statusStrip.Dock = DockStyle.Bottom;
            StatusStripLabel label0 = new StatusStripLabel();
            label0.Text = @"Position:";
            label0.Font = new Font(label0.Font, FontStyle.Bold);
            _label1 = new StatusStripLabel();
            StatusStripLabel label3 = new StatusStripLabel();
            label3.Text = @"Total Work:";
            label3.Font = new Font(label3.Font, FontStyle.Bold);
            _label2 = new StatusStripLabel();
            _label1.AutoSize = false;
            _label1.Size = new Size(200, _label1.Height);
            _label1.TextAlign = ContentAlignment.MiddleLeft;
            _label2.AutoSize = false;
            _label2.Size = new Size(200, _label2.Height);
            _label2.TextAlign = ContentAlignment.MiddleLeft;

            _statusStrip.Items.Add(label0);
            _statusStrip.Items.Add(_label1);
            _statusStrip.Items.Add(label3);
            _statusStrip.Items.Add(_label2);
            doubleBuffer.Parent.Controls.Add(_statusStrip);

            sortButton = new SortButton();


            sortButton.MouseClick += SortButton_MouseClick;
            sortButton.Theme = _theme;
            doubleBuffer.Parent.Controls.Add(sortButton);
            sortButton.BringToFront();


            _rowHeight = _font.Height * 2;
        }


        private void SortButton_MouseClick(object sender, MouseEventArgs e)
        {
            _fcWatcher.SortProjectsBy = sortButton.SortBy;
            _doubleBuffer.Refresh();
        }

        public void IncrementDayStart(int clicks)
        {
            _incDayStart(clicks * _scrollDays);
            _doubleBuffer.Refresh();
        }

        internal void IncrementRowHeight(int clicks)
        {
            _incRowHeight(clicks * ROWHEIGHTINC);
            _doubleBuffer.Refresh();
        }

        internal void IncrementRow(int clicks)
        {
            _incJoffset(clicks);
            _doubleBuffer.Refresh();
        }
        internal void ShowPresentToLast(TimePeriod timePeriod)
        {
            switch (timePeriod)
            {
                case TimePeriod.ONEWEEK:
                    _jOffset = 0;
                    _toPresent = true;
                    DaySpan = 7;
                    _doubleBuffer.Refresh();
                    break;
                case TimePeriod.ONEMONTH:
                    _jOffset = 0;
                    _toPresent = true;
                    DaySpan = FileChangeTable.InclusiveDaySpan(DateTime.Now.AddMonths(-1), DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
                case TimePeriod.THREEMONTHS:
                    _jOffset = 0;
                    _toPresent = true;
                    DaySpan = FileChangeTable.InclusiveDaySpan(DateTime.Now.AddMonths(-3), DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
                case TimePeriod.SIXMONTHS:
                    _jOffset = 0;
                    _toPresent = true;
                    DaySpan = FileChangeTable.InclusiveDaySpan(DateTime.Now.AddMonths(-6), DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
                case TimePeriod.ONEYEAR:
                    _jOffset = 0;
                    _toPresent = true;
                    DaySpan = FileChangeTable.InclusiveDaySpan(DateTime.Now.AddYears(-1), DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
                case TimePeriod.ALLTIME:
                    _jOffset = 0;
                    _toPresent = true;
                    DaySpan = FileChangeTable.InclusiveDaySpan(_fcWatcher.Table.StartTime, DateTime.Now);
                    _doubleBuffer.Refresh();
                    break;
            }
        }

        internal void ScrollToTop()
        {
            _jOffset = 0;
            _doubleBuffer.Refresh();
        }

        internal void SaveSettings()
        {
            Properties.Settings.Default.MinDateTime = _dt0;
            Properties.Settings.Default.MaxDateTime = _dt1;
            Properties.Settings.Default.ToPresent = _toPresent;
            Properties.Settings.Default.Joffset = _jOffset;
            Properties.Settings.Default.RowHeight = _rowHeight;
            Properties.Settings.Default.ShowInfoColumn = _showInfoColumn;
            Properties.Settings.Default.ShowIdleLine = _showIdleLine;
            Properties.Settings.Default.Save();
        }

        internal void LoadSettings()
        {
            _dt0 = Properties.Settings.Default.MinDateTime;
            _dt1 = Properties.Settings.Default.MaxDateTime;
            _toPresent = Properties.Settings.Default.ToPresent;
            _jOffset = Properties.Settings.Default.Joffset;
            _rowHeight = Properties.Settings.Default.RowHeight;
            _showInfoColumn = Properties.Settings.Default.ShowInfoColumn;
            _showIdleLine = Properties.Settings.Default.ShowIdleLine;
            _fcWatcher.SortProjectsBy = sortButton.SortBy;
        }

        private DateTime _dateTimeAtPtr(int x)
        {
            try
            {
                double d = (x - _leftDemark) / _boxSpace;
                return (_dt0.Date.AddDays(d));
            }
            catch (Exception)
            {
                return (_dt0);
            }
        }
        private int _screenxAtDateTime(DateTime dt)
        {
            int sx = (int)((dt - _dt0.Date).TotalDays * _boxSpace + _leftDemark + 0.5 - _boxOff);
            return (sx);
        }

        private Rectangle _mainClipRect;
        private int _leftDemark;
        private float _boxSpace = 10;
        private float _boxOff;

        private void _doubleBuffer_PaintEvent(object sender, PaintEventArgs e)
        {
            try
            {
                MetalTemperatureBrushes.SetRange(0.0, 1.0);
                _transLightenBr = new SolidBrush(Color.FromArgb(50, Color.Gainsboro));
                Graphics g = e.Graphics;
                int width = e.ClipRectangle.Width;
                int height = e.ClipRectangle.Height;
                _ttC.Clear();

                g.Clear(_theme.Window.Background.Color);
                FileChangeTable table;
                if (_fcWatcher == null ||
                _fcWatcher.Table == null ||
                (table = _fcWatcher.Table) == null ||
               table.ItemCount == 0)
                {
                    g.DrawString("NO DATA!", _font, Brushes.Red, BORDER, TOPHEADINGHEIGHT);
                    return;
                }

                DateTime today = DateTime.Now.Date;
                // day range
                if (_toPresent) _dt1 = today;
                _dt0 = _calcT0(_dt1);

                int fH = _font.Height;
                _leftDemark = BORDER + PROJCOLWIDTH + (_showInfoColumn ? INFOCOLUMNWIDTH : 0);
                int playWidth = (width - _leftDemark - FUTCOLWIDTH);
                _boxSpace = playWidth / (DaySpan);
                int boxW = (int)(_boxSpace > MINGAPPEDBOXSPACE ? (_boxSpace - TINYSPACE * 2) : _boxSpace);
                if (boxW < MINBOXW) boxW = MINBOXW;//not too small, always visible
                int boxCenterOff = boxW / 2;
                _boxOff = (_boxSpace - boxW) / 2;
                _scrollDays = Math.Max(1, (int)(50 / _boxSpace));

                bool showDayOfWeek = _boxSpace > fH;
                bool showDayOfMonth = _boxSpace > fH;
                bool showMondays = _boxSpace * 7 > fH * 2;
                bool showDayVerticals = _boxSpace > fH;
                bool showFullBanner = _rowHeight > fH * 2;
                int boxH = showFullBanner ? _rowHeight - fH - 2 : _rowHeight - THINBANNERHEIGHT;
                _mainClipRect = new Rectangle(_leftDemark, 0, width - _leftDemark, height);

                Rectangle mainR = new Rectangle(_leftDemark, TOPHEADINGHEIGHT, width - _leftDemark, height - TOPHEADINGHEIGHT);
                _ttC.Add(_toCode(MainArea.MAIN), mainR, null, null);
                Rectangle topR = new Rectangle(0, 0, width, TOPHEADINGHEIGHT);
                _ttC.Add(_toCode(MainArea.TOPHEADER), topR, null, null);

                // projects with activity in shown time range
                int peakOpsInDay = table.GetPeakOpsInDay();


                int playHeight = (height - TOPHEADINGHEIGHT);
                _maxJOffset = table.CountProjects(DataState.True, DataState.Ignore) - (playHeight / _rowHeight);
                if (_maxJOffset < 0) _maxJOffset = 0;
                _incJoffset(0);

                Brush fgBr = _theme.Window.Foreground.Brush;
                Brush bgBr = _theme.Window.Background.Brush;
                Brush bgGrayBr = _theme.Window.Low.Brush;
                Brush mo1Br = _theme.AccentWindow1.High.Brush;
                Brush mo2Br = _theme.AccentWindow2.High.Brush;
                Brush mo1CBr = _theme.AccentWindow1.High.ContrastBrush;
                Brush mo2CBr = _theme.AccentWindow2.High.ContrastBrush;
                Brush weekendBrushA = _theme.AccentWindow1.Low.Brush;
                Brush weekendBrushB = _theme.AccentWindow1.VLow.Brush;
                Brush hiliteBr = _theme.Highlight.Foreground.Brush;
                bool presentDayShown = false;

                sortButton.SortBy = _fcWatcher.SortProjectsBy;
                var loc = _doubleBuffer.Location;
                sortButton.Size = new Size(fH, fH);
                sortButton.Location = new Point(loc.X + PROJCOLWIDTH + BORDER - sortButton.Width, loc.Y + TOPHEADINGHEIGHT - sortButton.Height);

                // draw
                g.DrawString("Project", _font, fgBr, BORDER, TOPHEADINGHEIGHT - fH);
                if (_showInfoColumn)
                {
                    g.DrawString("wd:hr:mn", _font, fgBr, BORDER + BORDER + PROJCOLWIDTH, TOPHEADINGHEIGHT - 2 * fH);
                    g.DrawString("edit count", _font, fgBr, BORDER + BORDER + PROJCOLWIDTH, TOPHEADINGHEIGHT - fH);
                }

                _ttC.Add(_toCode(MainArea.PROJECT_COLUMN), new Rectangle(0, 0, _leftDemark, height), null, this);

                Rectangle? pDayInfoRect = null;
                List<float> verts = new List<float>();
                List<float> dayVerts = new List<float>();
                int yMonth = TOPHEADINGHEIGHT;
                // row per project
                var y = TOPHEADINGHEIGHT;
                int ic = 0;
                int idxJ = -1;
                foreach (var proj in table.EachVisibleProject())
                {
                    idxJ++;
                    if (idxJ < _jOffset) continue;
                    if (y > height) break;

                    ic++;
                    float fx = BORDER;
                    Rectangle rectBg = new Rectangle(0, y, width, _rowHeight);
                    Brush dayBrush;
                    Brush weekendBrush;
                    if (idxJ % 2 == 1) { dayBrush = bgGrayBr; weekendBrush = weekendBrushA; }
                    else { dayBrush = bgBr; weekendBrush = weekendBrushB; }
                    g.FillRectangle(dayBrush, rectBg);

                    var py = y + (_rowHeight - _font.Height) / 2;
                    var clipRect = rectBg;
                    clipRect.Width = _leftDemark;
                    g.SetClip(clipRect);
                    g.FillRectangle(proj.Brush, clipRect);


                    g.SetClip(clipRect);
                    if (_showInfoColumn)
                    {
                        var infoRect = clipRect;
                        infoRect.X = clipRect.Right - INFOCOLUMNWIDTH;
                        infoRect.Width = INFOCOLUMNWIDTH;
                        infoRect.Inflate(0, -2);
                        g.FillRectangle(_transLightenBr, infoRect);

                        //g.DrawString(proj.ActivityTrace.Summary, _font, proj.ContrastBrush, infoRect.X + BORDER, py);
                        g.DrawString(proj.EditCount.ToString() + " [" + proj.SelectedEditCount + "]",
                            _font, proj.ContrastBrush, infoRect.X + BORDER, py + fH);
                    }

                    if (proj.Selected)
                    {
                        Rectangle selRect = clipRect;
                        selRect.Width = 4;
                        selRect.Height--;
                        g.FillRectangle(hiliteBr, selRect);
                    }



                    Rectangle textRect = clipRect;
                    textRect.X = (int)fx;
                    textRect.Y = py;
                    textRect.Width = PROJCOLWIDTH - BORDER - BORDER;
                    textRect.Height = fH;
                    textRect.Inflate(0, 2);
                    g.ResetClip();
                    g.SetClip(textRect);
                    _drawInsetBorderRectangle(g, textRect);
                    g.DrawString(proj.Name, _font, txtFgBrush, (int)fx, py);

                    g.ResetClip();
                    _drawShadedTopBottom(g, clipRect);



                    Rectangle pRect = rectBg;
                    pRect.Width = _leftDemark;
                    _ttC.Add(_toCode(MainArea.PROJECT_ROW), rectBg, null, proj);
                    _ttC.Add(_toCode(MainArea.PROJECT_LABEL), pRect, proj.Path, proj);

                    fx = _leftDemark;
                    int icDay = -1;
                    int year = -999;

                    g.SetClip(_mainClipRect);
                    // iterate thru days
                    foreach (DateTime date in FileChangeTable.EachDay(_dt0, _dt1))
                    {
                        var pDay = proj.GetDay(date);
                        if (date == today) presentDayShown = true;

                        icDay++;
                        if (fx > width) break;
                        if (ic == 1)
                        {
                            int tY = (int)(y - 1.5 * fH);
                            int boxCenter = (int)fx + boxCenterOff;
                            if (showDayOfWeek) { _drawCenteredString(g, date.ToString("ddd").Substring(0, 1), _font, fgBr, boxCenter, tY); tY -= fH; }

                            if (showDayOfMonth || (icDay == 0 && showMondays)) { _drawCenteredString(g, date.Day.ToString(), _font, fgBr, boxCenter, tY); tY -= fH; }
                            else if (showMondays)
                            {
                                if (date.DayOfWeek == DayOfWeek.Monday)
                                    _drawCenteredString(g, date.Day.ToString(), _font, fgBr, boxCenter, tY);
                                tY -= fH;
                            }

                            if (icDay == 0 || date.Day == 1) //MONTH
                            {
                                yMonth = tY + fH;
                                g.FillRectangle(date.Month % 2 == 0 ? mo1Br : mo2Br, (int)(fx - _boxOff + 0.5), tY, width - fx, fH);
                                g.DrawString(date.ToString("MMMM"), _font, date.Month % 2 == 0 ? mo1CBr : mo2CBr, fx, tY); tY -= fH;
                            }

                            if (year != date.Year) //YEAR
                            {
                                year = date.Year;
                                g.DrawString(date.Year.ToString(), _font, fgBr, fx, tY);
                            }
                            // START OF WEEK
                            if (date.DayOfWeek == DayOfWeek.Monday ||
                                date.DayOfWeek == DayOfWeek.Saturday)
                                verts.Add(fx - _boxOff);

                            if (showDayVerticals) dayVerts.Add(fx - _boxOff);
                        }

                        if (date.DayOfWeek == DayOfWeek.Saturday ||
                            date.DayOfWeek == DayOfWeek.Sunday)
                        {
                            Rectangle weRect = rectBg;
                            weRect.X = (int)(fx - _boxOff + 0.5);
                            weRect.Width = (int)(_boxSpace + 1);
                            g.FillRectangle(weekendBrush, weRect);
                        }

                        // DAYBOX
                        if (pDay != null && pDay.Count > 0)
                        {
                            double activity = ((double)pDay.Count) / peakOpsInDay;
                            int bH = boxH;
                            Rectangle rect = new Rectangle((int)fx, y + _rowHeight - bH, boxW, bH);
                            Brush br = MetalTemperatureBrushes.GetBrush(activity);
                            g.FillRectangle(br, rect);
                            _ttC.Add(_toCode(MainArea.DAYBOX), rect, null, pDay);
                            if (pDay == _pDayInfoDay) pDayInfoRect = rect;
                        }


                        fx += _boxSpace;
                    }



                    // ACTIVITY TRACE
                    if (_boxSpace > fH)
                    {
                        int edW = _screenxAtDateTime(_dt0.AddMinutes(ActivityTraceBuilder.PerEditMinutes)) - _screenxAtDateTime(_dt0);
                        if (edW < 2) edW = 2;

                        foreach (var fci in proj.Collection)
                        {
                            if (fci.ChangeType.HasAny(ActionTypes.UserIdle | ActionTypes.Suspend)) continue;
                            int sx0 = _screenxAtDateTime(fci.DateTime);
                            g.FillRectangle(_theme.Window.Medium.Brush, sx0, y + _rowHeight - boxH, edW, boxH);
                        }
                        // REDLINES
                        if (_showIdleLine)
                            foreach (var fci in proj.Collection)
                            {
                                if (!fci.ChangeType.HasAny(ActionTypes.UserIdle | ActionTypes.Suspend)) continue;
                                int sx0 = _screenxAtDateTime(fci.DateTime);
                                g.FillRectangle(Brushes.Red, sx0, y + _rowHeight - boxH, 1, boxH);
                            }
                    }


                    g.ResetClip();
                    y += _rowHeight;
                }// end proj loop



                if (table.SelectionState)
                {
                    int SELBARH = 6;
                    int SELHANDLEW = 15;
                    int SELHANDLEOFF = 5;
                    Color clr = _theme.Window.Foreground.Color;
                    Brush brTrans = new SolidBrush(Color.FromArgb(40, clr));
                    Brush brBdr = new SolidBrush(Color.FromArgb(230, clr));
                    int sx0 = _screenxAtDateTime((DateTime)table.SelectionStartDate);
                    int sx1 = _screenxAtDateTime((DateTime)table.SelectionEndDate);
                    Rectangle rect = new Rectangle(sx0, TOPHEADINGHEIGHT, sx1 - sx0, height);

                    Rectangle mainSideR = mainR;
                    mainSideR.Y = 0;
                    mainSideR.Height = height;
                    g.SetClip(mainSideR);

                    g.FillRectangle(brTrans, rect);
                    Rectangle rectBrd = rect;
                    rectBrd.Height = SELBARH;
                    rectBrd.Y = TOPHEADINGHEIGHT - rectBrd.Height;
                    g.FillRectangle(brBdr, rectBrd);
                    g.TranslateTransform(rectBrd.X, rectBrd.Bottom);
                    g.FillPolygon(brTrans, ptsLeftTri);
                    g.DrawPolygon(_theme.Window.Medium.Pen, ptsLeftTri);
                    g.ResetTransform();
                    g.TranslateTransform(rectBrd.Right, rectBrd.Bottom);
                    g.FillPolygon(brTrans, ptsRightTri);
                    g.DrawPolygon(_theme.Window.Medium.Pen, ptsRightTri);
                    g.ResetTransform();
                    rect.Y = 0;
                    rect.Height = TOPHEADINGHEIGHT;
                    _ttC.Add(_toCode(MainArea.SELECTED_WHOLE), rect, null, null);
                    Rectangle rectEdge = rect;
                    rectEdge.X = rect.Left - SELHANDLEOFF;
                    rectEdge.Width = SELHANDLEW;
                    _ttC.Add(_toCode(MainArea.SELECTED_START), rectEdge, null, null);
                    rectEdge.X = rect.Right - SELHANDLEW + SELHANDLEOFF;
                    _ttC.Add(_toCode(MainArea.SELECTED_END), rectEdge, null, null);

                    foreach (var abBlock in table.Activity.Collection)
                    {
                        sx0 = _screenxAtDateTime(abBlock.StartDate);
                        sx1 = _screenxAtDateTime(abBlock.EndDate);
                        int aW = Math.Max(sx1 - sx0, 2);
                        g.FillRectangle(_theme.Highlight.Medium.Brush, sx0, TOPHEADINGHEIGHT - SELBARH+1, aW,
                            SELBARH-2);
                    }
                    g.ResetClip();
                }




                Rectangle futRect = new Rectangle(_doubleBuffer.Width - FUTCOLWIDTH, 0, FUTCOLWIDTH, _doubleBuffer.Height);
                _ttC.Add(_toCode(MainArea.PRESENT_AREA), futRect, "Scroll to present", null);
                Brush presBr = new SolidBrush(Color.FromArgb(100, _theme.Window.Medium.Color));
                g.FillRectangle(presBr, futRect);


                _label2.Text = table.Activity.Summary;

                // horiz
                g.DrawLine(_theme.Window.Medium.Pen, 0, TOPHEADINGHEIGHT, width, TOPHEADINGHEIGHT);
                // vert
                g.DrawLine(_theme.Window.High.Pen2, _leftDemark, 0, _leftDemark, height);
                g.SetClip(_mainClipRect);
                foreach (var vx in dayVerts) g.DrawLine(_theme.VeryTranslucentPen, vx, yMonth, vx, height);
                foreach (var vx in verts) g.DrawLine(_theme.TranslucentPen, vx, yMonth, vx, height);
                g.ResetClip();

                _drawDayInfo(g, pDayInfoRect);


                if (_ptrTrack != null)
                    g.DrawLine(_theme.TranslucentPen, ((Point)_ptrTrack).X, 0, ((Point)_ptrTrack).X, height);


                if (presentDayShown && _toPresent == false)
                {
                    _toPresent = true;
                    // wipe, go back to start!
                    _doubleBuffer_PaintEvent(sender, e);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void _drawShadedTopBottom(Graphics graphics, Rectangle rect)
        {
            graphics.DrawLine(transLitePen1, rect.Left, rect.Top, rect.Right, rect.Top);
            graphics.DrawLine(transLitePen2, rect.Left, rect.Top + 1, rect.Right, rect.Top + 1);
            graphics.DrawLine(transDarkPen2, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
            graphics.DrawLine(transDarkPen1, rect.Left, rect.Bottom - 2, rect.Right, rect.Bottom - 2);
        }

        private void _drawInsetBorderRectangle(Graphics graphics, Rectangle textRect)
        {
            graphics.FillRectangle(txtBgBrush, textRect);
            graphics.DrawRectangle(txtUlPen, textRect);
            textRect.Width += 4;
            textRect.Height += 4;
            textRect.X -= 5;
            textRect.Y -= 5;
            graphics.DrawRectangle(txtBrPen, textRect);
        }


        private string _niceDay(DateTime dateTime)
        {
            return (dateTime.ToString("ddd dd MMMM yyyy"));
        }

        internal void CopyWorkSummaryToClipboard()
        {
            string summary = _fcWatcher.Table.GetWorkSummary();
            Clipboard.SetText(summary);
        }

        internal void ShowInfoColumn(bool chk)
        {
            _showInfoColumn = chk;
            _doubleBuffer.Refresh();
        }

        public void ShowIdleLine(bool chk)
        {
            _showIdleLine = chk;
            _doubleBuffer.Refresh();
        }

        internal void RemoveTimeSelectionEdits()
        {
            var dR = MessageBox.Show("Remove edits in Time Selection?", "This will PERMANENTLY remove them! Are you sure?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (dR == DialogResult.Yes)
            {
                _fcWatcher.Table.RemoveTimeSelectionEdits();
                _doubleBuffer.Refresh();
            }
        }


        private void _drawCenteredString(Graphics g, string txt, Font font, Brush br, int x, int y)
        {
            var siz = g.MeasureString(txt, font);
            g.DrawString(txt, font, br, x - siz.Width / 2, y);
        }


        void _incJoffset(int inc)
        {
            _jOffset += inc;
            if (_jOffset < 0) _jOffset = 0;
            if (_jOffset > _maxJOffset) _jOffset = _maxJOffset;
        }


        void _incDayStart(int inc)
        {
            _toPresent = false;
            _dt0 = _dt0.AddDays(inc);

            if (_fcWatcher != null && _fcWatcher.Table != null)
            {
                var tmpT1 = _calcT1(_dt0);
                var today = DateTime.Now.Date;
                if (tmpT1 > today) tmpT1 = today;
                _dt0 = _calcT0(tmpT1);
            }

            _dt1 = _calcT1(_dt0);
        }


        void _incDaySpan(int inc)
        {
            DDaySpan = (int)(DDaySpan * Math.Pow(1.2, inc));
            if (!_toPresent) _dt1 = _calcT1(_dt0);
        }

        void _incRowHeight(int inc)
        {
            _rowHeight += inc;
            if (_rowHeight > ROWHEIGHTMAX) _rowHeight = ROWHEIGHTMAX;
            if (_rowHeight < _font.Height) _rowHeight = _font.Height;
        }
        double DDaySpan
        {
            get { return (_dDaySpan); }
            set
            {
                _dDaySpan = value; _iDaySpan = (int)Math.Round(_dDaySpan, MidpointRounding.AwayFromZero);
                if (DaySpan < 1) DaySpan = 1;
            }
        }
        int DaySpan
        { get { return (_iDaySpan); } set { _iDaySpan = value; if (_iDaySpan < 7) _iDaySpan = 7; _dDaySpan = _iDaySpan; } }

        DateTime _calcT0(DateTime dt1) { return (dt1.AddDays(-(DaySpan - 1))); }
        DateTime _calcT1(DateTime dt0) { return (dt0.AddDays(DaySpan - 1)); }


        private void _doubleBuffer_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.TOPHEADER)))
            {
                DateTime dtMin = _dt0.Date < _fcWatcher.Table.StartTime.Date ? _dt0.Date : _fcWatcher.Table.StartTime.Date;
                DateTime dtMax = _dt1.Date > _fcWatcher.Table.EndTime.Date ? _dt1.Date : _fcWatcher.Table.EndTime.Date;
                _fcWatcher.Table.SetTimeSelection(dtMin, dtMax.AddDays(1).Date, true);
                _doubleBuffer.Refresh();
            }
        }

        private DateTime? _dtHandleInit0;
        private DateTime? _dtHandleInit1;
        private DateTime _dtInitClick;
        private MainArea _dtMod = MainArea.NONE;
        private void _doubleBuffer_MouseDown(object sender, MouseEventArgs e)
        {
            if (_fcWatcher == null) return;

            // time selection:
            // select handle grabs and changes
            // no handle click starts selection
            // drag continues
            // + shift + whole select moves whole thing
            _dtHandleInit0 = _fcWatcher.Table.SelectionStartDate;
            _dtHandleInit1 = _fcWatcher.Table.SelectionEndDate;
            _dtInitClick = _dateTimeAtPtr(e.X);
            if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.SELECTED_START)))
            {
                _dtMod = MainArea.SELECTED_START;
                _doubleBuffer.Refresh();
            }
            else if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.SELECTED_END)))
            {
                _dtMod = MainArea.SELECTED_END;
                _doubleBuffer.Refresh();
            }
            else if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.SELECTED_WHOLE)) && ChartViewer.IsModifierHeld(Keys.Shift))
            {
                _dtMod = MainArea.SELECTED_WHOLE;
                _doubleBuffer.Refresh();

            }
            else if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.TOPHEADER)))
            {
                // no handle click STARTS selection
                _dtHandleInit0 = _dateTimeAtPtr(e.X);
                _dtHandleInit1 = _dateTimeAtPtr(e.X);
                _fcWatcher.Table.SetTimeSelection(_dtHandleInit0, _dtHandleInit1, true);
                _dtMod = MainArea.SELECTED_END;
                _doubleBuffer.Refresh();
            }


            FileChangeProject proj = _ttC.GetDataAtPointer(e, _toCode(MainArea.PROJECT_LABEL)) as FileChangeProject;


            // if only 1 selected and right clicked, select this one only
            if (e.Button == MouseButtons.Right)
            {
                int count = _fcWatcher.Table.CountProjects(DataState.True, DataState.True);
                if (proj != null && count <= 1)
                {
                    // force this one right-clicked to be selected
                    _fcWatcher.Table.SelectProject(proj, SelectionBehavior.SelectOnly);
                    _doubleBuffer.Refresh();
                    return;
                }
            }

            if (e.Button != MouseButtons.Left) return;

            if (proj != null)
            {
                if (ChartViewer.IsModifierHeld(Keys.Alt))
                {
                    // make project invisible
                    proj.Visible = false;
                    _fcWatcher.UpdateActivity();
                   _fcWatcher.FireEvent();
                    return;
                }
                else if (ChartViewer.IsModifierHeld(Keys.Shift))
                {
                    // append project selection, keep all just toggle this one
                    _fcWatcher.Table.SelectProject(proj, SelectionBehavior.AppendToggle);
                    _doubleBuffer.Refresh();
                    return;
                }
                else
                {
                    // if selected, and there are others selected, leave only this selected
                    // if no others selected toggle it
                    _fcWatcher.Table.SelectProject(proj,
                        e.Button == MouseButtons.Left
                            ? SelectionBehavior.UnselectOnToggle
                            : SelectionBehavior.UnselectToggle);
                    _doubleBuffer.Refresh();
                    return;
                }
            }


            if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.PROJECT_COLUMN)))
            {
                _fcWatcher.Table.SelectProject(null, SelectionBehavior.Unselect);
                _doubleBuffer.Refresh();
                return;
            }

            // view to present day
            if (_ttC.IsDataRegionAtPointer(e, _toCode(MainArea.PRESENT_AREA)))
            {
                var today = DateTime.Now.Date;
                _dt1 = today;
                _dt0 = _calcT0(today);
                _doubleBuffer.Refresh();
                return;
            }


            FileChangeDay pDay = _ttC.GetDataAtPointer(e, _toCode(MainArea.DAYINFO)) as FileChangeDay;
            if (pDay != null)
            {
                _setDayInfo(null);
                _doubleBuffer.Refresh();
                return;
            }

            pDay = _ttC.GetDataAtPointer(e, _toCode(MainArea.DAYBOX)) as FileChangeDay;
            if (pDay != null)
            {
                if (pDay == _pDayInfoDay) _setDayInfo(null);// toggle
                else _setDayInfo(pDay);
                _doubleBuffer.Refresh();
                return;
            }
        }

        private Point? _ptrTrack;

        private void _doubleBuffer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_fcWatcher == null) return;

            DateTime mDt0 = _dateTimeAtPtr(e.X);
            _label1.Text = mDt0.ToString(CultureInfo.InvariantCulture);

            Point? ptr = null;
            if (ChartViewer.IsModifierHeld(Keys.Shift)) ptr = e.Location;
            if (ptr != _ptrTrack)
            {
                _ptrTrack = ptr;
                _doubleBuffer.Refresh();
            }

            TimeSpan deltaOffset = mDt0 - _dtInitClick;
            switch (_dtMod)
            {
                case MainArea.NONE:
                    break;
                case MainArea.SELECTED_WHOLE:
                    _fcWatcher.Table.SetTimeSelection(_dtHandleInit0 + deltaOffset, _dtHandleInit1 + deltaOffset, true);
                    _doubleBuffer.Refresh();
                    break;
                case MainArea.SELECTED_START:
                    _fcWatcher.Table.SetTimeSelection(_dtHandleInit0 + deltaOffset, _dtHandleInit1, true);
                    _doubleBuffer.Refresh();
                    break;
                case MainArea.SELECTED_END:
                    _fcWatcher.Table.SetTimeSelection(_dtHandleInit0, _dtHandleInit1 + deltaOffset, true);
                    _doubleBuffer.Refresh();
                    break;
            }

        }

        private void _doubleBuffer_MouseUp(object sender, MouseEventArgs e)
        {
            bool refresh = false;

            if (_dtMod != MainArea.NONE)
            {
                _dtMod = MainArea.NONE;
                _fcWatcher.UpdateActivity();
                refresh = true;
            }

            if (refresh) _doubleBuffer.Refresh();
        }

        private string _dayInfo;
        private FileChangeDay _pDayInfoDay;
        private void _setDayInfo(FileChangeDay pDay)
        {
            _pDayInfoDay = pDay;

            if (_pDayInfoDay != null)
            {
                _dayInfo = pDay.Project.Name + Environment.NewLine +
                           _niceDay(pDay.DateTime) + Environment.NewLine +
                           Environment.NewLine +
                           "   Start: " + pDay.EstimatedStart.ToString("h:mm tt") + Environment.NewLine +
                           "     End: " + pDay.EstimatedEnd.ToString("h:mm tt") + Environment.NewLine +
                           "Duration: " + pDay.EstimatedDuration.ToString(@"hh\:mm") + Environment.NewLine +
                           "   Edits: " + pDay.EditCount + Environment.NewLine +
                           Environment.NewLine +
                           pDay.GetChangedFiles();

                // string[] lines = _dayInfo.Split(new[] { "\r\n", "\r", "\n" },StringSplitOptions.None);
                // int ic = 1;
                // _dayInfo = string.Join(Environment.NewLine, lines.Select(i => (ic++).ToString("000 ") + i).ToArray());


            }
            else
            {
                _dayInfo = null;
            }
        }

        private void _drawDayInfo(Graphics g, Rectangle? dayRectangle)
        {
            if (_pDayInfoDay == null) return;
            if (dayRectangle == null) return;

            int offset = _font.Height;
            Rectangle dRect = (Rectangle)dayRectangle;
            var matches = Regex.Matches(_dayInfo, Environment.NewLine);
            int lineCount = matches.Count + 1;

            // main area rectangle attempt to fit inside this:
            Rectangle smRect = _mainClipRect;
            smRect.Height -= TOPHEADINGHEIGHT;
            smRect.Y = TOPHEADINGHEIGHT;
            smRect.Inflate(-5, -5);

            for (int columns = 1; columns <= 4; columns++)
            {
                // try to fit it into columns
                int iLineStep = (int)Math.Round((double)lineCount / columns, MidpointRounding.AwayFromZero);
                List<string> strList = new List<string>();
                int colWidth = 0;
                int boxHeight = 0;
                int iLine1 = 0;
                int i0 = 0;
                for (int i = 0; i < columns; i++)
                {
                    iLine1 += iLineStep;
                    int i1 = iLine1 < lineCount ? matches[iLine1 - 1].Index : _dayInfo.Length - 1;

                    string strSub = _dayInfo.Substring(i0, i1 - i0 + 1).Trim();
                    strList.Add(strSub);

                    var siz = g.MeasureString(strSub, _font);
                    colWidth = Math.Max(colWidth, (int)(siz.Width + 0.5));
                    boxHeight = Math.Max(boxHeight, (int)(siz.Height + 0.5));
                    i0 = i1 + 1;
                }

                // multiply up widths
                int boxWidth = colWidth * columns + (columns - 1) * offset * 2;

                // try to fit
                Rectangle rect = new Rectangle(dRect.X + offset, dRect.Y + offset, boxWidth, boxHeight);
                rect.Inflate(2, 2);
                if (rect.Right > smRect.Right) rect.X = smRect.Right - rect.Width;
                if (rect.Left < smRect.Left) rect.X = smRect.Left;
                if (rect.Bottom > smRect.Bottom) rect.Y = smRect.Bottom - rect.Height;
                if (rect.Top < smRect.Top) rect.Y = smRect.Top;

                if (rect.Bottom <= smRect.Bottom)
                {
                    // border around
                    Paths clip1 = _pathFromRect(dRect);
                    Paths clip2 = _pathFromRect(Rectangle.Round(rect));
                    Paths union = new Paths();
                    Clipper c = new Clipper();
                    c.AddPaths(clip1, PolyType.ptSubject, true);
                    c.AddPaths(clip2, PolyType.ptClip, true);
                    c.Execute(ClipType.ctUnion, union, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
                    Paths solution = new Paths();
                    ClipperOffset co = new ClipperOffset();
                    co.AddPath(union[0], JoinType.jtRound, EndType.etClosedPolygon);
                    co.Execute(ref solution, 1.0);

                    g.DrawRectangle(_theme.Window.Medium.Pen, rect);
                    g.FillRectangle(_theme.Window.VLow.Brush, rect);
                    g.DrawRectangle(_theme.Window.Medium.Pen, rect);
                    solution.ForEach(poly => _drawPoly(g, _theme.Highlight.Foreground.Pen2, poly));

                    int x = rect.X + 2;
                    foreach (var subStr in strList)
                    {
                        g.DrawString(subStr, _font, _theme.Window.High.Brush, x, rect.Y + 3);
                        x += (colWidth + offset * 2);
                    }

                    //g.DrawString(lineCount.ToString(), _font, Brushes.Red, rect.X+100, rect.Y);
                    _ttC.Add(_toCode(MainArea.DAYINFO), rect, null, _pDayInfoDay);
                    break;
                }
            }

        }

        private void _drawPoly(Graphics g, Pen pen, Path poly)
        {
            if (poly.Count > 1)
                g.DrawPolygon(pen, poly.Select(pt => new Point((int)pt.X, (int)pt.Y)).ToArray());
        }

        private Paths _pathFromRect(Rectangle rect)
        {
            Paths clip = new Paths(1);
            clip.Add(new Path(4));
            clip[0].Add(new IntPoint(rect.Left, rect.Top));
            clip[0].Add(new IntPoint(rect.Left, rect.Bottom));
            clip[0].Add(new IntPoint(rect.Right, rect.Bottom));
            clip[0].Add(new IntPoint(rect.Right, rect.Top));
            return (clip);
        }

        private void DoubleBuffer1_MouseWheel(object sender, MouseEventArgs e)
        {
            int click = e.Delta / 120;

            if (ChartViewer.IsModifierHeld(Keys.Shift) && ChartViewer.IsModifierHeld(Keys.Control))
            {
                // day at mouse..
                double ptrProp = ((double)(e.X - _leftDemark)) / (_doubleBuffer.Width - _leftDemark - FUTCOLWIDTH);
                var ptrDate = _dt0.AddDays(ptrProp * DDaySpan);
                _incDaySpan(-click);
                // re-center
                _dt0 = ptrDate.AddDays(-(ptrProp * DDaySpan));
                _dt1 = _calcT1(_dt0);
                _toPresent = false;
                _doubleBuffer.Refresh();
            }
            else if (ChartViewer.IsModifierHeld(Keys.Control))
            {
                _incRowHeight(click * 3);
                _doubleBuffer.Refresh();
            }
            else if (ChartViewer.IsModifierHeld(Keys.Shift))
            {
                _incDayStart(_scrollDays * click);
                _doubleBuffer.Refresh();
            }
            else
            {
                _incJoffset(-click);
                _doubleBuffer.Refresh();
            }
        }

        public FileChangeProject GetProjectAtPointer()
        {
            var ptr = _doubleBuffer.PointToClient(Cursor.Position);
            FileChangeProject proj = _ttC.GetDataAtPointer(ptr, _toCode(MainArea.PROJECT_LABEL)) as FileChangeProject;
            return (proj);
        }

        readonly ColorForm colorForm = new ColorForm();
        public void EditColor()
        {
            int count = _fcWatcher.Table.CountProjects(DataState.True, DataState.True);
            if (count == 0) return;

            // get the first project onscreen that is selected
            DataRegion dReg = null;
            _fcWatcher.Table.ProjectCollection.ForEach(proj =>
            {
                if (dReg == null)
                    dReg = _ttC.GetDataRegionWithData(_toCode(MainArea.PROJECT_LABEL), proj);
            });
            FileChangeProject firstSelProject = _fcWatcher.Table.ProjectCollection.FirstOrDefault(proj => proj.Selected && proj.Visible);

            //var fProj = _fcWatcher.Table.ProjectCollection.FirstOrDefault(proj => proj.Selected && proj.Visible);
            var pt = dReg != null
                ? _doubleBuffer.PointToScreen(new Point(dReg.Rectangle.Right - 7, dReg.Rectangle.Top))
                : _doubleBuffer.PointToScreen(new Point(_leftDemark - 7, TOPHEADINGHEIGHT));

            colorForm.SelectedColor = count > 1 ? Color.Gray : firstSelProject.Color;
            colorForm.InfoText = count > 1 ? "multiple" : firstSelProject.Name;
            colorForm.Location = pt;
            _fcWatcher.Table.ProjectCollection.ForEach(proj =>
            {
                if (proj.Visible) colorForm.AppendUserColors(proj.Color);
            });

            var dR = colorForm.ShowDialog();

            if (dR == DialogResult.OK)
                _fcWatcher.Table.ProjectCollection.ForEach(proj =>
                {
                    if (proj.Selected && proj.Visible) proj.Color = colorForm.SelectedColor;
                });
            _doubleBuffer.Refresh();
        }

        public void RandomColors()
        {
            _fcWatcher.Table.ProjectCollection.ForEach(proj => { if (proj.Selected && proj.Visible) proj.RandomizeColor(); });
            _doubleBuffer.Refresh();
        }

        public void SameRandomColor()
        {
            var color = FileChangeProject.ColorRotator.Random();
            _fcWatcher.Table.ProjectCollection.ForEach(proj => { if (proj.Selected && proj.Visible) proj.Color = color; });
            _doubleBuffer.Refresh();
        }

        public MainArea GetAreasPointedAt()
        {
            var ptr = _doubleBuffer.PointToClient(Cursor.Position);

            MainArea areas = MainArea.NONE;
            _ttC.Collection.ForEach(dr =>
            {
                if (dr.Rectangle.Contains(ptr))
                {
                    MainArea tmp;
                    if (Enum.TryParse(dr.Code, out tmp)) areas |= tmp;
                }
            });

            return (areas);
        }

        public bool AnyAreasPointedAt(MainArea areaReq)
        {
            var ptr = _doubleBuffer.PointToClient(Cursor.Position);

            var dR = _ttC.Collection.FirstOrDefault(dr =>
            {
                MainArea tmp;
                return (dr.Rectangle.Contains(ptr) &&
                        Enum.TryParse(dr.Code, out tmp) &&
                        areaReq.HasFlag(tmp));
            });

            return (dR != null);
        }
    }

}
