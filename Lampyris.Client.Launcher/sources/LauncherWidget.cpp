// Project Include(s)
#include "LauncherWidget.h"

LauncherWidget::LauncherWidget(const QStringList& args, QWidget* parent)
    : QWidget(parent), m_launcherManager(new LauncherManager(args, this)) {
    m_ui.setupUi(this);

    connect(m_launcherManager, &LauncherManager::updateStatus, this, [this](const QString& status) {
        m_ui.textStatus->setText(status);
    });

    connect(m_launcherManager, &LauncherManager::updateProgress, this, [this](int progress) {
        m_ui.progressBar->setValue(progress);
    });

    connect(m_launcherManager, &LauncherManager::retryCountdown, this, [this](int seconds) {
        m_ui.textStatus->setText(QString("检查更新失败，将在 %1 秒后重试...").arg(seconds));
    });

    connect(m_launcherManager, &LauncherManager::loadSucceed, this, [this]() {
        this->close();
    });

    connect(m_launcherManager, &LauncherManager::updateVersion, this, [this](const QString& version) {
        m_ui.textVersion->setText(version);
    });

    m_launcherManager->checkForUpdates();
}
