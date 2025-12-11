#include "UIControlDemoWidget.h"

AssetTrendCurveTab::AssetTrendCurveTab(QWidget* parent) : QWidget(parent) {
    QVBoxLayout* layout = new QVBoxLayout(this);
    AssetTrendCurveWidget* widget = new AssetTrendCurveWidget();
    layout->addWidget(widget);
}

MarketOverviewBarChartTab::MarketOverviewBarChartTab(QWidget* parent) : QWidget(parent) {
    QVBoxLayout* layout = new QVBoxLayout(this);

    // 创建 MarketOverviewBarChartWidget 实例
    MarketOverviewBarChartWidget* widget = new MarketOverviewBarChartWidget();

    // 构造测试数据
    MarketSummaryData testData;
    testData.riseCount = 50;         // 涨的数量
    testData.fallCount = 30;        // 跌的数量
    testData.unchangedCount = 20;   // 平盘数量
    testData.avgChangePerc = 1.5;   // 平均涨跌幅
    testData.top10AvgChangePerc = 3.2; // 前10名平均涨跌幅
    testData.last10AvgChangePerc = -2.8; // 后10名平均涨跌幅
    testData.mainStreamAvgChangePerc = 2.0; // 主流币平均涨跌幅

    // 构造 intervalData（区间数据）
    testData.intervalData = {
        { -1, -7, 5 },  // <-7%
        { -7, -5, 10 }, // -7%~-5%
        { -5, -3, 15 }, // -5%~-3%
        { -3, -1, 20 }, // -3%~-1%
        { 0, 0, 25 },   // 平盘
        { 1, 3, 30 },   // 1%~3%
        { 3, 5, 20 },   // 3%~5%
        { 5, 7, 15 },   // 5%~7%
        { 7, -1, 10 },  // >7%
    };

    // 更新数据到 MarketOverviewBarChartWidget
    widget->updateSumamryData(testData);

    // 设置柱状宽度
    widget->setBarWidth(40);

    // 将 widget 添加到布局
    layout->addWidget(widget);
}

BottomPopupTab::BottomPopupTab(QWidget* parent) : QWidget(parent) {
    QVBoxLayout* layout = new QVBoxLayout(this);

    QPushButton* openPopupButton = new QPushButton("打开弹出窗口");
    layout->addWidget(openPopupButton);

    // 创建底部弹出窗口
    BottomPopupWidget* popupWidget = new BottomPopupWidget(this);

    // 设置弹出窗口内容
    QWidget* contentWidget = new QWidget();
    QVBoxLayout* contentLayout = new QVBoxLayout(contentWidget);
    contentLayout->addWidget(new QLabel("这是弹出窗口的内容"));
    contentLayout->addWidget(new QPushButton("按钮1"));
    contentLayout->addWidget(new QPushButton("按钮2"));
    popupWidget->setContentWidget(contentWidget);

    // 点击按钮时显示弹出窗口
    connect(openPopupButton, &QPushButton::clicked, [=]() {
        popupWidget->showPopup(contentWidget);
        });
}

TableHeaderTab::TableHeaderTab(QWidget* parent) : QWidget(parent) {
    QVBoxLayout* layout = new QVBoxLayout(this);

    TableHeader* tableHeader = new TableHeader();
    TableHeaderDefinition definition;
    definition.startFieldGroup(0.4)
        .addField("代码", true)
        .addField("成交额", false)
        .end();
    definition.startFieldGroup(0.3)
        .addField("价格", true)
        .addField("涨速", true)
        .end();
    definition.startFieldGroup(0.3)
        .addField("涨幅", true)
        .end();

    tableHeader->setHeaderDefinition(definition);
    layout->addWidget(tableHeader);
}

TabButtonViewTab::TabButtonViewTab(QWidget* parent) : QWidget(parent) {
    QVBoxLayout* layout = new QVBoxLayout(this);

    TabButtonView* tabView = new TabButtonView;
    tabView->setStyleSheet("background-color: black;");
    tabView->addTab("Tab 1");
    tabView->addTab("Tab 2");
    tabView->addTab("Tab 3");
    tabView->addTab("Tab 4");

    tabView->setDragEnabled(true); // 启用拖拽模式
    layout->addWidget(tabView);
}

BetterLineEditTab::BetterLineEditTab(QWidget* parent) : QWidget(parent) {
    QVBoxLayout* layout = new QVBoxLayout(this);

    BetterLineEdit* lineEdit = new BetterLineEdit();
    lineEdit->setOptions({ "option1", "option2", "option3" });
    layout->addWidget(lineEdit);
}

TradeWidgetTab::TradeWidgetTab(QWidget* parent) : QWidget(parent) {
    QVBoxLayout* layout = new QVBoxLayout(this);

    TradeWidget* tradeWidget = new TradeWidget();
    tradeWidget->setObjectName("tradeWidget");
    tradeWidget->setStyleSheet("QWidget#tradeWidget{background-color: black;} QWidget{color:white;} QPushButton {background-color:transparent;}");
    layout->addWidget(tradeWidget);
}

UIControlDemoWidget::UIControlDemoWidget(QWidget* parent) : QMainWindow(parent) {
    QTabWidget* tabWidget = new QTabWidget(this);

    // 添加各个 Tab 页
    tabWidget->addTab(new AssetTrendCurveTab(), "Asset Trend Curve");
    tabWidget->addTab(new MarketOverviewBarChartTab(), "Market Overview");
    tabWidget->addTab(new BottomPopupTab(), "Bottom Popup");
    tabWidget->addTab(new TableHeaderTab(), "Table Header");
    tabWidget->addTab(new TabButtonViewTab(), "Tab Button View");
    tabWidget->addTab(new BetterLineEditTab(), "Better Line Edit");
    tabWidget->addTab(new TradeWidgetTab(), "Trade Widget");

    setCentralWidget(tabWidget);
    resize(800, 600);
}