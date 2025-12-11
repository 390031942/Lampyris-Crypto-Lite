#pragma once

// QT Include(s)
#include <QObject>
#include <QString>
#include <QStringList>
#include <QNetworkAccessManager>
#include <QNetworkReply>
#include <QSettings>
#include <QPluginLoader>
#include <QQueue>

#define CONFIG_FILE_PATH              "appconfig.ini"
#define VERSION_JSON_PATH             "version.json"
#define CONFIG_KEY_BASE_URL           "General/baseUrl"
#define CONFIG_KEY_VERSION_ENDPOINT   "General/versionEndpoint"
#define CONFIG_KEY_DOWNLOAD_ENDPOINT  "General/downloadEndpoint"
#define CONFIG_KEY_DLL_NAME           "General/entryPointDLLName"
#define RETRY_INTERVAL_MS              5000

class LauncherManager : public QObject {
    Q_OBJECT
    using DownloadQueue = QQueue<QString>;
public:
    explicit              LauncherManager(const QStringList& args, 
                                          QObject* parent = nullptr);
    void                  checkForUpdates();            
signals:                  
    void                  updateStatus(const QString& status);
    void                  updateVersion(const QString& version);
    void                  updateProgress(int progress);        
    void                  retryCountdown(int seconds);         
    void                  loadSucceed();
private:                  
    void                  loadConfig();
    void                  createDefaultConfig();
    void                  showFatalErrorTips(const QString& message);
    void                  downloadPlugin(const std::vector<std::pair<QString, QString>> &md5FileList);
    void                  downloadNextFile();
    void                  loadPlugin();
                          
    QStringList           m_args;
    QString               m_baseUrl;
    QString               m_versionEndpoint;
    QString               m_downloadEndpoint;
    QString               m_currentVersion;
    QString               m_entryPointDLLName;
    DownloadQueue         m_downloadQueue;
    int                   m_needDownloadFileCount;
    QString               m_currentDownloadingFileName;
    QString               m_currentDownloadingFilePath;
};
