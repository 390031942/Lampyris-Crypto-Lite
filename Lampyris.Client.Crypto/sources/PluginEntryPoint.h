#pragma once

// Project Include(s)
#include "Global.h"
#include "UI/Standalone/Main/MainWidget.h"

// QT Include(s)
#include <QMessageBox>
#include <QObject>

#if defined(LAMPYRIS_EXE)
#include <QApplication>
#else 
class LAMPYRIS_MODULE_EXPORT PluginEntryPoint :public QObject, public IPlugin {
    Q_OBJECT
    Q_INTERFACES(IPlugin)
    Q_PLUGIN_METADATA(IID LAMPYRIS_PLUGIN_IID)
public:
    virtual int main(const QStringList& args) {
        QMessageBox::information(NULL, "Plugin Entry Point Message", "Plugin Loaded Success(override 2)");
        return 0;
    }
};
#endif // !LAMPYRIS_EXE