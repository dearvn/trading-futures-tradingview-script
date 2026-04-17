// ESFuturesNoRepaintV15.cs
// NinjaTrader 8 indicator — ported from Pine Script es-futures-no-repaint-v1.5.txt
//
// Strategy: ES/SPX 1M scalping with multi-timeframe WMA confluence, VWMA Bollinger,
// SuperTrend (CCI-guided), T3 Tillson filter, pseudo Heikin-Ashi, and pivot breakout.
//
// Data series layout (AddDataSeries order):
//   BarsArray[0] = 1M  primary
//   BarsArray[1] = 3M
//   BarsArray[2] = 5M
//   BarsArray[3] = 8M
//   BarsArray[4] = 10M
//   BarsArray[5] = 11M  (wma_10_11, wma_8_48 proxies)
//   BarsArray[6] = 12M  (wma_10_48, wma_8_11 proxies)
//   BarsArray[7] = 60M  (daily low proxy)
//
// Trading hours: 06:30–12:30 PT (UTC-8).  Signals suppressed outside window.
// Signals: green ArrowUp = BUY, red ArrowDown = SELL.
//          orange Dot = CLOSE LONG, cyan Dot = CLOSE SHORT.

#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class ESFuturesNoRepaintV15 : Indicator
    {
        // ── SERIES (intermediate computed values, needed across bars) ──────────
        private Series<double> avgSeries;       // (open[1..10])/10
        private Series<double> avgChgSeries;    // avg absolute bar-to-bar change
        private Series<double> hkCloseSeries;   // pseudo-HA close
        private Series<double> hkOpenSeries;    // pseudo-HA open
        private Series<double> hkHighSeries;    // pseudo-HA high
        private Series<double> hkLowSeries;     // pseudo-HA low
        private Series<double> barUpSeries;     // 1.0 when bar_up (hk_c < hk_o)
        private Series<double> barDownSeries;
        private Series<double> ocSeries;        // o_c = 1M open (for WMA13/48/200)

        private Series<double> basisSeries;     // VWMA(open,11) 1M
        private Series<double> upper6Series;    // basis + 1.0*dev
        private Series<double> lower6Series;    // basis - 1.0*dev
        private Series<double> lower1Series;    // basis - 0.236*dev
        private Series<double> upper1Series;    // basis + 0.236*dev
        private Series<double> basis3Series;    // VWMA(open10m,50)
        private Series<double> uper10Series;    // (upper3_6 + basis3)/2
        private Series<double> lower10Series;   // (lower3_6 + basis3)/2

        private Series<double> wma13Series, wma48Series, wma200Series;

        private Series<double> cciSeries;
        private Series<double> stTslSeries;
        private Series<double> stTrendSeries;
        private Series<double> stTrendUpSeries, stTrendDownSeries;

        private Series<double> avg3mSeries, avg3m8Series;
        private Series<double> avg5mSeries, avg8mSeries, avg10mSeries;
        private Series<double> wma3m3Series, wma3m8Series;
        private Series<double> wma5m5Series, wma8m8Series, wma10m10Series;
        private Series<double> wma10_11Series, wma10_48Series;
        private Series<double> wma8_11Series, wma8_48Series;

        private Series<double> ctoPrevSeries;   // T3 Cto
        private Series<double> ema3Series;

        private Series<double> macdSeries, histSeries;
        private Series<double> macd1226Series, hist2Series, sig9Series;

        private Series<double> rsiSeries;       // RSI(hk_c, 5)
        private Series<double> rsi2Series;      // RSI(avg, 11) — Wilder's
        private Series<double> rsiMaSeries;     // SMA(rsi2, 24)

        private Series<double> wma3Series, wma11Series, wma21Series;

        private Series<double> ohSeries, olSeries;   // pivot hi/lo inputs
        private Series<double> lc9Series;             // tracks longCondition9 across bars

        // ── PERSISTENT STATE ─────────────────────────────────────────────────
        private bool isCrossDownBasis, isCrossUpBasis;
        private bool isCrossDownLower1, isCrossUpLower1;
        private bool isCrossDownRsi, isCrossUpRsi;
        private bool isCrossUpT3, isCrossDownT3;
        private bool isStrongBuy, isStrongSell;
        private bool isBuyST, isSellST;
        private bool isUpTrend, isDownTrend;
        private bool isLongCond11, isUp10m;
        private bool crossDownBasis;
        private int  trendUpLevel, trendDownLevel;
        private double runningPh = double.NaN;
        private double runningPl = double.NaN;
        private double callPrice, putPrice;

        // EMA/RMA running state
        private double ema5Val, ema13Val, ema12Val, ema26Val;
        private double ema8MVal, ema9MVal;
        private double rmaUpVal, rmaDnVal;      // for rsi2
        private double rsiRmaUp, rsiRmaDn;     // for rsi (on hk_c)
        private double ema3Val;
        private double t3_i1, t3_i2, t3_i3, t3_i4, t3_i5, t3_i6;
        private double stTrendUp, stTrendDown, stTrend;

        private bool emaInit, rsi2Init, rsiInit, ema3Init, t3Init;

        // ── PROPERTIES ───────────────────────────────────────────────────────
        [NinjaScriptProperty, Range(0.01, 5.0)]
        [Display(Name = "Stop Loss Call %", GroupName = "Risk/Reward", Order = 1)]
        public double StopPerCallPct { get; set; }

        [NinjaScriptProperty, Range(0.01, 5.0)]
        [Display(Name = "Take Profit Call %", GroupName = "Risk/Reward", Order = 2)]
        public double TakePerCallPct { get; set; }

        [NinjaScriptProperty, Range(0.01, 5.0)]
        [Display(Name = "Stop Loss Put %", GroupName = "Risk/Reward", Order = 3)]
        public double StopPerPutPct { get; set; }

        [NinjaScriptProperty, Range(0.01, 5.0)]
        [Display(Name = "Take Profit Put %", GroupName = "Risk/Reward", Order = 4)]
        public double TakePerPutPct { get; set; }

        [NinjaScriptProperty, Range(1, 100)]
        [Display(Name = "BB Length (1M)", GroupName = "Bollinger", Order = 5)]
        public int BbLength { get; set; }

        [NinjaScriptProperty, Range(0.1, 10.0)]
        [Display(Name = "BB Mult", GroupName = "Bollinger", Order = 6)]
        public double BbMult { get; set; }

        // ── STATE CHANGE ─────────────────────────────────────────────────────
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description              = "ES Futures 1M No Repaint V1.5 — NinjaTrader 8 port";
                Name                     = "ESFuturesNoRepaintV15";
                IsOverlay                = true;
                IsSuspendedWhileInactive = true;
                Calculate                = Calculate.OnBarClose;
                BarsRequiredToPlot       = 210;

                StopPerCallPct = 0.07;
                TakePerCallPct = 0.20;
                StopPerPutPct  = 0.07;
                TakePerPutPct  = 0.14;
                BbLength       = 11;
                BbMult         = 3.0;

                // Plots (Values[0..8])
                AddPlot(new Stroke(Brushes.Blue,   4), PlotStyle.Line, "Basis3");      // 0
                AddPlot(new Stroke(Brushes.White,  4), PlotStyle.Line, "Uper10");      // 1
                AddPlot(new Stroke(Brushes.Green,  4), PlotStyle.Line, "Lower10");     // 2
                AddPlot(new Stroke(Brushes.Yellow, 2), PlotStyle.Line, "Basis1M");     // 3
                AddPlot(new Stroke(Brushes.Red,    2), PlotStyle.Line, "Upper6");      // 4
                AddPlot(new Stroke(Brushes.Blue,   2), PlotStyle.Line, "Lower6");      // 5
                AddPlot(new Stroke(Brushes.Gray,   4), PlotStyle.Line, "Avg");         // 6
                AddPlot(new Stroke(Brushes.White,  1), PlotStyle.Line, "MACD1226");    // 7
                AddPlot(new Stroke(Brushes.Yellow, 1), PlotStyle.Line, "Signal9");     // 8
            }
            else if (State == State.Configure)
            {
                AddDataSeries(BarsPeriodType.Minute, 3);   // [1]
                AddDataSeries(BarsPeriodType.Minute, 5);   // [2]
                AddDataSeries(BarsPeriodType.Minute, 8);   // [3]
                AddDataSeries(BarsPeriodType.Minute, 10);  // [4]
                AddDataSeries(BarsPeriodType.Minute, 11);  // [5] proxy for 11M security calls
                AddDataSeries(BarsPeriodType.Minute, 12);  // [6] proxy for 12M security calls
                AddDataSeries(BarsPeriodType.Minute, 60);  // [7] low1d proxy

                avgSeries       = new Series<double>(this, MaximumBarsLookBack.Infinite);
                avgChgSeries    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                hkCloseSeries   = new Series<double>(this, MaximumBarsLookBack.Infinite);
                hkOpenSeries    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                hkHighSeries    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                hkLowSeries     = new Series<double>(this, MaximumBarsLookBack.Infinite);
                barUpSeries     = new Series<double>(this, MaximumBarsLookBack.Infinite);
                barDownSeries   = new Series<double>(this, MaximumBarsLookBack.Infinite);
                ocSeries        = new Series<double>(this, MaximumBarsLookBack.Infinite);
                basisSeries     = new Series<double>(this, MaximumBarsLookBack.Infinite);
                upper6Series    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                lower6Series    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                lower1Series    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                upper1Series    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                basis3Series    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                uper10Series    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                lower10Series   = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma13Series     = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma48Series     = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma200Series    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                cciSeries       = new Series<double>(this, MaximumBarsLookBack.Infinite);
                stTslSeries     = new Series<double>(this, MaximumBarsLookBack.Infinite);
                stTrendSeries   = new Series<double>(this, MaximumBarsLookBack.Infinite);
                stTrendUpSeries = new Series<double>(this, MaximumBarsLookBack.Infinite);
                stTrendDownSeries = new Series<double>(this, MaximumBarsLookBack.Infinite);
                avg3mSeries     = new Series<double>(this, MaximumBarsLookBack.Infinite);
                avg3m8Series    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                avg5mSeries     = new Series<double>(this, MaximumBarsLookBack.Infinite);
                avg8mSeries     = new Series<double>(this, MaximumBarsLookBack.Infinite);
                avg10mSeries    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma3m3Series    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma3m8Series    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma5m5Series    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma8m8Series    = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma10m10Series  = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma10_11Series  = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma10_48Series  = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma8_11Series   = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma8_48Series   = new Series<double>(this, MaximumBarsLookBack.Infinite);
                ctoPrevSeries   = new Series<double>(this, MaximumBarsLookBack.Infinite);
                ema3Series      = new Series<double>(this, MaximumBarsLookBack.Infinite);
                macdSeries      = new Series<double>(this, MaximumBarsLookBack.Infinite);
                histSeries      = new Series<double>(this, MaximumBarsLookBack.Infinite);
                macd1226Series  = new Series<double>(this, MaximumBarsLookBack.Infinite);
                hist2Series     = new Series<double>(this, MaximumBarsLookBack.Infinite);
                sig9Series      = new Series<double>(this, MaximumBarsLookBack.Infinite);
                rsiSeries       = new Series<double>(this, MaximumBarsLookBack.Infinite);
                rsi2Series      = new Series<double>(this, MaximumBarsLookBack.Infinite);
                rsiMaSeries     = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma3Series      = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma11Series     = new Series<double>(this, MaximumBarsLookBack.Infinite);
                wma21Series     = new Series<double>(this, MaximumBarsLookBack.Infinite);
                ohSeries        = new Series<double>(this, MaximumBarsLookBack.Infinite);
                olSeries        = new Series<double>(this, MaximumBarsLookBack.Infinite);
                lc9Series       = new Series<double>(this, MaximumBarsLookBack.Infinite);
            }
        }

        // ── MAIN BAR UPDATE ───────────────────────────────────────────────────
        protected override void OnBarUpdate()
        {
            if (BarsInProgress != 0) return;

            // Require enough bars across all series for accurate calculations
            if (CurrentBars[0] < 210 || CurrentBars[1] < 12 || CurrentBars[2] < 12 ||
                CurrentBars[3] < 12  || CurrentBars[4] < 55 || CurrentBars[5] < 12 ||
                CurrentBars[6] < 12  || CurrentBars[7] < 5)
                return;

            // ── 1M OPEN AVERAGE (10 confirmed bars, no open[0]) ───────────────
            double avg = 0;
            for (int i = 1; i <= 10; i++) avg += Opens[0][i];
            avg /= 10.0;
            avgSeries[0] = avg;

            double avgChange = 0;
            for (int i = 1; i <= 10; i++) avgChange += Math.Abs(Opens[0][i] - Opens[0][i + 1]);
            avgChange /= 10.0;
            avgChgSeries[0] = avgChange;

            // ── MTF AVERAGES (all start from [1] — confirmed bars only) ───────
            double avg3m  = (Opens[1][3] + Opens[1][2] + Opens[1][1]) / 3.0;
            double avg3m8 = 0; for (int i = 1; i <= 8; i++) avg3m8 += Opens[1][i]; avg3m8 /= 8.0;
            double avg5m  = 0; for (int i = 1; i <= 5; i++) avg5m  += Opens[2][i]; avg5m  /= 5.0;
            double avg8m  = 0; for (int i = 1; i <= 8; i++) avg8m  += Opens[3][i]; avg8m  /= 8.0;
            double avg10m = 0; for (int i = 1; i <= 10; i++) avg10m += Opens[4][i]; avg10m /= 10.0;

            avg3mSeries[0]  = avg3m;
            avg3m8Series[0] = avg3m8;
            avg5mSeries[0]  = avg5m;
            avg8mSeries[0]  = avg8m;
            avg10mSeries[0] = avg10m;

            // MTF WMAs (v1.5 fix: wma_5m_5 now uses avg_5m, not avg_3m)
            wma3m3Series[0]   = WmaOf(avg3mSeries,  3);
            wma3m8Series[0]   = WmaOf(avg3m8Series, 8);
            wma5m5Series[0]   = WmaOf(avg5mSeries, 11);   // was bugged in v1.3 (used 3m)
            wma8m8Series[0]   = WmaOf(avg8mSeries, 21);
            wma10m10Series[0] = WmaOf(avg10mSeries, 48);

            // Approximate 11M/12M security calls using 10M/8M averages
            wma10_11Series[0] = WmaOf(avg10mSeries, 4);
            wma10_48Series[0] = WmaOf(avg10mSeries, 9);
            wma8_11Series[0]  = WmaOf(avg8mSeries,  3);
            wma8_48Series[0]  = WmaOf(avg8mSeries,  8);

            double close10 = avg10m;
            double close8  = avg8m;
            bool up10m  = close10 > wma10_11Series[0];
            bool down8m = close8  < wma8_48Series[0];

            // ── 1M VWMA BOLLINGER BANDS ───────────────────────────────────────
            double basis  = Vwma1M(BbLength);
            double dev    = BbMult * Stdev1M(BbLength, basis);
            basisSeries[0]  = basis;
            upper6Series[0] = basis + dev;
            lower6Series[0] = basis - dev;
            lower1Series[0] = basis - 0.236 * dev;
            upper1Series[0] = basis + 0.236 * dev;

            // ── 10M VWMA BOLLINGER BANDS ──────────────────────────────────────
            double basis3  = Vwma10M(50);
            double dev3    = 3.0 * Stdev10M(50, basis3);
            double upper3_6 = basis3 + dev3;
            double lower3_6 = basis3 - dev3;
            basis3Series[0]  = basis3;
            uper10Series[0]  = (upper3_6 + basis3) / 2.0;
            lower10Series[0] = (lower3_6 + basis3) / 2.0;

            // ── PSEUDO HEIKIN-ASHI ────────────────────────────────────────────
            // o_c = open[0], o_o = open[1], o_h = max(open[3..1]), o_l = min(open[3..1])
            double o_c = Opens[0][0];
            double o_o = Opens[0][1];
            double o_h = Math.Max(Opens[0][3], Math.Max(Opens[0][2], Opens[0][1]));
            double o_l = Math.Min(Opens[0][3], Math.Min(Opens[0][2], Opens[0][1]));

            double hkClose = (o_h + o_l + o_o + o_c) / 4.0;
            double hkOpen  = (o_o + o_c) / 2.0;
            double hkHigh  = Math.Max(o_h, Math.Max(hkOpen, hkClose));
            double hkLow   = Math.Min(o_l, Math.Min(hkOpen, hkClose));
            bool bar_up    = hkClose < hkOpen;  // "bar_up" in Pine = HA close < HA open
            bool bar_down  = !bar_up;

            hkCloseSeries[0] = hkClose;
            hkOpenSeries[0]  = hkOpen;
            hkHighSeries[0]  = hkHigh;
            hkLowSeries[0]   = hkLow;
            barUpSeries[0]   = bar_up   ? 1.0 : 0.0;
            barDownSeries[0] = bar_down ? 1.0 : 0.0;
            ocSeries[0]      = o_c;

            // ochl approximate (used in is_br check — effectively always false, see note below)
            double ochl = (hkOpen + hkHigh + hkLow + hkClose) / 4.0;

            // ── WMA 13/48/200 on o_c (pseudo-HA close proxy = open) ──────────
            wma13Series[0]  = WmaOf(ocSeries, 13);
            wma48Series[0]  = WmaOf(ocSeries, 48);
            wma200Series[0] = WmaOf(ocSeries, 200);
            double wma13 = wma13Series[0], wma48 = wma48Series[0], wma200 = wma200Series[0];

            bool xUpWMA13   = ocSeries[1] < wma13Series[1] && o_c >= wma13;
            bool xDnWMA13   = ocSeries[1] > wma13Series[1] && o_c <= wma13;
            bool xUpWMA48   = ocSeries[1] < wma48Series[1] && o_c >= wma48;
            bool xDnWMA48   = ocSeries[1] > wma48Series[1] && o_c <= wma48;
            bool xUpWMA200  = ocSeries[1] < wma200Series[1] && o_c >= wma200;
            bool xDnWMA200  = ocSeries[1] > wma200Series[1] && o_c <= wma200;
            bool xUpWMA13_48   = wma13Series[1] < wma48Series[1]   && wma13 >= wma48;
            bool xDnWMA13_48   = wma13Series[1] > wma48Series[1]   && wma13 <= wma48;
            bool xUpWMA48_200  = wma48Series[1] < wma200Series[1]  && wma48 >= wma200;
            bool xDnWMA48_200  = wma48Series[1] > wma200Series[1]  && wma48 <= wma200;

            if ((xUpWMA13 || xUpWMA48 || xUpWMA200) && !isUpTrend)
                { isUpTrend = true;  isDownTrend = false; }
            if ((xDnWMA13 || xDnWMA48 || xDnWMA200) && !isDownTrend)
                { isUpTrend = false; isDownTrend = true;  }
            if (isUpTrend)
            {
                if (xUpWMA13_48)  { trendUpLevel = 1; trendDownLevel = 0; }
                if (xUpWMA48_200) { trendUpLevel = 2; trendDownLevel = 0; }
            }
            if (isDownTrend)
            {
                if (xDnWMA13_48)  { trendUpLevel = 0; trendDownLevel = 1; }
                if (xDnWMA48_200) { trendUpLevel = 0; trendDownLevel = 2; }
            }

            // ── CCI-GUIDED SUPERTREND ─────────────────────────────────────────
            double cciVal = CciOf(avgSeries, 28);
            cciSeries[0] = cciVal;

            double hl2  = (Highs[0][0] + Lows[0][0]) / 2.0;
            double atr3 = AtrOf(3);
            double stUp = hl2 - 3.0 * atr3;
            double stDn = hl2 + 3.0 * atr3;
            double prevCci = cciSeries[1];
            stTrendUp   = prevCci > 0 ? Math.Max(stUp, stTrendUp)   : stUp;
            stTrendDown = prevCci < 0 ? Math.Min(stDn, stTrendDown) : stDn;
            stTrend     = cciVal > 0 ? 1 : cciVal < 0 ? -1 : (stTrend == 0 ? 1 : stTrend);
            double stTsl = stTrend == 1 ? stTrendUp : stTrendDown;
            stTrendUpSeries[0]   = stTrendUp;
            stTrendDownSeries[0] = stTrendDown;
            stTrendSeries[0]     = stTrend;
            stTslSeries[0]       = stTsl;

            // buy_trend: average >= stTsl more recently than average < stTsl
            bool buyNow  = avgSeries[1] >= stTslSeries[1];
            bool sellNow = avgSeries[1] <  stTslSeries[1];
            if (sellNow && !isSellST) { isSellST = true;  isBuyST = false; }
            if (buyNow  && !isBuyST)  { isBuyST  = true;  isSellST = false; }
            bool buy_trend  = isBuyST;
            bool sell_trend = isSellST;

            // ── PIVOT HIGH / LOW (lb=5, rb=5) ────────────────────────────────
            double ohNow = Math.Max(Opens[0][5], Math.Max(Opens[0][4], Math.Max(Opens[0][3],
                           Math.Max(Opens[0][2], Math.Max(Opens[0][1], Opens[0][0])))));
            double olNow = Math.Min(Opens[0][4], Math.Min(Opens[0][3], Math.Min(Opens[0][2],
                           Math.Min(Opens[0][1], Opens[0][0]))));
            ohSeries[0] = ohNow;
            olSeries[0] = olNow;

            // Detect pivot when candidate at rb=5 is the local extremum
            if (CurrentBars[0] >= 11)
            {
                double phCand = ohSeries[5];
                bool isPH = true;
                for (int i = 0; i <= 10; i++) if (i != 5 && ohSeries[i] >= phCand) { isPH = false; break; }
                if (isPH) runningPh = phCand;

                double plCand = olSeries[5];
                bool isPL = true;
                for (int i = 0; i <= 10; i++) if (i != 5 && olSeries[i] <= plCand) { isPL = false; break; }
                if (isPL) runningPl = plCand;
            }

            // ── PERSISTENT BOLLINGER CROSS STATES ────────────────────────────
            if (avgSeries[1] > basisSeries[1] && avg <= basis)
                { isCrossDownBasis = true;  isCrossUpBasis  = false; }
            if (avgSeries[1] < basisSeries[1] && avg >= basis)
                { isCrossDownBasis = false; isCrossUpBasis  = true;  }
            if (avgSeries[1] > lower1Series[1] && avg <= lower1Series[0])
                { isCrossDownLower1 = true;  isCrossUpLower1 = false; }
            if (avgSeries[1] < lower1Series[1] && avg >= lower1Series[0])
                { isCrossDownLower1 = false; isCrossUpLower1 = true;  }

            if (ocSeries[1] > lower6Series[1] && o_c <= lower6Series[0] && avgSeries[1] > avg)
                crossDownBasis = false;
            if (avgSeries[1] > basisSeries[1] && avg <= basis)
                crossDownBasis = true;
            if (avgSeries[1] < basisSeries[1] && avg >= basis)
                crossDownBasis = false;

            // ── STRONG BUY/SELL (pivot breakout) ─────────────────────────────
            if (!double.IsNaN(runningPh))
            {
                if (avgSeries[1] < runningPh && avg >= runningPh) { isStrongBuy = true; isStrongSell = false; }
                if (avgSeries[1] > runningPh && avg <  runningPh)   isStrongBuy = false;
            }
            if (!double.IsNaN(runningPl))
            {
                if (avgSeries[1] > runningPl && avg <= runningPl) { isStrongSell = true; isStrongBuy = false; }
                if (avgSeries[1] < runningPl && avg >= runningPl)   isStrongSell = false;
            }

            // ── T3 TILLSON FILTER (on avg) ────────────────────────────────────
            // di=3.5, alpha=0.4; coefficients from Pine Script
            const double c1 = 2.0 / (3.5 + 1.0);
            const double c2 = 1.0 - c1;
            const double c3 = 3.0 * (0.4 * 0.4 + 0.4 * 0.4 * 0.4);         // 0.672
            const double c4 = -3.0 * (2.0 * 0.4 * 0.4 + 0.4 + 0.4 * 0.4 * 0.4); // -1.272
            const double c5 = 3.0 * 0.4 + 1.0 + 0.4 * 0.4 * 0.4 + 3.0 * 0.4 * 0.4; // 1.664
            if (!t3Init) { t3_i1=t3_i2=t3_i3=t3_i4=t3_i5=t3_i6=avg; t3Init=true; }
            t3_i1 = c1 * avg  + c2 * t3_i1;
            t3_i2 = c1 * t3_i1 + c2 * t3_i2;
            t3_i3 = c1 * t3_i2 + c2 * t3_i3;
            t3_i4 = c1 * t3_i3 + c2 * t3_i4;
            t3_i5 = c1 * t3_i4 + c2 * t3_i5;
            t3_i6 = c1 * t3_i5 + c2 * t3_i6;
            double Cto = -0.4*0.4*0.4*t3_i6 + c3*t3_i5 + c4*t3_i4 + c5*t3_i3;
            ctoPrevSeries[0] = Cto;

            const double ema3alpha = 2.0 / (3.0 + 1.0);
            if (!ema3Init) { ema3Val = avg; ema3Init = true; }
            ema3Val = ema3alpha * avg + (1.0 - ema3alpha) * ema3Val;
            ema3Series[0] = ema3Val;

            if (ema3Series[1] < ctoPrevSeries[1] && ema3Val >= Cto) { isCrossUpT3 = true;  isCrossDownT3 = false; }
            if (ema3Series[1] > ctoPrevSeries[1] && ema3Val <= Cto) { isCrossDownT3 = true; isCrossUpT3  = false; }

            // long/short — T3-based directional signals
            double src1 = avgSeries[1];
            bool long_sig = (avg > Cto && hkCloseSeries[1] < ctoPrevSeries[1] && avg > hkCloseSeries[1]) ||
                            (hkCloseSeries[1] > ctoPrevSeries[1] && avg > hkCloseSeries[1] &&
                             hkCloseSeries[1] < hkCloseSeries[2] && avg > ema3Val);
            bool short_sig = (avg < Cto && hkCloseSeries[1] > ctoPrevSeries[1] && avg < hkCloseSeries[1]) ||
                             (hkCloseSeries[1] < ctoPrevSeries[1] && avg < hkCloseSeries[1] &&
                              hkCloseSeries[1] > hkCloseSeries[2] && avg < ema3Val);

            // ── MACD (5/13/8 custom + 12/26/9 standard) ──────────────────────
            if (!emaInit) { ema5Val=ema13Val=ema12Val=ema26Val=avg; ema8MVal=ema9MVal=0.0; emaInit=true; }
            ema5Val  += 2.0/(5  +1.0) * (avg - ema5Val);
            ema13Val += 2.0/(13 +1.0) * (avg - ema13Val);
            ema12Val += 2.0/(12 +1.0) * (avg - ema12Val);
            ema26Val += 2.0/(26 +1.0) * (avg - ema26Val);
            double macdVal  = ema5Val  - ema13Val;
            double macd1226 = ema12Val - ema26Val;
            ema8MVal += 2.0/(8+1.0) * (macdVal  - ema8MVal);
            ema9MVal += 2.0/(9+1.0) * (macd1226 - ema9MVal);
            double hist   = macdVal  - ema8MVal;
            double hist2  = macd1226 - ema9MVal;
            macdSeries[0]   = macdVal;
            histSeries[0]   = hist;
            macd1226Series[0] = macd1226;
            hist2Series[0]  = hist2;
            sig9Series[0]   = ema9MVal;

            bool macd_up_1 = hist2Series[2] < 0 && hist2Series[1] > hist2Series[2] &&
                             hist2Series[3] < 0 && hist2Series[2] < hist2Series[3];
            bool just_change_down = hist2Series[1] > 0 && hist2 < 0;
            bool is_weak_put = just_change_down || (hist2Series[2] > 0 && hist2Series[1] < 0);

            // ── RSI on hk_c (5-period, Wilder's RMA) ─────────────────────────
            double hkChg = hkClose - hkCloseSeries[1];
            if (!rsiInit) { rsiRmaUp=Math.Max(hkChg,0); rsiRmaDn=Math.Max(-hkChg,0); rsiInit=true; }
            rsiRmaUp += (1.0/5.0) * (Math.Max( hkChg,0) - rsiRmaUp);
            rsiRmaDn += (1.0/5.0) * (Math.Max(-hkChg,0) - rsiRmaDn);
            double rsi = rsiRmaDn == 0 ? 100 : rsiRmaUp == 0 ? 0 : 100 - 100.0/(1+rsiRmaUp/rsiRmaDn);
            rsiSeries[0] = rsi;

            // ── RSI2 on avg (11-period, Wilder's RMA) ────────────────────────
            double avgChg2 = avg - avgSeries[1];
            if (!rsi2Init) { rmaUpVal=Math.Max(avgChg2,0); rmaDnVal=Math.Max(-avgChg2,0); rsi2Init=true; }
            rmaUpVal += (1.0/11.0) * (Math.Max( avgChg2,0) - rmaUpVal);
            rmaDnVal += (1.0/11.0) * (Math.Max(-avgChg2,0) - rmaDnVal);
            double rsi2 = rmaDnVal == 0 ? 100 : rmaUpVal == 0 ? 0 : 100 - 100.0/(1+rmaUpVal/rmaDnVal);
            double rsiMa = SmaOf(rsi2Series, 24);
            rsi2Series[0]  = rsi2;
            rsiMaSeries[0] = rsiMa;

            if (rsi2Series[1] < rsiMaSeries[1] && rsi2 >= rsiMa)
                { isCrossDownRsi = false; isCrossUpRsi = true;  }
            if (rsi2Series[1] > rsiMaSeries[1] && rsi2 <= rsiMa)
                { isCrossDownRsi = true;  isCrossUpRsi = false; }

            // ── WMA 3/11/21 on avg ────────────────────────────────────────────
            wma3Series[0]  = WmaOf(avgSeries, 3);
            wma11Series[0] = WmaOf(avgSeries, 11);
            wma21Series[0] = WmaOf(avgSeries, 21);
            double wma3av = wma3Series[0], wma11av = wma11Series[0], wma21av = wma21Series[0];

            // ── MISC DERIVED ──────────────────────────────────────────────────
            double tinyChange = Math.Abs(Math.Abs(Opens[0][2] - Opens[0][1]) - avgChange);
            bool end_up_2 = !double.IsNaN(runningPh) &&
                            ocSeries[1] < runningPh && o_c >= runningPh &&
                            Opens[0][2] < Opens[0][1] && Opens[0][1] < o_c && tinyChange > 0.1;

            bool is_up = o_c > avg;
            bool start_up_1 = is_up;
            for (int i = 1; i <= 10; i++) if (Opens[0][i] > avgSeries[i]) { start_up_1 = false; break; }
            if (!is_up) start_up_1 = false;

            bool start_bottom = hist < 0 && histSeries[1] < hist;

            bool bottomsupport = !double.IsNaN(runningPl) &&
                                 avgSeries[1] > runningPl && avg <= runningPl &&
                                 rsi > rsiSeries[1] + 8;
            bool bigdrop = rsi + 6 < rsiSeries[1] && !double.IsNaN(runningPh) && avg < runningPh &&
                           barUpSeries[4] > 0 && barUpSeries[3] > 0 && barUpSeries[2] > 0 &&
                           barUpSeries[1] <= 0 && barUpSeries[0] <= 0 &&
                           wma3m3Series[0] > wma5m5Series[0];
            bool is_cross_res = !double.IsNaN(runningPh) &&
                                ocSeries[1] < runningPh && o_c >= runningPh &&
                                o_c > wma13 && wma13 > wma48;

            // NOTE: is_br is effectively always false in Pine Script (o_c=open, so
            // "open[1] < o_c[1]" = open[1]<open[1] = false). Kept for completeness.
            bool is_br = false;

            double wma3m3  = wma3m3Series[0];
            double wma3m8  = wma3m8Series[0];
            double wma5m5  = wma5m5Series[0];
            double wma8m8  = wma8m8Series[0];
            double uper10  = uper10Series[0];
            double lower1  = lower1Series[0];
            double upper1  = upper1Series[0];
            double lower6  = lower6Series[0];
            double upper6  = upper6Series[0];

            // ── LONG CONDITIONS ───────────────────────────────────────────────
            // LC1: short-term uptrend + price breaks above wma21 + 3M trend rising (v1.5 fix)
            bool lc1 = wma3av > wma11av &&
                       (ocSeries[1] < wma21Series[1] && o_c >= wma21av) &&
                       wma3m3Series[1] < wma3m3 &&
                       !(wma3m8Series[1] > wma3m8);

            bool lc2 = (wma3av > wma11av && start_up_1) ||
                       (long_sig && bar_up &&
                        !BottomSupportAt(2) && BottomSupportAt(1) && bottomsupport &&
                        Opens[0][1] < o_c);

            bool lc3 = long_sig && !BottomSupportAt(2) && BottomSupportAt(1) && bottomsupport &&
                       Opens[0][1] < o_c;

            bool lc4 = XOver(ocSeries, wma3m8Series) && XOver(ocSeries, wma5m5Series) &&
                       !(wma3m3 < wma5m5 && wma5m5 < wma3m8) && !(macdSeries[1] > macdVal);

            bool lc5 = start_bottom && XOver(wma3m3Series, wma5m5Series) && avgSeries[1] < avg;

            bool lc6 = buy_trend && bar_up && is_cross_res;

            bool lc7 = bar_up && buy_trend && isCrossUpT3 && bottomsupport && isStrongBuy;

            // LC9: 3 down bars + 2 up bars + MACD histogram turning up from negative
            bool lc9 = barDownSeries[3] > 0 && barDownSeries[2] > 0 &&
                       barUpSeries[1] > 0  && bar_up &&
                       hist2Series[1] < 0  && hist2Series[1] < hist2;
            if (rsi2Series[1] < rsi2 && rsi2 > 70 &&
                barUpSeries[2] > 0 && barUpSeries[1] > 0 && bar_up)
                lc9 = false;
            lc9Series[0] = lc9 ? 1.0 : 0.0;

            // LC10: continuation after lc9, with MACD confirmation (uses lc9[1])
            bool lc10 = lc9Series[1] > 0 && o_c > avg && Opens[0][1] > avgSeries[1] && macd_up_1;
            bool closeShort2 = wma3av < wma11av && ocSeries[1] > wma3Series[1] && o_c > wma11av &&
                               !(wma3m3Series[1] > wma3m3) && !(wma5m5Series[1] > wma5m5);
            if (closeShort2 || macd1226Series[1] > macd1226) lc10 = false;

            // LC11: 4-bar setup with hkHigh crossover wma5m5
            bool lc11 = !BarUpAt(4) && !BarUpAt(3) && !BarUpAt(2) && BarUpAt(1) && bar_up &&
                        XOver(hkHighSeries, wma5m5Series);

            // LC12: deep below BB lower + consecutive rising opens + avg momentum reversal
            bool lc12 = avg < lower6 &&
                        (Opens[0][3] < avgSeries[3] && Opens[0][2] >= avgSeries[2]) &&
                        Opens[0][0] > Opens[0][1] && Opens[0][1] > Opens[0][2] && Opens[0][2] > Opens[0][3] &&
                        avgSeries[5] > avgSeries[3];
            // isLongCond11 latches when lc12 triggers near a recent lc11
            if (lc12 && (PastBool(lc9Series, 1) || PastBool(lc9Series, 2)))
                isLongCond11 = true;

            // LC13: break above 10M mid-band with 1M basis crossing 10M basis
            bool lc13 = XOver(ocSeries, uper10Series) &&
                        (XOver(basisSeries, basis3Series) || XOverAt(basisSeries, basis3Series, 1));
            if (lc13 && o_c > basis) isUp10m = true;
            if (XUnder(ocSeries, uper10Series) || XUnder(avgSeries, uper10Series)) isUp10m = false;

            bool longCondition = lc9 || lc4 || lc5 || lc6 || lc13 || lc1 || lc7;
            if (!bar_up || macd1226Series[1] > macd1226 || rsiMaSeries[1] > rsiMa)
                longCondition = false;
            if (XUnder(wma3m3Series, wma5m5Series) || histSeries[1] > hist || !bar_up)
                longCondition = false;
            longCondition = lc10 || longCondition;

            // ── SHORT CONDITIONS ──────────────────────────────────────────────
            bool sc1 = bigdrop && isStrongSell;
            bool sc2 = is_br && !bar_up;   // always false, see is_br note above

            bool sc3 = BarUpAt(10) && BarUpAt(9) && BarUpAt(8) && BarUpAt(7) && BarUpAt(6) &&
                       BarUpAt(5)  && BarUpAt(4) && BarUpAt(3) && BarUpAt(2) && BarUpAt(1) && !bar_up;

            bool sc4 = BarUpAt(3) && BarUpAt(2) && !BarUpAt(1) && !bar_up &&
                       XUnder(hkLowSeries, wma5m5Series);
            bool weak_put_uper  = avg > upper1 && upper1Series[1] < upper1 &&
                                  XUnder(ocSeries, avgSeries);
            bool weak_put_uper2 = XUnderAt(avgSeries, upper1Series, 2) &&
                                  Opens[0][1] > avgSeries[1] && o_c < avg;
            if (weak_put_uper || weak_put_uper2) sc4 = false;

            bool start_short_2 = wma3av > wma11av && Opens[0][2] < wma3Series[2] &&
                                 Opens[0][1] < wma11Series[1] && o_c < wma11av;
            bool sc5 = start_short_2;
            if ((hist2Series[3] < hist2Series[2] && hist2Series[2] > hist2Series[1] &&
                 hist2Series[1] > hist2) ||
                (is_weak_put && XUnder(ocSeries, lower1Series)))
                sc5 = false;

            bool sc6 = sc4 && bigdrop;
            if (weak_put_uper2) { sc6 = false; sc4 = false; }

            // SC7: RSI dropping hard + price well below WMA13 OR RSI drop + short_sig (v1.5 fix: operator precedence)
            bool sc7 = (o_c * 0.94 < wma13 && rsi + 8 < rsiSeries[1]) ||
                       (rsi + 8 < rsiSeries[1] && short_sig);

            bool shortCondition = sc1 || sc2 || sc3 || sc4 || sc5 || sc6 || sc7;
            if (bar_up || macd1226Series[1] < macd1226 || rsiMaSeries[1] < rsiMa)
                shortCondition = false;

            // ── CLOSE LONG CONDITIONS ─────────────────────────────────────────
            bool end_up_1 = Opens[0][4] < Opens[0][3] && Opens[0][3] < Opens[0][2] &&
                            Opens[0][2] > Opens[0][1] && Opens[0][1] > o_c;
            bool closeLong1 = end_up_1;
            bool closeLong2 = end_up_2;
            bool closeLong3 = XUnder(ocSeries, wma3m3Series) && !(histSeries[1] < hist);
            if (o_c > avg && avg < basis) closeLong3 = false;
            bool closeLong4 = hist > 0 && histSeries[1] > hist && !(wma3m3Series[1] < wma3m3);
            if (o_c > avg && avg < basis) closeLong4 = false;

            if (isUp10m) { closeLong1 = false; closeLong3 = false; closeLong4 = false; }
            bool closelong = closeLong1 || closeLong2 || closeLong3 || closeLong4;
            if (isLongCond11) closelong = false;
            if (bottomsupport) closelong = false;

            // ── CLOSE SHORT CONDITIONS ────────────────────────────────────────
            bool closeShort1 = start_bottom &&
                               (!(o_c < wma3m3 || Opens[0][1] < wma3m3Series[1]) ||
                                wma3m3Series[1] < wma3m3);
            bool closeShort3 = short_sig;

            if (o_c < wma21av || isCrossUpRsi) closeShort2 = false;
            if (isCrossDownBasis && isCrossDownLower1 && o_c < lower1 && avgSeries[1] > avg)
                { closeShort1 = false; closeShort3 = false; }
            if (isCrossDownBasis && o_c < avg && avgSeries[1] > avg)
                closeShort3 = false;

            bool closeshort = closeShort1 || closeShort2 || closeShort3;
            if (o_c < Opens[0][1]) closeshort = false;

            // ── TRADING HOURS LOCK (6:30 AM – 12:30 PM PT / UTC-8) ───────────
            // Time[0] is UTC in NinjaTrader; subtract 8 h for Pacific Time
            DateTime ptTime = Time[0].ToUniversalTime().AddHours(-8);
            int hr = ptTime.Hour, mi = ptTime.Minute;
            bool lock_time = hr >= 13 || (hr == 12 && mi > 30) || hr < 6 || (hr == 6 && mi < 30);

            // ── SL/TP LEVELS (informational) ──────────────────────────────────
            double slCall  = callPrice  > 0 ? callPrice  * (1 - StopPerCallPct / 100.0) : 0;
            double tpCall  = callPrice  > 0 ? callPrice  * (1 + TakePerCallPct / 100.0) : 0;
            double slPut   = putPrice   > 0 ? putPrice   * (1 + StopPerPutPct  / 100.0) : 0;
            double tpPut   = putPrice   > 0 ? putPrice   * (1 - TakePerPutPct  / 100.0) : 0;

            // ── SIGNALS ───────────────────────────────────────────────────────
            if (longCondition && !lock_time)
            {
                callPrice = o_c; putPrice = 0.0;
                isLongCond11 = false;
                Draw.ArrowUp(this, "BUY_" + CurrentBar, false, 0,
                             Low[0] - 5 * TickSize, Brushes.Lime);
                if (AlertsEnabled)
                    Alert("BuySignal", Priority.High,
                          string.Format("V1.5 BUY @ {0:F2}  SL:{1:F2}  TP:{2:F2}",
                                        o_c, slCall, tpCall),
                          NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert1.wav",
                          10, Brushes.Lime, Brushes.Black);
            }

            if (shortCondition && !lock_time)
            {
                putPrice = o_c; callPrice = 0.0;
                Draw.ArrowDown(this, "SELL_" + CurrentBar, false, 0,
                               High[0] + 5 * TickSize, Brushes.Red);
                if (AlertsEnabled)
                    Alert("SellSignal", Priority.High,
                          string.Format("V1.5 SELL @ {0:F2}  SL:{1:F2}  TP:{2:F2}",
                                        o_c, slPut, tpPut),
                          NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert2.wav",
                          10, Brushes.Red, Brushes.Black);
            }

            if (closelong && callPrice > 0.0)
            {
                Draw.Dot(this, "CL_" + CurrentBar, false, 0, High[0] + 8 * TickSize, Brushes.Orange);
                if (AlertsEnabled)
                    Alert("CloseLong", Priority.Medium, "V1.5 CLOSE LONG @ " + o_c.ToString("F2"),
                          NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert3.wav",
                          10, Brushes.Orange, Brushes.Black);
                callPrice = 0.0;
            }

            if (closeshort && putPrice > 0.0)
            {
                Draw.Dot(this, "CS_" + CurrentBar, false, 0, Low[0] - 8 * TickSize, Brushes.Cyan);
                if (AlertsEnabled)
                    Alert("CloseShort", Priority.Medium, "V1.5 CLOSE SHORT @ " + o_c.ToString("F2"),
                          NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert4.wav",
                          10, Brushes.Cyan, Brushes.Black);
                putPrice = 0.0;
            }

            // ── CHART PLOTS ───────────────────────────────────────────────────
            Values[0][0] = basis3Series[0];   // Basis3 (10M VWMA, blue thick)
            Values[1][0] = uper10Series[0];   // Uper10 (white thick)
            Values[2][0] = lower10Series[0];  // Lower10 (green thick)
            Values[3][0] = basisSeries[0];    // Basis 1M (yellow)
            Values[4][0] = upper6Series[0];   // Upper BB (red)
            Values[5][0] = lower6Series[0];   // Lower BB (blue)
            Values[6][0] = avg;               // 10-bar open avg (gray thick)
            Values[7][0] = macd1226;          // MACD 12/26 (white thin)
            Values[8][0] = ema9MVal;          // Signal 9 (yellow thin)
        }

        // ── HELPER METHODS ────────────────────────────────────────────────────

        // VWMA of 1M open prices over `length` bars (index 0 = most recent)
        private double Vwma1M(int length)
        {
            double sumPV = 0, sumV = 0;
            for (int i = 0; i < length; i++)
            {
                double v = Volumes[0][i]; if (v <= 0) v = 1;
                sumPV += Opens[0][i] * v;
                sumV  += v;
            }
            return sumPV / sumV;
        }

        private double Stdev1M(int length, double mean)
        {
            double sumSq = 0;
            for (int i = 0; i < length; i++) sumSq += Math.Pow(Opens[0][i] - mean, 2);
            return Math.Sqrt(sumSq / length);
        }

        // VWMA of 10M (BarsArray[4]) open prices
        private double Vwma10M(int length)
        {
            double sumPV = 0, sumV = 0;
            int avail = Math.Min(length, CurrentBars[4] + 1);
            for (int i = 0; i < avail; i++)
            {
                double v = Volumes[4][i]; if (v <= 0) v = 1;
                sumPV += Opens[4][i] * v;
                sumV  += v;
            }
            return sumV > 0 ? sumPV / sumV : Opens[4][0];
        }

        private double Stdev10M(int length, double mean)
        {
            int avail = Math.Min(length, CurrentBars[4] + 1);
            double sumSq = 0;
            for (int i = 0; i < avail; i++) sumSq += Math.Pow(Opens[4][i] - mean, 2);
            return avail > 0 ? Math.Sqrt(sumSq / avail) : 0;
        }

        // WMA of Series<double>: weight = (period - i) for i = 0..period-1
        private double WmaOf(Series<double> s, int period)
        {
            int avail = Math.Min(period, CurrentBars[0] + 1);
            double sumW = 0, sumWV = 0;
            for (int i = 0; i < avail; i++)
            {
                double w = period - i;
                sumW  += w;
                sumWV += w * s[i];
            }
            return sumW > 0 ? sumWV / sumW : s[0];
        }

        // SMA of Series<double>
        private double SmaOf(Series<double> s, int period)
        {
            int avail = Math.Min(period, CurrentBars[0] + 1);
            double sum = 0;
            for (int i = 0; i < avail; i++) sum += s[i];
            return avail > 0 ? sum / avail : s[0];
        }

        // CCI on a Series<double>: (value - sma) / (0.015 * meanDev)
        private double CciOf(Series<double> s, int period)
        {
            double sma = SmaOf(s, period);
            int avail  = Math.Min(period, CurrentBars[0] + 1);
            double md  = 0;
            for (int i = 0; i < avail; i++) md += Math.Abs(s[i] - sma);
            md /= avail;
            return md == 0 ? 0 : (s[0] - sma) / (0.015 * md);
        }

        // Simple ATR (average of true ranges, no smoothing — sufficient for SuperTrend init)
        private double AtrOf(int period)
        {
            double sum = 0;
            int avail = Math.Min(period, CurrentBars[0]);
            if (avail < 1) return 0;
            for (int i = 0; i < avail; i++)
            {
                double tr = Math.Max(Highs[0][i] - Lows[0][i],
                            Math.Max(Math.Abs(Highs[0][i] - Closes[0][i + 1]),
                                     Math.Abs(Lows[0][i]  - Closes[0][i + 1])));
                sum += tr;
            }
            return sum / avail;
        }

        // Crossover / Crossunder helpers (at current bar)
        private bool XOver(Series<double> a, Series<double> b)
            => a[1] < b[1] && a[0] >= b[0];
        private bool XUnder(Series<double> a, Series<double> b)
            => a[1] > b[1] && a[0] <= b[0];

        // Cross at `n` bars ago: [n+1] → [n]
        private bool XOverAt(Series<double> a, Series<double> b, int n)
            => a[n+1] < b[n+1] && a[n] >= b[n];
        private bool XUnderAt(Series<double> a, Series<double> b, int n)
            => a[n+1] > b[n+1] && a[n] <= b[n];

        // barUpSeries lookup helpers
        private bool BarUpAt(int barsAgo)   => barUpSeries[barsAgo]   > 0;
        private bool BarDownAt(int barsAgo) => barDownSeries[barsAgo] > 0;

        // Boolean Series<double> lookup (1.0 = true)
        private bool PastBool(Series<double> s, int barsAgo) => s[barsAgo] > 0;

        // bottomsupport at `barsAgo`: avg crossed under running_pl with RSI rising
        private bool BottomSupportAt(int barsAgo)
        {
            if (double.IsNaN(runningPl)) return false;
            return avgSeries[barsAgo + 1] > runningPl &&
                   avgSeries[barsAgo]     <= runningPl &&
                   rsiSeries[barsAgo] > rsiSeries[barsAgo + 1] + 8;
        }
    }
}
