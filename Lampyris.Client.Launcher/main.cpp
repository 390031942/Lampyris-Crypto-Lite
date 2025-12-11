#include "LampyrisClientLauncher.h"
#include <QtWidgets/QApplication>

int main(int argc, char *argv[])
{
    QApplication a(argc, argv);
    LampyrisClientLauncher w;
    w.show();
    return a.exec();
}
