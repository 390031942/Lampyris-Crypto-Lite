#pragma once

#include <QtWidgets/QMainWindow>
#include "ui_LampyrisClientLauncher.h"

class LampyrisClientLauncher : public QMainWindow
{
    Q_OBJECT

public:
    LampyrisClientLauncher(QWidget *parent = nullptr);
    ~LampyrisClientLauncher();

private:
    Ui::LampyrisClientLauncherClass ui;
};
