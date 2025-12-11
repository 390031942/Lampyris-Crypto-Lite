#pragma once

// Project Include(s)
#include "Global.h"
#include "UI/Standalone/Main/MainWidget.h"

// QT Include(s)
#include <QMessageBox>
#include <QObject>

class LAMPYRIS_MODULE_EXPORT PluginEntryPoint :public QObject, public IPlugin {
    Q_OBJECT
    Q_INTERFACES(IPlugin)
    Q_PLUGIN_METADATA(IID LAMPYRIS_PLUGIN_IID)
public:
    virtual int main(const QStringList& args);
};
