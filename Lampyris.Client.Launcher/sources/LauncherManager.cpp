// Project Include(s)
#include "LauncherManager.h"
#include "SingletonApp.h"
#include <interface/IPlugin.h>
#include <Utilities/PathUtil.h>

// QT Include(s)
#include <QFile>
#include <QJsonDocument>
#include <QJsonObject>
#include <QJsonArray>
#include <QTimer>
#include <QElapsedTimer>
#include <QDebug>
#include <QApplication>
#include <QCryptographicHash>
#include <QDir>

#if defined(Q_OS_WINDOWS)
#include <QMessageBox>
#define PLATFORM "Standalone"

#elif defined(Q_OS_ANDROID)
#include <Platformdependent/Android/Toast.hpp>
#define PLATFORM "Android"


#elif defined(Q_OS_IOS)
#define PLATFORM "IOS"

#endif

LauncherManager::LauncherManager(const QStringList& args, QObject* parent)
    : QObject(parent), m_args(args) {
    // 设置默认值
    m_baseUrl = "http://47.83.146.10:7015";
    m_versionEndpoint = PathUtil::combineUrlPath("/version", PLATFORM);
    m_downloadEndpoint = PathUtil::combineUrlPath("/download", PLATFORM);
    m_currentVersion = "0.0";
    m_entryPointDLLName = "Lampyris.Client.Crypto";

    loadConfig();

    // 入口文件DLL名称即可设置为application名称
    SingletonApp* app = static_cast<SingletonApp*>(QApplication::instance());
    app->setApplicationName(m_entryPointDLLName);

    // 判断有没有相同实例在运行
    if (app->isAlreadyRunning()) {
        QApplication::quit();
    }
}

void LauncherManager::loadConfig() {
    QFile configFile(CONFIG_FILE_PATH);
    if (!configFile.exists()) {
        qDebug() << "[LauncherManager::loadConfig]配置文件不存在，将创建一个具有默认值的配置文件.";
        createDefaultConfig();
    }

    QSettings m_settings(CONFIG_FILE_PATH, QSettings::IniFormat);

    m_baseUrl = m_settings.value(CONFIG_KEY_BASE_URL, "").toString();
    m_versionEndpoint = m_settings.value(CONFIG_KEY_VERSION_ENDPOINT, "").toString();
    m_downloadEndpoint = m_settings.value(CONFIG_KEY_DOWNLOAD_ENDPOINT, "").toString();

    if (m_baseUrl.isEmpty() || m_versionEndpoint.isEmpty() || m_downloadEndpoint.isEmpty() || m_currentVersion.isEmpty()) {
        qDebug() << "[LauncherManager::loadConfig]配置文件存在键值对为空的情况，将恢复为默认设置.";
        createDefaultConfig(); // 有问题也需要恢复默认文件
    }
}

void LauncherManager::createDefaultConfig() {
    QSettings m_settings(CONFIG_FILE_PATH, QSettings::IniFormat);

    // 写入默认配置项
    m_settings.setValue(CONFIG_KEY_BASE_URL, m_baseUrl);
    m_settings.setValue(CONFIG_KEY_VERSION_ENDPOINT, m_versionEndpoint);
    m_settings.setValue(CONFIG_KEY_DOWNLOAD_ENDPOINT, m_downloadEndpoint);
    m_settings.setValue(CONFIG_KEY_DLL_NAME, m_entryPointDLLName);
}

void LauncherManager::showFatalErrorTips(const QString& message) {
#if defined(Q_OS_WINDOWS)
    QMessageBox::critical(Q_NULLPTR, "Lampyris应用程序致命错误!", message);
#elif defined(Q_OS_ANDROID)
    ToastHelper::showToast(message, 1);
#endif
}

