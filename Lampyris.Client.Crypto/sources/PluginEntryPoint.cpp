#pragma once

// Project Include(s)
#include "PluginEntryPoint.h"
#include "UIControlDemoWidget.h"

int PluginEntryPoint::main(const QStringList& args) {
    QMessageBox::information(NULL, "Plugin Entry Point Message", "Plugin Loaded Success(override 2)");
    return 0;
}
