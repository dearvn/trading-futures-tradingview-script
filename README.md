# SocSwift Chart — Real-Time Options Flow on the Chart

I now trade **SPX options** using the live chart at **[trade.socswift.com](https://trade.socswift.com/dashboard/chart-dom?symbol=SPX)** — it plots institutional options flow (sweeps/blocks) directly on the price chart in real time, so you can see WHERE big money is buying calls and puts while the candle is still forming.

**Important:** this chart does NOT give buy/sell signals. It gives **context** — the raw institutional flow. You read that context to understand the trend, then decide your own CALL or PUT entry by following what the big players are doing.

![SocSwift SPX chart with flow marks](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/socswift-chart.png?raw=true "SocSwift SPX 1m with 0DTE flow marks")
<!-- save your SPX chart screenshot as socswift-chart.png in the repo root -->

## What is on the chart

| Element | Meaning |
|---------|---------|
| Candlesticks + Volume | Standard OHLCV, down to **30-second** bars |
| **VWAP** | Volume-weighted average price for the session |
| **EMA 21** | 21-period exponential moving average (blue line) — my trend filter |
| **Flow marks** | Real-time options sweeps printed at the price/time they hit the tape |
| **GEX levels** | Dealer gamma-exposure levels (support/resistance magnets) — toggle button on the toolbar |
| **DOM box** | Live price ladder for one option contract (bid/ask depth) |
| Current price line | Red dotted line with countdown to bar close |

## How to read a flow mark

Example: `▲ C7600 ASK 0DTE $1.2M`

- `▲` green / `▼` red — direction of the aggressor (lifting the ask = buying, hitting the bid = selling)
- `C7600` / `P7600` — **C**all or **P**ut at strike 7600
- `ASK` / `BID` — side the order executed on. Calls bought at ASK = bullish; Puts bought at ASK = bearish
- `0DTE` — days to expiration (0DTE = expires today; the fastest, most aggressive flow)
- `$1.2M` — total premium paid. Bigger premium = more conviction

**How I use it:** the marks are context, not signals. When a cluster of green `C... ASK` marks stacks up while price holds above VWAP/EMA 21, that is institutional call buying confirming the trend — I enter a CALL and ride with the big players. Red `P... ASK` clusters near highs tell me a reversal is being bought — I stay out or look for a PUT entry. No mark by itself is an entry; the confluence of flow + trend is.

## Chart features

- **Timeframes:** 30s, 1m, 3m, 5m, 15m, Daily, Weekly (seconds bars are streamed live — great for 0DTE scalping)
- **Multi-chart layouts:** 1 / 2 / 3 / 4 panes, each pane with its own symbol, timeframe, and indicators
- **Drawing tools:** trendline, brush (smoothed), fibonacci, rectangle, text, with magnet/snap mode — drawings are saved server-side per user, so they survive reload and follow you across devices
- **Watchlist** with live prices, and an alert sound when unusually large flow prints
- **GEX levels / Flow marks toggles** on the top toolbar to declutter the chart

## How to register

1. Go to **https://trade.socswift.com** and click **Sign Up** (email + password, then verify your email)
2. Log in → open **Dashboard → Billing** and start a plan (a trial is available for new accounts)
3. Open **Dashboard → Chart** (chart-dom), pick a symbol (SPX, SPY, QQQ, and other big-cap optionable tickers…)
4. Turn on **Flow marks** and **GEX levels** in the toolbar and pick your timeframe

Questions or a custom indicator/bot: donald.nguyen.it@gmail.com

---

# Management members on TradingView and the Discord channel on your website.
https://www.patreon.com/donaldit/shop/manage-members-on-tradingview-discord-415211

# NQ1M

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/NQ1m.png?raw=true "NQ1 1M")


# SPX trading options

In this indicator, I detect the real-time TOP (blue line) and BOTTOM (white line) levels, as well as signals to CALL or PUT. The options move very quickly, so a bot is needed to trade automatically.

Get one at: https://www.patreon.com/donaldit/shop/spx-15-options-trading-strategy-321411

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/spx-options.png?raw=true "SPX-30M options")