void LauncherManager::checkForUpdates() {
    emit updateStatus("正在检查更新...");
    QFile configFile(PathUtil::combinePath(QApplication::applicationDirPath(), CONFIG_FILE_PATH));
    if (!configFile.exists()) {
        qDebug() << "[LauncherManager::loadConfig]配置文件不存在，将创建一个具有默认值的配置文件.";
        createDefaultConfig();
    }

    // 先从旧的version.json里加载出版本信息
    QFile versionJsonFile(PathUtil::combinePath(QApplication::applicationDirPath(), VERSION_JSON_PATH));
    if (!versionJsonFile.exists()) {
        qDebug() << "[LauncherManager::loadConfig]版本记录文件不存在，将全量下载服务端的文件";
    }
    else {
        if (versionJsonFile.open(QFileDevice::ReadOnly)) {
            QJsonParseError error;
            QJsonDocument jsonDoc = QJsonDocument::fromJson(versionJsonFile.readAll(), &error);
            if (jsonDoc.isNull()) {
                qDebug() << "[LauncherManager::loadConfig]版本记录Json解析失败，将全量下载服务端的文件：" + error.errorString();
            }
            else {
                m_currentVersion = jsonDoc["Version"].toString();
                emit updateVersion(m_currentVersion);
            }
        }
    }

    QNetworkAccessManager* manager = new QNetworkAccessManager(this);
    QString url = PathUtil::combineUrlPath(m_baseUrl, m_versionEndpoint);
    QNetworkReply* reply = manager->get(QNetworkRequest(QUrl(url)));

    // 创建一个 QTimer 用于超时处理
    QTimer* timeoutTimer = new QTimer(this);
    timeoutTimer->setSingleShot(true); // 确保只触发一次
    timeoutTimer->setInterval(10000); // 设置超时时间为 10 秒

    // 超时处理逻辑
    connect(timeoutTimer, &QTimer::timeout, [this, reply, timeoutTimer]() {
        if (reply->isRunning()) {
            reply->abort(); // 中止网络请求
            showFatalErrorTips("更新应用程序失败，将启动旧版本...");
            loadPlugin();
        }
        timeoutTimer->deleteLater(); // 删除超时计时器
    });

    // 网络请求完成的处理逻辑
    connect(reply, &QNetworkReply::finished, [this, reply, timeoutTimer]() {
        timeoutTimer->stop(); // 停止超时计时器
        timeoutTimer->deleteLater(); // 删除计时器

        if (reply->error() == QNetworkReply::NoError) {
            QByteArray response = reply->readAll();
            qDebug() << QString("[LauncherManager::checkForUpdates]收到服务端返回的version:%1").arg(response);

            QJsonDocument jsonDoc = QJsonDocument::fromJson(response);
            if (jsonDoc.isNull()) {
                showFatalErrorTips("解析服务端返回的json失败，应用程序将退出");
                QApplication::quit();
                return;
            }
            QJsonObject jsonObj = jsonDoc.object();

            QString latestVersion = jsonObj["Version"].toString();
            QJsonObject md5 = jsonObj["Md5"].toObject();
            std::vector<std::pair<QString, QString>> md5FileList;

            // 遍历 "Md5" 对象中的键值对
            for (auto it = md5.begin(); it != md5.end(); ++it) {
                QString fileName = it.key();      
                QString md5Value = it.value().toString();

                md5FileList.push_back({ fileName, md5Value });
            }

            if (latestVersion != m_currentVersion) {
                emit updateStatus("正在更新应用程序动态链接库...");
                downloadPlugin(md5FileList);
            }
            else {
                emit updateStatus("正在载入程序...");
                loadPlugin();
            }
        }
        else {
            showFatalErrorTips("检查更新失败,应用程序将退出: " + reply->errorString());
            QApplication::quit();
        }
        reply->deleteLater();
    });

    // 启动超时计时器
    timeoutTimer->start();
}

void LauncherManager::downloadPlugin(const std::vector<std::pair<QString, QString>>& md5FileList) {
    // 获取本地文件夹路径
    QString localDirPath = QApplication::applicationDirPath();

    QDir localDir(localDirPath);

    // 获取本地文件列表
    QStringList localFiles = localDir.entryList(QDir::Files);

    // 创建一个集合存储传入的文件名，方便后续检查本地多余文件
    QSet<QString> remoteFileNames;
    for (const auto& pair : md5FileList) {
        remoteFileNames.insert(pair.first);
    }

    // 读取本地 JSON 文件，记录当前版本下有哪些文件，以便下面检查是否需要删除
    QSet<QString> localFileNames;
    QFile jsonFile(PathUtil::combinePath(localDirPath, "version.json"));
    QJsonObject md5; // 用于存储 JSON 文件中的 MD5 信息
    if (jsonFile.exists() && jsonFile.open(QIODevice::ReadOnly)) {
        QJsonDocument jsonDoc = QJsonDocument::fromJson(jsonFile.readAll());
        if (!jsonDoc.isNull()) {
            QJsonObject jsonObj = jsonDoc.object();
            md5 = jsonObj["Md5"].toObject(); // 获取 JSON 文件中的 MD5 信息

            for (auto it = md5.begin(); it != md5.end(); ++it) {
                QString fileName = it.key();
                localFileNames.insert(fileName);
            }
        }
        jsonFile.close();
    }

    // 删除本地多余的文件
    for (const QString& localFileName : localFiles) {
        // 如果本地文件不在传入的文件列表中，则删除
        if (localFileNames.contains(localFileName) && !remoteFileNames.contains(localFileName)) {
            QString localFilePath = PathUtil::combinePath(localDirPath, localFileName);
            if (QFile::remove(localFilePath)) {
                qDebug() << "[LauncherManager::downloadPlugin]Deleted local file:" << localFilePath;
            }
            else {
                qDebug() << "[LauncherManager::downloadPlugin]Failed to delete local file:" << localFilePath;
            }
        }
    }

    // 队列存储需要下载的文件
    m_downloadQueue.clear();

    // 遍历传入的文件列表，检查是否需要下载
    for (const auto& pair : md5FileList) {
        QString fileName = pair.first;
        QString remoteMd5 = pair.second;

        QString localFilePath = localDirPath + QDir::separator() + fileName;
        QFile localFile(localFilePath);

        bool needDownload = true;

        if (localFile.exists()) {
            // 如果本地文件存在，计算其 MD5 值
            if (localFile.open(QIODevice::ReadOnly)) {
                QByteArray fileData = localFile.readAll();
                QString localMd5 = QCryptographicHash::hash(fileData, QCryptographicHash::Md5).toHex();
                localFile.close();

                // 如果 MD5 值一致，则不需要下载
                if (localMd5 == remoteMd5) {
                    needDownload = false;
                }
            }
        }

        if (needDownload) {
            // 将需要下载的文件加入队列
            m_downloadQueue.enqueue(fileName);
        }
    }

    // 开始下载队列中的文件
    if (!m_downloadQueue.isEmpty()) {
        // version.json一定要下载
        m_downloadQueue.push_back("version.json");
        m_needDownloadFileCount = m_downloadQueue.size();
        downloadNextFile();
    }
    else {
        qDebug() << "LauncherManager::downloadNextFileThe MD5 check of the files is complete, no files need to be downloaded, and the application will be launched.";
        loadPlugin();
    }
}

