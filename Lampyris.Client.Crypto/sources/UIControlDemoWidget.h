// QT Include(s)
#include <QApplication>
#include <QTabWidget>
#include <QVBoxLayout>
#include <QFile>
#include <QDebug>
#include <QMainWindow>

// Project Include(s)
#include "UI/Standalone/Quote/QuoteTickerDataWidget.h"
#include "UI/Common/AssetTrendCurveWidget.h"
#include "UI/Common/MarketOverviewBarChartWidget.h"
#include "UI/Mobile/Common/BottomPopupWidget.h"
#include "UI/Common/TabButtonView.h"
#include "UI/Common/BetterLineEdit.h"
#include "UI/Common/TableHeader.h"
#include "UI/Standalone/Trade/TradeWidget.h"

// Tab 1: AssetTrendCurveWidget
class AssetTrendCurveTab : public QWidget {
public:
    AssetTrendCurveTab(QWidget* parent = nullptr);
};

// Tab 2: MarketOverviewBarChartWidget
class MarketOverviewBarChartTab : public QWidget {
public:
    MarketOverviewBarChartTab(QWidget* parent = nullptr);
};

// Tab 3: BottomPopupWidget
class BottomPopupTab : public QWidget {
public:
    BottomPopupTab(QWidget* parent = nullptr);
};

// Tab 4: TableHeader
class TableHeaderTab : public QWidget {
public:
    TableHeaderTab(QWidget* parent = nullptr);
};

// Tab 5: TabButtonView
class TabButtonViewTab : public QWidget {
public:
    TabButtonViewTab(QWidget* parent = nullptr);
};

// Tab 6: BetterLineEdit
class BetterLineEditTab : public QWidget {
public:
    BetterLineEditTab(QWidget* parent = nullptr);
};

// Tab 7: TradeWidget
class TradeWidgetTab : public QWidget {
public:
    TradeWidgetTab(QWidget* parent = nullptr);
};

// UIControlDemoWidget
class UIControlDemoWidget : public QMainWindow {
public:
    UIControlDemoWidget(QWidget* parent = nullptr);
};