A new indicator to predict signal: https://www.patreon.com/donaldit/shop/simple-indicator-for-tracking-trading-on-231800?source=storefront

Get options data to auto trading from: https://tradier.com/

Api to integrate to Schwab: https://github.com/alexgolec/schwab-py

# The $NQM2024 futures strategy is 70% accurate in a 5-minute timeframe

Please access: 

https://www.patreon.com/donaldit/shop/nqm2024-futures-trading-160775?utm_medium=clipboard_copy&utm_source=copyLink&utm_campaign=productshare_fan&utm_content=join_link

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/trailing-nq.png?raw=true "NQM2024")


# $ES futures, $SPX strategy 80% accurate:

Please access: 

https://www.patreon.com/donaldit/shop/one-strategy-for-auto-trading-es-futures-155876?source=storefront

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/trailing-es.png?raw=true "ESSPX")

# Add indicator High Low Super for SPX, ES, ETH...

supper-high-low-live.txt

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/high-low.png?raw=true "ETH")


# Update new indicator trailing

Lux-Trailing-BUY_SELL.txt

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/trailing.png?raw=true "SPX")


# Updated new strategy
nq-est-futres.txt

# Trading $SPX options

Timeframe: 15M, Ticker $SPX

Refs https://www.patreon.com/collection/333532?view=expanded

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/supr-box.png?raw=true "SPX")



If you win, please support me on Paypal: clickclone@gmail.com
## BEST Trading manually

The best way to use this Indicator is when you encounter a signal, refresh it to confirm whether it has occurred or not. If it happens then go according to this signal.

```bash
ES1M-BEST.txt
```

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/best.png?raw=true "ESU2023")


## New update

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/new.png?raw=true "ESU2023")

## Script Trading ES 20223

Please use this one with timeframe 5M: `https://github.com/dearvn/trading-futures-tradingview-script/blob/main/ESH2023-5M.txt`

## Trading GOLD
 
Using script `GOLD_UZ_OZ.txt`

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/gold.png?raw=true "Gold")


## ES Futures 1M No Repaint — v1.4 (latest)

File: `es-futures-no-repaint-v1.4.txt`  
Timeframe: **1M**, Ticker: **ES1!**  
Trading hours: 6:30 AM – 12:30 PM UTC-8

### Repaint fixes in v1.4 (vs v1.3)
- All `request.security()` calls changed from `lookahead_on` → `lookahead_off` (13 calls fixed) — this was the primary repainting source
- Multi-timeframe averages (`avg_3m`, `avg_5m`, `avg_8m`, `avg_10m`) now use only confirmed HTF bars — removed all `[0]` (current unconfirmed bar) references
- `avg` and `avg_change` shifted to exclude the current open bar (`open[0]` removed)
- All persistent `var bool` cross states (`is_cross_down_basis`, `is_cross_up_rsi`, etc.) now only latch on `barstate.isconfirmed`

### Auto Trade on Tradovate via TradingView Webhook

**Flow:**
```
TradingView Alert (Pine Script v1.4)
    ↓  webhook HTTP POST (JSON)
Your server (tradovate-trading-bot)
    ↓  Tradovate WebSocket / REST API
Tradovate → order executed
```

**Step 1 — Add alertcondition to the Pine Script**

Add to the end of `es-futures-no-repaint-v1.4.txt`:

```pine
alertcondition(longCondition and not lock_time,
  title="BUY Signal",
  message='{"action":"buy","symbol":"ESM2025","qty":1,"price":{{close}}}')

alertcondition(shortCondition and not lock_time,
  title="SELL Signal",
  message='{"action":"sell","symbol":"ESM2025","qty":1,"price":{{close}}}')

alertcondition(closelong,
  title="CLOSE LONG",
  message='{"action":"closeLong","symbol":"ESM2025"}')

alertcondition(closeshort,
  title="CLOSE SHORT",
  message='{"action":"closeShort","symbol":"ESM2025"}')
```

**Step 2 — Configure TradingView Alert**

1. Add indicator v1.4 to chart (1M, ES1!)
2. Create Alert → select condition **"BUY Signal"** (repeat for SELL, CLOSE LONG, CLOSE SHORT)
3. Set **Webhook URL** = `https://your-server.com/webhook`
4. The `{{close}}` placeholder is auto-filled by TradingView at alert time