void LauncherManager::downloadNextFile() {
    if (m_downloadQueue.isEmpty()) {
        qDebug() << "[LauncherManager::downloadNextFile]Download finished, and the application will be launched.";
        loadPlugin();
        return;
    }

    // 从队列中取出下一个文件
    auto currentFile = m_downloadQueue.dequeue();
    QString fileName = currentFile;
    m_currentDownloadingFileName = fileName;

    QString localFilePath = PathUtil::combinePath(QApplication::applicationDirPath(), fileName);
    m_currentDownloadingFilePath = localFilePath;

    // 下载文件
    QNetworkAccessManager* manager = new QNetworkAccessManager(this);
    QString url = PathUtil::combineUrlPath(m_baseUrl, m_downloadEndpoint, fileName);
    QNetworkReply* reply = manager->get(QNetworkRequest(QUrl(url)));

    QElapsedTimer timer;
    qint64 lastBytesReceived = 0;
    timer.start();

    connect(reply, &QNetworkReply::downloadProgress, [this](qint64 bytesReceived, qint64 bytesTotal) {
        int progress = static_cast<int>((m_needDownloadFileCount - m_downloadQueue.size() - 1) * 100.0) / (m_needDownloadFileCount);
        emit updateProgress(progress);
        emit updateStatus("下载文件" + m_currentDownloadingFileName + "中:" +
            QString::number(bytesReceived / 1024.0f) + "kb");
    });

    connect(reply, &QNetworkReply::finished, [this, reply]() {
        if (reply->error() == QNetworkReply::NoError) {
            QFile file(m_currentDownloadingFilePath);
            if (file.open(QIODevice::WriteOnly)) {
                file.write(reply->readAll());
                file.close();
                qDebug() << "[LauncherManager::downloadNextFile]Downloaded file:" << m_currentDownloadingFileName;
            }
        }
        else {
            qWarning() << "[LauncherManager::downloadNextFile]Failed to download file:" << m_currentDownloadingFileName << ", error:" << reply->errorString();
        }

        reply->deleteLater();

        // 下载下一个文件
        downloadNextFile();
    });
}

void LauncherManager::loadPlugin() {
    QString dllPath = PathUtil::combinePath(QApplication::applicationDirPath(), m_entryPointDLLName);
    QPluginLoader loader(dllPath);
    QObject* plugin = loader.instance();
    if (plugin) {
        IPlugin* pluginInterface = qobject_cast<IPlugin*>(plugin);
        if (pluginInterface) {
            emit updateStatus("正在加载应用程序...");
            int retCode = 0;
            if ((retCode = pluginInterface->main(m_args)) != 0) {
                showFatalErrorTips(QString::asprintf("加载应用程序失败,错误码:%d", retCode));
                QApplication::quit();
            }
            else {
                emit loadSucceed();
            }
            return;
        }
    }
    showFatalErrorTips(QString("加载应用程序失败,无法正确加载插件:%1").arg(loader.errorString()));
    QApplication::quit();
}
