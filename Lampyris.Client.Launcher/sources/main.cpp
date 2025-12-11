// Project Include(s)
#include "LauncherWidget.h"
#include "SingletonApp.h"

// QT Include(s)
#include <QApplication>

int main(int argc, char *argv[]) {
    SingletonApp a(argc, argv);

    QStringList arguments;
    for (int i = 0; i < argc; ++i) {
        arguments << QString::fromUtf8(argv[i]);
    }

    LauncherWidget w(arguments);
    w.show();

    return a.exec();
}