**Step 3 — tradovate-trading-bot receives webhook**

The bot exposes a `/webhook` endpoint, parses the JSON payload, and sends the order to Tradovate API.

See: [tradovate-trading-bot](https://github.com/dearvn/tradovate-trading-bot)

---

Review history timeframe 30s: `es-futures-repaint-30s.txt`

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/alerts.png?raw=true "alerts")


## Newbie

**Use `color-trend-lite.txt` to trade easily**

**BLUE: trend up**

**RED: trend down**

**How to use: ex: when I enter CALL if color is still BLUE and high[1] < high then keep CALL. Or when I enter PUT if color is still RED and low[1] > low  then keep PUT. Else EXIT CALL or EXIT PUT**

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/color-trend.png?raw=true "color-trend")

## Strategy ES 1M

using: `best-strategy-es-1m.txt`

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/strategy-es.png?raw=true "strategy-es")

## Binance Futures Trading

Ref: https://github.com/dearvn/tradingview-pinscript-futures-binance

## PRIVATE SCRIPT

**11/07/2022**
![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/private.png?raw=true "private")


## WINNING VS LOSING TRADES

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/today.png?raw=true "today")

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/gain_loss_report.png?raw=true "gain_loss_report")


## SUPORT ME

I like a cup of coffee at https://www.patreon.com/donaldit

## IMPORTANT
* Currently, I am trading on ```trade-futures.txt``` script
Belong to ticker and timeframe, I set input IN and input OUT
Backtest on timeframe 5M
* ES: input IN = 5, input OUT = 3
* NQ: input IN = 15, input OUT = 12 or input IN = 8, input OUT = 5

Enjoy daily trading Futures and if this script is good please me coffee (https://www.patreon.com/donaldit)
or need implement a script donald.nguyen.it@gmail.com

## WEAK MARKET 
* I implement logic to trade when market is weak this time let use script ```best-indicator.txt```

**Using:**
*GC = Great Call (exit PUT beforce CALL)
*GP = Great PUT (exit CALL beforce PUT)
*Timframe: 5m

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/best-indicator.png?raw=true "best-indicator.png")

## SWING 
Using indicator `swing.txt` to exit or entry CALL PUT
![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/swing.png?raw=true "swing.png")

## MARKET CRASH
* I implement logic to trade when market crash this time let use script ```win-99.txt```

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/win100.png?raw=true "WIN100%")


## Alert
* I write some alert and can set webhook to get signal on Wordpress Plugin https://github.com/dearvn/tradingview-alerts

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/alert.png?raw=true "Alert")


# trading-futures-tradingview-script
I write pine script to trading futures ES1 NQ1 with signal IN (accurate 90%) and now I am trading on that
## Logic to trade futures
I can't use existing indicators to trade future, I lost so much
Few months ago, I backtest on this scripts and my idea is using the point to trade instead of indicator
Absolutely, the point is correct with futures.

For example, when "IN" signal notify I can CALL on Tradovate platform.

![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/nq.png?raw=true "NQ1")

## History Gain/Loss
* 2022-08-30 Gain PUT 253 Points
![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/nq-2022-08-30_at_22.12.05.png?raw=true "NQ1 2022-08-30 at 22.12.05")

* 2022-08-31 Gain/Loss in evidence
![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/nq_2022-30-31_at_17.34.17.png?raw=true "NQ1 2022-08-31 at 17.24.17")

* 2022-09-01 Gain/Loss in evidence
![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/nq_2022-09-01.png?raw=true "NQ1 2022-09-01")

* 2022-09-02 Gain/Loss in evidence
![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/nq_2022-09-02_at_10.13.06.png?raw=true "NQ1 nq_2022-09-02 at 10.13.06")

* 2022-09-06 Gain/Loss in evidence
![Alt text](https://github.com/dearvn/trading-futures-tradingview-script/raw/main/nq-2022-09-06_at_11.02.17.png?raw=true "NQ1 nq-2022-09-06 at 11.02.17")

